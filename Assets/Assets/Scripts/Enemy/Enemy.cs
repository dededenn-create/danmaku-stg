using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int hp = 1;
    public GameObject powerItemPrefab;
    public GameObject explosionBigPrefab;
    [Tooltip("既存の爆発に加えて出す、半透明の粒バースト(FakeExplosionBurstなど)。雑魚敵も含め、死ぬ全ての敵に適用される")]
    public GameObject deathBurstPrefab;

    [Header("中ボス撃破時の小アイテムばらまき")]
    public GameObject smallPowerItemPrefab;
    public int scatterCount = 10;
    public float spreadRadius = 0.5f;

    [Header("スコア")]
    public int killScoreValue = 200; // 雑魚200 / 強雑魚1000 / 中ボス15000をInspectorで設定

    [Header("サウンド")]
    public AudioSource audioSource;
    public AudioClip hitSound; 
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

    [Header("これがtrueだと、死んだ時にStageManagerへ通知する（中ボス用）")]
    public bool isBlockingBoss = false;

    [Header("これがtrueだと、死んだ時に演出（背景速度アップなど）を発動する（本物の中ボス専用）")]
    public bool isMidBoss = false;

    [Header("演出用（本物の中ボスの死亡時に速度を何倍にするか）")]
    public float midBossSpeedMultiplier = 1.5f;

    private bool isDead = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bullet"))
        {
            hp -= 1;
            Destroy(other.gameObject);

            ScoreManager.Instance.AddHitScore();

            if (hp <= 0 && !isDead)
            {
                Die();
            }
        }
    }

    /// <summary>
    /// 弾以外(ボムなど)から外部的にダメージを与えたい場合に呼ぶ。
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        hp -= amount;

        if (hp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// まとまった大きなダメージ(ボムなど)を、一発で入れるのではなく
    /// 高速で細かく何回も撃ち込む形にする。
    /// </summary>
    public void TakeDamageOverTime(int totalDamage, float duration, int tickCount = 10)
    {
        StartCoroutine(DamageOverTimeRoutine(totalDamage, duration, tickCount));
    }

    private IEnumerator DamageOverTimeRoutine(int totalDamage, float duration, int tickCount)
    {
        tickCount = Mathf.Max(1, tickCount);
        int perTick = Mathf.Max(1, totalDamage / tickCount);
        float interval = duration / tickCount;
        int applied = 0;

        for (int i = 0; i < tickCount; i++)
        {
            if (isDead) yield break;

            int amount = Mathf.Min(perTick, totalDamage - applied);
            if (amount <= 0) yield break;

            TakeDamage(amount);
            applied += amount;

            yield return new WaitForSeconds(interval);
        }
    }

    void Die()
    {
        isDead = true;

        ScoreManager.Instance.AddKillScore(killScoreValue);

        // 大アイテムは設定されている場合のみドロップ
        if (powerItemPrefab != null)
        {
            Instantiate(powerItemPrefab, transform.position, Quaternion.identity);
        }

        Instantiate(explosionBigPrefab, transform.position, Quaternion.identity);

        // 既存の爆発に加えて、半透明の粒バーストも追加で出す(雑魚敵にも適用)
        if (deathBurstPrefab != null)
        {
            Instantiate(deathBurstPrefab, transform.position, Quaternion.identity);
        }

        // 本物の中ボスなら、小アイテムを円状にばらまく
        if (isMidBoss && smallPowerItemPrefab != null)
        {
            for (int i = 0; i < scatterCount; i++)
            {
                float angle = (360f / scatterCount) * i;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * spreadRadius;

                Instantiate(smallPowerItemPrefab, transform.position + offset, Quaternion.identity);
            }
        }

        // ボス指定されてる敵が死んだら、StageManagerに知らせる
        if (isBlockingBoss)
        {
            StageManager stageManager = FindFirstObjectByType<StageManager>();
            if (stageManager != null)
            {
                stageManager.ClearBlockingEnemy();
            }
        }

        // 本物の中ボスが死んだら、背景・柱の速度アップ演出を発動
        if (isMidBoss)
        {
            BackgroundLoop[] backgrounds = FindObjectsByType<BackgroundLoop>(FindObjectsSortMode.None);
            foreach (var bg in backgrounds)
            {
                bg.SetSpeedMultiplier(midBossSpeedMultiplier);
            }

            PillarLoopManager[] pillarManagers = FindObjectsByType<PillarLoopManager>(FindObjectsSortMode.None);
            foreach (var pm in pillarManagers)
            {
                pm.SetSpeedMultiplier(midBossSpeedMultiplier);
            }

            ParticleSpeedController[] particleControllers = FindObjectsByType<ParticleSpeedController>(FindObjectsSortMode.None);
            foreach (var pc in particleControllers)
            {
                pc.SetSpeedMultiplier(midBossSpeedMultiplier);
            }

            // 本物の中ボスが死んだ時だけ、画面上の敵弾を全てプレイヤーへ自動吸収させる
            AutoCollectAllBullets();
        }

        PlayDeathSound();

        Destroy(gameObject);
    }

    /// <summary>
    /// 撃破音を鳴らす。一時的なAudioSourceを生成して鳴らすので、
    /// Inspectorのaudio Source欄を設定していなくても動く。
    /// extendDeathSoundがONの場合、ピッチはそのままに、ループ再生でextendedDeathSoundDuration秒まで長さを延長する。
    /// </summary>
    private void PlayDeathSound()
    {
        if (deathSound == null) return;

        GameObject temp = new GameObject("EnemyDeathSE");
        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = deathSound;
        src.volume = deathSoundVolume;
        src.pitch = deathSoundPitch;
        src.spatialBlend = 0f; // 2Dサウンド

        float totalDuration;
        if (extendDeathSound && extendedDeathSoundDuration > 0f)
        {
            src.loop = true;
            totalDuration = extendedDeathSoundDuration; // ピッチに関係なく、指定した秒数そのまま使う
        }
        else
        {
            src.loop = false;
            totalDuration = deathSound.length / Mathf.Max(0.01f, Mathf.Abs(deathSoundPitch));
        }

        src.Play();
        Destroy(temp, totalDuration);
    }

    /// <summary>
    /// 画面上に残っている全ての敵弾(EnemyBullet.activeBullets)を
    /// プレイヤーへ自動的に吸い込ませる。EnemyBullet.StartSuckToPlayer()が
    /// 吸い込み中はCollider2Dを無効化する仕組みになっているため、
    /// 自動回収中の弾に触れてもミスにはならない。
    /// </summary>
    private void AutoCollectAllBullets()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("Enemy: PlayerタグのGameObjectが見つからないため、弾の自動回収をスキップしました。");
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
}