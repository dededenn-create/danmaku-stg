using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BossController : MonoBehaviour
{
    public enum BossState { Spawning, GaugeFill, PreBattleDelay, Battle, PhaseClearDelay, Defeated }
    public BossState state = BossState.Spawning;

    [Header("出現演出")]
    [Tooltip("ボスが出現してから、ゲージ表示&回復&BGMが始まるまでの間(秒)。登場モーションの時間などに合わせて調整")]
    public float spawnDelay = 0.5f;
    [Tooltip("出現時、アルファ値0から1へこの秒数かけてうっすらフェードインする")]
    public float fadeInDuration = 1.0f;

    private SpriteRenderer[] spriteRenderers;

    [Header("フェーズごとのHP設定")]
    [Tooltip("要素数がフェーズ数になる。例：[800, 1000, 1200] なら3フェーズ")]
    public float[] phaseMaxHP = new float[] { 800f, 1000f, 1200f };

    [Header("ゲージ回復速度")]
    [Tooltip("ゲージが0から満タンになるまでにかかる秒数(フェーズのHP量に関わらず一定)")]
    public float gaugeFillDuration = 2.5f;

    [Header("戦闘開始までの遅延")]
    [Tooltip("ゲージが満タンになってから、実際に弾幕・被弾判定が始まるまでの待ち時間(秒)")]
    public float preBattleDelay = 1.0f;

    [Header("フェーズクリア後の遅延")]
    [Tooltip("フェーズのHPが0になってから、次のゲージ回復が始まるまでの待ち時間(秒)")]
    public float postPhaseClearDelay = 1.0f;

    [Header("フェーズごとの弾幕スクリプト")]
    [Tooltip("currentPhaseに対応する順番で登録する(要素0=フェーズ1, 要素1=フェーズ2...)")]
    [SerializeField] private MonoBehaviour[] phaseBulletScripts;

    [Header("必殺技カットイン(骨組みのみ、中身は後で差し込む)")]
    [Tooltip("カットインを表示するパネル(GameObject)。中身の見た目は未実装、ON/OFFのタイミングだけ")]
    [SerializeField] private GameObject cutInPanel;
    [Tooltip("カットインを表示するフェーズ番号のリスト(例: 2, 4)")]
    [SerializeField] private int[] cutInPhases = new int[] { 2, 4 };
    [SerializeField] private float cutInDuration = 1.0f;

    private float currentHP;    // 実際のHP(被弾で減る)
    private float displayHP;    // ゲージ表示用(回復時に0→maxまで動く)

    [Header("フェーズ管理")]
    public int currentPhase = 1; // 1始まり
    public int totalPhases => phaseMaxHP.Length;

    private float CurrentMaxHP => phaseMaxHP[currentPhase - 1];

    public event System.Action<float> onDisplayHPChanged; // 0〜1で通知(UIゲージ用)
    public event System.Action<int> onPhaseStart;          // フェーズ開始通知(弾幕切り替え用)
    public event System.Action onBossDefeated;             // 撃破通知
    public event System.Action onGaugeFillStart;           // ゲージ回復(0→満タン)が始まったタイミングで通知

    [Header("デバッグ(演出確認用、リリース前にOFFにすること)")]
    [Tooltip("ONにすると、起動時に最初から最終フェーズ(最後の弾幕)でスタートする。撃破演出を素早く何度も見たい時用")]
    public bool debugSkipToFinalPhaseOnStart = false;
#if ENABLE_INPUT_SYSTEM
    [Tooltip("このキーを押すと、その場で即座にHPを0にして撃破する(Battle中のみ有効)")]
    public Key debugInstantKillKey = Key.K;
#endif

    void Start()
    {
        currentPhase = debugSkipToFinalPhaseOnStart ? totalPhases : 1;
        displayHP = 0f;
        currentHP = 0f;
        state = BossState.Spawning;

        // 満タンになるまでは被弾しない(無敵)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 出現時、いきなり出さずアルファ値でうっすらフェードインさせる
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        SetSpriteAlpha(0f);
        StartCoroutine(FadeInRoutine());

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            SetSpriteAlpha(Mathf.Clamp01(t / fadeInDuration));
            yield return null;
        }
        SetSpriteAlpha(1f);
    }

    private void SetSpriteAlpha(float alpha)
    {
        if (spriteRenderers == null) return;

        foreach (SpriteRenderer sr in spriteRenderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        // ボス出現〜ゲージ表示/BGM開始までの「間」
        yield return new WaitForSeconds(spawnDelay);

        state = BossState.GaugeFill;
        onGaugeFillStart?.Invoke(); // 初回のゲージ回復開始を通知(BGMイントロのトリガー用)
    }

    void Update()
    {
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && ENABLE_INPUT_SYSTEM
        if (state == BossState.Battle && Keyboard.current != null && Keyboard.current[debugInstantKillKey].wasPressedThisFrame)
        {
            TakeDamage(currentHP); // 現在のHPをまるごと削って即座に撃破演出へ
        }
#endif

        switch (state)
        {
            case BossState.Spawning:
                // SpawnRoutine側で待機中。ここでは何もしない
                break;
            case BossState.GaugeFill:
                HandleGaugeFill();
                break;
            case BossState.PreBattleDelay:
                // コルーチン(EnterBattleRoutine)側で待機中。ここでは何もしない
                break;
            case BossState.PhaseClearDelay:
                // コルーチン(PhaseClearRoutine)側で待機中。ここでは何もしない
                break;
            case BossState.Battle:
                // 弾幕中。被弾処理はTakeDamage()から呼ばれる
                break;
            case BossState.Defeated:
                break;
        }
    }

    void HandleGaugeFill()
    {
        float maxHP = CurrentMaxHP;

        // 1秒あたり (maxHP / gaugeFillDuration) だけ増える = 常にgaugeFillDuration秒で満タン
        float fillSpeed = maxHP / gaugeFillDuration;
        displayHP += fillSpeed * Time.deltaTime;
        onDisplayHPChanged?.Invoke(Mathf.Clamp01(displayHP / maxHP));

        if (displayHP >= maxHP)
        {
            displayHP = maxHP;
            currentHP = maxHP;
            StartCoroutine(EnterBattleRoutine());
        }
    }

    private IEnumerator EnterBattleRoutine()
    {
        // ゲージが満タンになった直後、まだ弾幕も被弾判定も始めない
        state = BossState.PreBattleDelay;

        yield return new WaitForSeconds(preBattleDelay);

        // 必殺技フェーズ(cutInPhasesに含まれる)なら、弾幕開始前にカットインを挟む
        if (IsCutInPhase(currentPhase) && cutInPanel != null)
        {
            cutInPanel.SetActive(true);
            yield return new WaitForSeconds(cutInDuration);
            cutInPanel.SetActive(false);
        }

        // 遅延が終わってから、初めて弾幕開始と被弾判定ON
        state = BossState.Battle;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        onPhaseStart?.Invoke(currentPhase);
        StartBulletPattern();
    }

    // 弾がボスに当たった時、弾側のスクリプトからこれを呼ぶ
    public void TakeDamage(float damage)
    {
        if (state != BossState.Battle) return;

        float maxHP = CurrentMaxHP;

        // 通常の敵(Enemy.cs)と同じく、被弾のたびにヒットスコアを加算する
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddHitScore();
        }

        currentHP -= damage;
        onDisplayHPChanged?.Invoke(Mathf.Clamp01(currentHP / maxHP));

        if (currentHP <= 0f)
        {
            currentHP = 0f;
            OnPhaseCleared();
        }
    }

    /// <summary>
    /// まとまった大きなダメージ(ボムなど)を、一発で入れるのではなく
    /// 高速で細かく何回も撃ち込む形にする。TakeDamage()を連続で呼ぶだけなので、
    /// 実際のHPもゲージの見た目も、両方自然にカウントダウンしていく。
    /// </summary>
    public void TakeDamageOverTime(float totalDamage, float duration, int tickCount = 20)
    {
        StartCoroutine(DamageOverTimeRoutine(totalDamage, duration, tickCount));
    }

    private IEnumerator DamageOverTimeRoutine(float totalDamage, float duration, int tickCount)
    {
        tickCount = Mathf.Max(1, tickCount);
        float perTick = totalDamage / tickCount;
        float interval = duration / tickCount;

        for (int i = 0; i < tickCount; i++)
        {
            // フェーズが切り替わるなどしてBattle状態でなくなったら、そこで打ち切る
            if (state != BossState.Battle) yield break;

            TakeDamage(perTick);

            yield return new WaitForSeconds(interval);
        }
    }

    void OnPhaseCleared()
    {
        StopBulletPattern();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; // 回復中は無敵

        // 途中フェーズの終わりごとに小アイテムをばらまく(最終フェーズ撃破時はアイテムを一切出さない)
        if (currentPhase < totalPhases)
        {
            SpawnScatterItems();
        }

        // フェーズの終わりごとに、画面上の全敵弾をプレイヤーへ自動吸収させる(触れてもミスにならない)
        AutoCollectAllBullets();

        // フェーズの終わりごとに、毎回スコアを加算する(最終フェーズ撃破時も含む)
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddKillScore(phaseClearScoreValue);
        }

        if (currentPhase >= totalPhases)
        {
            Defeat();
        }
        else
        {
            // 途中フェーズクリア時、1回だけ粒バーストを発動する(最終フェーズはDefeat側の爆発演出があるのでここでは出さない)
            if (phaseClearBurstPrefab != null)
            {
                Instantiate(phaseClearBurstPrefab, transform.position, Quaternion.identity);
            }

            StartCoroutine(PlayPhaseClearSoundRoutine());

            StartCoroutine(PhaseClearRoutine());
        }
    }

    private IEnumerator PhaseClearRoutine()
    {
        // HPが0になった直後、少し間を置いてから次フェーズの回復を始める
        state = BossState.PhaseClearDelay;

        yield return new WaitForSeconds(postPhaseClearDelay);

        currentPhase++;
        displayHP = 0f;
        state = BossState.GaugeFill; // 再度ゲージ回復開始(次フェーズのHPまで溜まる)

        // 2フェーズ目以降のゲージ回復ではBGMは鳴らさない(BossSequenceManager側で初回のみに制御)
        onGaugeFillStart?.Invoke();
    }

    [Header("撃破時の演出・報酬(Enemy.csのDie()を移植)")]
    public GameObject powerItemPrefab;
    public GameObject explosionBigPrefab;
    [Tooltip("完全撃破(最終フェーズ撃破)の瞬間に、フェーズクリアスコアとは別に追加で加算されるスコア")]
    public int killScoreValue = 15000;

    [Header("フェーズ終了時のスコア(毎フェーズ共通、撃破時も含む)")]
    [Tooltip("フェーズのHPが0になるたび、毎回加算されるスコア(最終フェーズ撃破時もここでまず加算される)")]
    public int phaseClearScoreValue = 3000;

    [Header("途中フェーズクリア時の演出")]
    [Tooltip("途中フェーズクリア時、1回だけ発動する粒バースト(FakeExplosionBurstなど)。最終フェーズの撃破演出とは別物")]
    public GameObject phaseClearBurstPrefab;

    [Header("フェーズ終了時の小アイテムばらまき(毎フェーズ共通)")]
    public GameObject smallPowerItemPrefab;
    public int scatterCount = 10;
    public float spreadRadius = 0.5f;

    [Header("サウンド")]
    [Tooltip("撃破時に鳴らす音。AudioSourceは不要(一時的なAudioSourceを自動生成して鳴らす)")]
    public AudioClip deathSound;
    [Tooltip("撃破音の音量")]
    [Range(0f, 1f)]
    public float deathSoundVolume = 1f;
    [Tooltip("撃破音の再生ピッチ。1が通常、下げると低く、上げると高くなる")]
    public float deathSoundPitch = 1f;
    [Tooltip("ONにすると、音源をループ再生してExtended Durationの秒数まで引き伸ばす(ピッチは変わらず、長さだけ別途延長される)")]
    public bool extendDeathSound = false;
    [Tooltip("引き伸ばす場合の合計再生時間(秒)。extendDeathSoundがONの時のみ使う")]
    public float extendedDeathSoundDuration = 2f;
    [Tooltip("撃破の瞬間から、実際に音が鳴り始めるまでの遅延時間(秒、実時間)")]
    public float deathSoundDelay = 0f;

    [Header("サウンド(途中フェーズクリア時)")]
    [Tooltip("途中フェーズクリア時に鳴らす音。最終フェーズ撃破時はdeathSoundが鳴るのでこちらは鳴らない")]
    public AudioClip phaseClearSound;
    [Range(0f, 1f)]
    public float phaseClearSoundVolume = 1f;
    [Tooltip("フェーズクリア音の再生ピッチ。1が通常")]
    public float phaseClearSoundPitch = 1f;
    [Tooltip("ONにすると、音源をループ再生してExtended Durationの秒数まで引き伸ばす")]
    public bool extendPhaseClearSound = false;
    public float extendedPhaseClearSoundDuration = 1.5f;
    [Tooltip("フェーズクリアの瞬間から、実際に音が鳴り始めるまでの遅延時間(秒、実時間)")]
    public float phaseClearSoundDelay = 0f;

    /// <summary>
    /// フェーズ終了(HPが0になった瞬間)ごとに毎回呼ばれる、小アイテムのばらまき処理。
    /// 途中フェーズでも最終フェーズ(撃破)でも、OnPhaseCleared()から共通で呼ばれる。
    /// </summary>
    void SpawnScatterItems()
    {
        if (smallPowerItemPrefab == null) return;

        for (int i = 0; i < scatterCount; i++)
        {
            float angle = (360f / scatterCount) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * spreadRadius;
            Instantiate(smallPowerItemPrefab, transform.position + offset, Quaternion.identity);
        }
    }

    /// <summary>
    /// フェーズ終了ごとに、画面上に残っている全ての敵弾(EnemyBullet.activeBullets)を
    /// プレイヤーへ自動的に吸い込ませる。EnemyBullet.StartSuckToPlayer()が
    /// 吸い込み中はCollider2Dを無効化する仕組みになっているため、
    /// 自動回収中の弾に触れてもミスにはならない。
    /// </summary>
    void AutoCollectAllBullets()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("BossController: PlayerタグのGameObjectが見つからないため、弾の自動回収をスキップしました。");
            return;
        }

        // ToArray()でコピーしてから回す(StartSuckToPlayer中にリストの中身が変わることはないが念のため安全に)
        foreach (EnemyBullet bullet in EnemyBullet.activeBullets.ToArray())
        {
            if (bullet != null)
            {
                bullet.StartSuckToPlayer(playerObj.transform);
            }
        }
    }

    void Defeat()
    {
        state = BossState.Defeated;
        StopBulletPattern();

        // 完全撃破時、フェーズクリアスコア(OnPhaseClearedで既に加算済み)とは別に、追加のボーナススコアを加算
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddKillScore(killScoreValue);
        }

        // 最終撃破時はアイテムを一切出さない(大アイテム・小アイテムのばらまきどちらも無し)

        onBossDefeated?.Invoke();

        StartCoroutine(DefeatEffectRoutine());
    }

    [Header("撃破演出(スローモーション→爆発)")]
    [Tooltip("スローモーション中のTime.timeScale。小さいほどゆっくりになる")]
    public float slowMotionTimeScale = 0.2f;
    [Tooltip("体力が0になった瞬間からスローモーション開始。そこから爆発が起きるまでの実時間(秒、溜めの間)")]
    public float delayBeforeExplosion = 0.5f;
    [Tooltip("爆発が起きてから、通常速度に戻すまでスローモーションを継続する実時間(秒)")]
    public float slowMotionHoldAfterExplosion = 1.0f;

    /// <summary>
    /// 撃破の瞬間に呼ばれる演出。
    /// 体力0→即スローモーション開始→(スロー中に)爆発→少し継続→通常速度に戻してボスを消す、の流れ。
    /// 待ち時間はすべてWaitForSecondsRealtimeを使うので、timeScaleが下がっていても正しく実時間で進む。
    /// </summary>
    private System.Collections.IEnumerator DefeatEffectRoutine()
    {
        // ①体力を削り切った瞬間、即座にスローモーション開始
        Time.timeScale = slowMotionTimeScale;

        // ②スローの中で少し溜めてから爆発
        yield return new WaitForSecondsRealtime(delayBeforeExplosion);

        if (explosionBigPrefab != null)
        {
            Instantiate(explosionBigPrefab, transform.position, Quaternion.identity);
        }
        StartCoroutine(PlayDeathSoundRoutine());

        // ③爆発後、少しスローを継続してから通常速度に戻す
        yield return new WaitForSecondsRealtime(slowMotionHoldAfterExplosion);

        Time.timeScale = 1f; // 通常速度に戻す

        Destroy(gameObject);
    }

    /// <summary>
    /// 撃破音を鳴らす。一時的なAudioSourceを生成して鳴らすので、
    /// あらかじめシーンやプレハブにAudioSourceを用意しておく必要がない。
    /// deathSoundDelay秒(実時間)待ってから鳴らし始める。
    /// extendDeathSoundがONの場合、ピッチはそのままに、ループ再生でextendedDeathSoundDuration秒まで長さを延長する。
    /// </summary>
    private IEnumerator PlayDeathSoundRoutine()
    {
        yield return PlayOneShotSoundRoutine(
            deathSound, deathSoundVolume, deathSoundPitch,
            extendDeathSound, extendedDeathSoundDuration, deathSoundDelay, "BossDeathSE");
    }

    /// <summary>
    /// 途中フェーズクリア音を鳴らす。仕組みはPlayDeathSoundRoutineと同じ。
    /// </summary>
    private IEnumerator PlayPhaseClearSoundRoutine()
    {
        yield return PlayOneShotSoundRoutine(
            phaseClearSound, phaseClearSoundVolume, phaseClearSoundPitch,
            extendPhaseClearSound, extendedPhaseClearSoundDuration, phaseClearSoundDelay, "BossPhaseClearSE");
    }

    /// <summary>
    /// 効果音再生の共通処理。一時的なAudioSourceを生成して鳴らし、鳴り終わったら自動的に消す。
    /// delaySeconds待ってから再生を開始し(実時間、timeScaleの影響を受けない)、
    /// extendがtrueならピッチはそのままループ再生でextendedDuration秒まで長さを延長する。
    /// </summary>
    private IEnumerator PlayOneShotSoundRoutine(
        AudioClip clip, float volume, float pitch,
        bool extend, float extendedDuration, float delaySeconds, string tempObjectName)
    {
        if (clip == null) yield break;

        if (delaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
        }

        GameObject temp = new GameObject(tempObjectName);
        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.spatialBlend = 0f; // 2Dサウンド

        float totalDuration;
        if (extend && extendedDuration > 0f)
        {
            src.loop = true;
            totalDuration = extendedDuration; // ピッチに関係なく、指定した秒数そのまま使う
        }
        else
        {
            src.loop = false;
            totalDuration = clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch));
        }

        src.Play();
        Destroy(temp, totalDuration);
    }

    void OnDestroy()
    {
        // 万が一演出の途中でこのオブジェクトが破棄されても、時間が止まったままにならないようにする
        Time.timeScale = 1f;
    }

    private bool IsCutInPhase(int phase)
    {
        foreach (int p in cutInPhases)
        {
            if (p == phase) return true;
        }
        return false;
    }

    void StartBulletPattern()
    {
        // currentPhase(1始まり)に対応する弾幕スクリプトだけ有効化、他は無効化
        for (int i = 0; i < phaseBulletScripts.Length; i++)
        {
            if (phaseBulletScripts[i] == null) continue;
            phaseBulletScripts[i].enabled = (i == currentPhase - 1);
        }
    }

    void StopBulletPattern()
    {
        // 全フェーズの弾幕スクリプトを無効化(発射停止)
        foreach (var script in phaseBulletScripts)
        {
            if (script != null) script.enabled = false;
        }
    }
}