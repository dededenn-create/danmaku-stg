using System.Collections;
using UnityEngine;

public class BossSequenceManager : MonoBehaviour
{
    public static BossSequenceManager Instance;

    [Header("背景切り替え")]
    [SerializeField] private GameObject bossBackground;
    [SerializeField] private GameObject roadBackground;

    [Header("道中演出")]
    [SerializeField] private ParticleSystem[] roadAmbientParticles;

    [Header("暗転演出")]
    [SerializeField] private CanvasGroup transitionPanel;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float holdBlackDuration = 0.3f;
    [SerializeField] private float fadeInDuration = 0.5f;

    [Header("音楽フェード")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private float musicFadeOutDuration = 1.0f;

    [Header("タイミング")]
    [SerializeField] private float clearDelay = 0.5f;
    [SerializeField] private float bossSpawnDelayAfterBackground = 2.0f;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform bossSpawnPoint;

    [Header("UI")]
    [SerializeField] private BossHealthGaugeUI bossHealthGaugeUI;
    [SerializeField] private ResultDisplayController resultDisplayController;

    [Header("ボス戦BGM")]
    [SerializeField] private AudioSource bgmLoopSource;   // bgmSourceとは別に用意するループ専用AudioSource
    [SerializeField] private AudioClip bossBgmIntroClip;  // イントロ部分
    [SerializeField] private AudioClip bossBgmLoopClip;   // ループ本編部分
    [SerializeField] private double scheduleLeadTime = 0.5; // PlayScheduledの予約余裕(秒)

    [Header("曲名表示")]
    [SerializeField] private StageIntroDisplay stageIntroDisplay;
    [SerializeField] private string bossSongName = "ボス曲名をここに";

    private bool countdownStarted = false;
    private bool bossBgmStarted = false; // 最初の1回だけ鳴らすためのフラグ

    private void Awake()
    {
        Instance = this;

        if (transitionPanel != null)
        {
            transitionPanel.alpha = 0f;
        }
    }

    public void StartCountdown(float delay)
    {
        if (countdownStarted) return;
        countdownStarted = true;
        StartCoroutine(CountdownRoutine(delay));
    }

    private IEnumerator CountdownRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartBossSequence();
    }

    public void StartBossSequence()
    {
        StartCoroutine(BossSequenceRoutine());
    }

    [Header("ボス登場演出")]
    [Tooltip("周囲から粒が集まってくる演出のプレハブ(BossConvergeIntroが付いたもの)。未設定なら演出なし")]
    [SerializeField] private GameObject convergeIntroPrefab;
    [Tooltip("背景切り替えが終わってから、この演出が始まるまでの秒数")]
    [SerializeField] private float convergeStartDelay = 1.0f;
    [Tooltip("演出が始まってから、何秒間続けるか")]
    [SerializeField] private float convergeDuration = 1.5f;

    private IEnumerator BossSequenceRoutine()
    {
        ClearAllEnemies();

        yield return new WaitForSeconds(clearDelay);

        yield return StartCoroutine(SwitchToBossBackground());

        // 登場演出は、ボス出現までの待機とは別に並行して動かす(お互いのタイミングに干渉しない)
        StartCoroutine(PlayConvergeIntroRoutine());

        yield return new WaitForSeconds(bossSpawnDelayAfterBackground);

        SpawnBoss();
    }

    /// <summary>
    /// 背景切り替え後、convergeStartDelay秒待ってから演出を開始し、
    /// convergeDuration秒間だけ続ける。ボス出現のタイミングとは独立して動く。
    /// </summary>
    private IEnumerator PlayConvergeIntroRoutine()
    {
        if (convergeIntroPrefab == null || bossSpawnPoint == null) yield break;

        yield return new WaitForSeconds(convergeStartDelay);

        GameObject introObj = Instantiate(convergeIntroPrefab, bossSpawnPoint.position, Quaternion.identity);
        BossConvergeIntro intro = introObj.GetComponent<BossConvergeIntro>();
        if (intro != null)
        {
            intro.Begin(convergeDuration);
        }
    }

    private void ClearAllEnemies()
    {
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
            Destroy(enemy);
    }

    private IEnumerator SwitchToBossBackground()
    {
        // 1. 暗転(画面が黒くなる)
        yield return StartCoroutine(FadePanel(0f, 1f, fadeOutDuration));

        // 2. 背景切り替え(画面は黒いまま裏で切り替える)
        roadBackground.SetActive(false);
        bossBackground.SetActive(true);

        if (roadAmbientParticles != null)
        {
            foreach (var ps in roadAmbientParticles)
            {
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        // 3. 音楽フェードは暗転処理と完全に独立して裏で進行(待たない)
        StartCoroutine(FadeMusic(musicFadeOutDuration));

        yield return new WaitForSeconds(holdBlackDuration);

        // 4. 明転(画面が戻る)
        yield return StartCoroutine(FadePanel(1f, 0f, fadeInDuration));
    }

    private IEnumerator FadePanel(float from, float to, float duration)
    {
        if (transitionPanel == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transitionPanel.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        transitionPanel.alpha = to;
    }

    private IEnumerator FadeMusic(float duration)
    {
        if (bgmSource == null) yield break;

        float startVolume = bgmSource.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }

        bgmSource.volume = 0f;
        bgmSource.Stop();

        if (bgmLoopSource != null)
        {
            bgmLoopSource.volume = 0f;
            bgmLoopSource.Stop();
        }
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null || bossSpawnPoint == null) return;

        GameObject bossObj = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);
        BossController controller = bossObj.GetComponent<BossController>();

        if (controller != null && bossHealthGaugeUI != null)
        {
            bossHealthGaugeUI.Show();
            controller.onDisplayHPChanged += bossHealthGaugeUI.SetFill;
            controller.onBossDefeated += bossHealthGaugeUI.Hide;
        }

        if (controller != null)
        {
            controller.onGaugeFillStart += OnBossGaugeFillStart;

            if (resultDisplayController != null)
            {
                controller.onBossDefeated += resultDisplayController.HandleBossDefeated;
            }
        }
    }

    private void OnBossGaugeFillStart()
    {
        // 最初の1回(ボス初登場、ゲージが0から溜まり始めた瞬間)だけBGMを鳴らす
        if (bossBgmStarted) return;
        bossBgmStarted = true;

        PlayBossBgmWithIntro();

        if (stageIntroDisplay != null)
        {
            stageIntroDisplay.PlayBossSongName(bossSongName);
        }
    }

    /// <summary>
    /// イントロを再生し、そのままシームレスにループ本編へつなげる。
    /// PlayScheduledで開始時刻をdspTime基準で予約するため、フレーム単位の誤差が出ない。
    /// </summary>
    private void PlayBossBgmWithIntro()
    {
        if (bgmSource == null || bgmLoopSource == null) return;
        if (bossBgmIntroClip == null || bossBgmLoopClip == null) return;

        // 念のため両方停止しておく
        bgmSource.Stop();
        bgmLoopSource.Stop();

        // イントロ側の設定
        bgmSource.clip = bossBgmIntroClip;
        bgmSource.loop = false;
        bgmSource.volume = 1f;

        // ループ側の設定
        bgmLoopSource.clip = bossBgmLoopClip;
        bgmLoopSource.loop = true;
        bgmLoopSource.volume = 1f;

        double startTime = AudioSettings.dspTime + scheduleLeadTime;
        double introLength = (double)bossBgmIntroClip.samples / bossBgmIntroClip.frequency;

        bgmSource.PlayScheduled(startTime);
        bgmLoopSource.PlayScheduled(startTime + introLength);
    }
}