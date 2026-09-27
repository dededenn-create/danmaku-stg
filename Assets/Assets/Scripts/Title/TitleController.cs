using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class TitleController : MonoBehaviour
{
    [Header("遷移先シーン名")]
    public string gameSceneName = "SampleScene";

    [Header("フェード演出(シーン遷移)")]
    public GameObject fadeCanvasRoot;
    public CanvasGroup fadeGroup;

    public float flashDuration = 1f;   // 暗転にかかる時間
    public float holdDuration = 0.3f;  // 真っ黒な状態を維持する時間
    public float fadeInDuration = 1f;  // フェードインにかかる時間

    [Header("表示テキスト")]
    [Tooltip("最初に表示しておく「Press Z Button」のCanvasGroup")]
    public CanvasGroup pressZTextGroup;
    [Tooltip("1回目のZ押下後に表示する「Game Start」のCanvasGroup。この状態でもう一度Zを押すとゲームが始まる")]
    public CanvasGroup gameStartTextGroup;

    [Header("テキスト切り替えのタイミング")]
    [Tooltip("Press Z Buttonがフェードアウトする時間")]
    public float pressZFadeOutDuration = 0.5f;
    [Tooltip("Press Z Buttonが消えてから、Game Startが浮かび上がり始めるまでの間")]
    public float delayBeforeGameStartFadeIn = 0.3f;
    [Tooltip("Game Startがフェードインする時間")]
    public float gameStartFadeInDuration = 0.5f;

    [Header("SE(決定音と同じものを設定する想定)")]
    public AudioClip confirmSound;
    [Range(0f, 1f)]
    public float confirmSoundVolume = 1f;
    public float confirmSoundPitch = 1f;

    private enum TitleState { PressZ, GameStartReady, Transitioning }
    private TitleState state = TitleState.PressZ;
    private bool isSwitchingText = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (fadeCanvasRoot != null)
        {
            DontDestroyOnLoad(fadeCanvasRoot);
        }
    }

    void Start()
    {
        state = TitleState.PressZ;
        isSwitchingText = false;

        if (pressZTextGroup != null)
        {
            pressZTextGroup.gameObject.SetActive(true);
            pressZTextGroup.alpha = 1f;
        }
        if (gameStartTextGroup != null)
        {
            gameStartTextGroup.gameObject.SetActive(false);
            gameStartTextGroup.alpha = 0f;
        }
    }

    void Update()
    {
        if (state == TitleState.Transitioning) return;
        if (isSwitchingText) return; // Press Z→Game Startへの切り替え演出中は入力を無視

        bool pressed = Keyboard.current.zKey.wasPressedThisFrame ||
                       Keyboard.current.enterKey.wasPressedThisFrame;

        if (!pressed) return;

        PlayConfirmSound();

        if (state == TitleState.PressZ)
        {
            // 1回目のZ: 「Press Z Button」をフェードアウト→少し間を置いて「Game Start」をフェードイン
            state = TitleState.GameStartReady;
            isSwitchingText = true;
            StartCoroutine(SwitchToGameStartText());
        }
        else if (state == TitleState.GameStartReady)
        {
            // 2回目のZ: ここで初めて実際にゲームを開始する
            StartGame();
        }
    }

    private IEnumerator SwitchToGameStartText()
    {
        if (pressZTextGroup != null)
        {
            yield return FadeCanvasGroup(pressZTextGroup, 1f, 0f, pressZFadeOutDuration);
            pressZTextGroup.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(delayBeforeGameStartFadeIn);

        if (gameStartTextGroup != null)
        {
            gameStartTextGroup.gameObject.SetActive(true);
            gameStartTextGroup.alpha = 0f;
            yield return FadeCanvasGroup(gameStartTextGroup, 0f, 1f, gameStartFadeInDuration);
        }

        isSwitchingText = false;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float t = 0f;
        group.alpha = from;

        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        group.alpha = to;
    }

    /// <summary>
    /// SEを再生する。一時的なAudioSourceを生成して鳴らすので、
    /// あらかじめシーンやプレハブにAudioSourceを用意しておく必要がない。
    /// </summary>
    private void PlayConfirmSound()
    {
        if (confirmSound == null) return;

        GameObject temp = new GameObject("TitleConfirmSE");
        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = confirmSound;
        src.volume = confirmSoundVolume;
        src.pitch = confirmSoundPitch;
        src.spatialBlend = 0f; // 2Dサウンド
        DontDestroyOnLoad(temp); // シーン遷移中でも最後まで鳴り切るように
        src.Play();

        float safePitch = Mathf.Max(0.01f, Mathf.Abs(confirmSoundPitch));
        Destroy(temp, confirmSound.length / safePitch);
    }

    public void StartGame()
    {
        if (state == TitleState.Transitioning) return;
        state = TitleState.Transitioning;
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        // 1. 全体的にぶわっと暗くなる
        fadeGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            fadeGroup.alpha = Mathf.Clamp01(elapsed / flashDuration);
            yield return null;
        }
        fadeGroup.alpha = 1f;

        // 2. 真っ黒な状態をしばらく維持する
        yield return new WaitForSeconds(holdDuration);

        // 3. シーン読み込み（真っ黒な状態のまま切り替わる）
        yield return SceneManager.LoadSceneAsync(gameSceneName);

        // 4. 黒からゆっくりフェードインしてゲーム画面を見せる
        elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            fadeGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        fadeGroup.alpha = 0f;

        // 5. 役目を終えたので消す
        Destroy(fadeCanvasRoot);
        Destroy(gameObject);
    }
}