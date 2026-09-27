using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using unityroom.Api;

public class GameOverController : MonoBehaviour
{
    [Header("ゲームオーバー用パネル")]
    [SerializeField] private CanvasGroup gameOverPanel;
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private float gameOverMaxAlpha = 0.7f;
    [SerializeField] private float gameOverDelay = 0.5f;

    [Header("スコア表示")]
    [Tooltip("死亡時点のスコアを表示するテキスト")]
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("被弾時の軽いフラッシュ用パネル")]
    [SerializeField] private CanvasGroup hitFlashPanel;
    [SerializeField] private float hitFlashInDuration = 0.05f;
    [SerializeField] private float hitFlashOutDuration = 0.3f;
    [SerializeField] private float hitFlashMaxAlpha = 0.4f;

    [Header("選択肢")]
    [SerializeField] private TextMeshProUGUI restartText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    [Header("カーソル")]
    [SerializeField] private RectTransform cursorIcon;
    [SerializeField] private RectTransform restartCursorPos;
    [SerializeField] private RectTransform titleCursorPos;

    [Header("SE(移動音・決定音)")]
    [Tooltip("カーソル移動時の音。AudioSourceは不要(一時的なAudioSourceを自動生成して鳴らす)")]
    [SerializeField] private AudioClip moveSound;
    [Range(0f, 1f)]
    [SerializeField] private float moveSoundVolume = 1f;
    [SerializeField] private float moveSoundPitch = 1f;

    [Tooltip("決定時の音")]
    [SerializeField] private AudioClip confirmSound;
    [Range(0f, 1f)]
    [SerializeField] private float confirmSoundVolume = 1f;
    [SerializeField] private float confirmSoundPitch = 1f;
    [Tooltip("決定音が鳴り終わった後、さらにこの秒数待ってからシーン切り替えする(余韻を残したい場合に伸ばす)")]
    [SerializeField] private float confirmSoundExtraDelay = 0f;

    [Header("BGM")]
    [Tooltip("ゲームオーバー時にフェードアウトして止めたいBGM。複数登録できる(例: 道中BGMとボス戦BGMのイントロ/ループなど)")]
    [SerializeField] private AudioSource[] bgmSources;
    [SerializeField] private float bgmFadeOutDuration = 1.0f;

    [Header("ハイスコア")]
    [Tooltip("今回ハイスコアを更新した時だけ表示する「Hi Score!」などのオブジェクト(任意)")]
    [SerializeField] private GameObject newRecordObject;

    [SerializeField] private string titleSceneName = "Title";

    private Coroutine hitFlashCoroutine;
    private bool isSelecting = false;
    private int selectedIndex = 0;
    private float[] bgmOriginalVolumes;

    private void Update()
    {
        if (!isSelecting) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            selectedIndex = 1 - selectedIndex;
            UpdateSelectionVisual();
            PlayOneShotSound(moveSound, moveSoundVolume, moveSoundPitch);
        }

        if (Keyboard.current.zKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
        {
            isSelecting = false; // 連打で二重に決定処理が走らないようにする
            StartCoroutine(ConfirmRoutine());
        }
    }

    /// <summary>
    /// 決定音を最後まで(+余韻分)鳴らしてから、実際の決定処理(シーン切り替え)を行う。
    /// これをしないと、シーン切り替えで即座にオブジェクトが消え、音が途中で切れてしまう。
    /// 音そのものをもっと長くしたい場合は、Cubase側でタイムストレッチして
    /// 長い音源を作り、それをConfirm Soundに設定する。
    /// </summary>
    private IEnumerator ConfirmRoutine()
    {
        float clipDuration = PlayConfirmSound();

        yield return new WaitForSecondsRealtime(clipDuration + confirmSoundExtraDelay);

        ConfirmSelection();
    }

    /// <summary>
    /// 決定音を再生する。一時的なAudioSourceを生成して鳴らすので、
    /// あらかじめシーンやプレハブにAudioSourceを用意しておく必要がない。
    /// 実際に再生した長さ(秒)を返す。
    /// </summary>
    private float PlayConfirmSound()
    {
        if (confirmSound == null) return 0f;

        GameObject temp = new GameObject("ConfirmSE");
        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = confirmSound;
        src.volume = confirmSoundVolume;
        src.pitch = confirmSoundPitch;
        src.spatialBlend = 0f; // 2Dサウンド
        src.Play();

        float safePitch = Mathf.Max(0.01f, Mathf.Abs(confirmSoundPitch));
        float duration = confirmSound.length / safePitch;

        Destroy(temp, duration);

        return duration;
    }

    /// <summary>
    /// SEを再生する共通処理。一時的なAudioSourceを生成して鳴らすので、
    /// あらかじめシーンやプレハブにAudioSourceを用意しておく必要がない。
    /// </summary>
    private void PlayOneShotSound(AudioClip clip, float volume, float pitch)
    {
        if (clip == null) return;

        GameObject temp = new GameObject("MenuSE");
        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.spatialBlend = 0f; // 2Dサウンド
        src.Play();

        float safePitch = Mathf.Max(0.01f, Mathf.Abs(pitch));
        Destroy(temp, clip.length / safePitch);
    }

    // ---- ゲームオーバー(残機0) ----
    public void ShowGameOver()
    {
        StartCoroutine(DelayedShowGameOver());

        // 死亡時点のスコアを表示する
        if (finalScoreText != null && ScoreManager.Instance != null)
        {
            finalScoreText.text = ScoreManager.Instance.score.ToString("D7");
        }

        // 死亡時点のスコアでもハイスコア更新チェックを行う
        // (更新されればScoreManager側のHighScoreテキストも自動的に反映される)
        if (ScoreManager.Instance != null)
        {
            bool isNewRecord = ScoreManager.Instance.TryUpdateHighScore();
            if (newRecordObject != null)
            {
                newRecordObject.SetActive(isNewRecord);
            }

            // unityroomのスコアランキングに送信する(ボードNo1、ハイスコア降順)
            if (UnityroomApiClient.Instance != null)
            {
                UnityroomApiClient.Instance.SendScore(1, ScoreManager.Instance.score, ScoreboardWriteMode.HighScoreDesc);
            }
        }

        if (bgmSources != null && bgmSources.Length > 0)
        {
            bgmOriginalVolumes = new float[bgmSources.Length];
            for (int i = 0; i < bgmSources.Length; i++)
            {
                bgmOriginalVolumes[i] = (bgmSources[i] != null) ? bgmSources[i].volume : 1f;
            }
            StartCoroutine(FadeOutBGM());
        }
    }

    private IEnumerator FadeOutBGM()
    {
        float t = 0f;

        while (t < bgmFadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float ratio = t / bgmFadeOutDuration;

            for (int i = 0; i < bgmSources.Length; i++)
            {
                if (bgmSources[i] == null) continue;
                bgmSources[i].volume = Mathf.Lerp(bgmOriginalVolumes[i], 0f, ratio);
            }

            yield return null;
        }

        for (int i = 0; i < bgmSources.Length; i++)
        {
            if (bgmSources[i] == null) continue;
            bgmSources[i].volume = 0f;
            bgmSources[i].Stop();
        }
    }

    private IEnumerator DelayedShowGameOver()
    {
        yield return new WaitForSeconds(gameOverDelay);

        Time.timeScale = 0f;
        gameOverPanel.gameObject.SetActive(true);
        selectedIndex = 0;
        UpdateSelectionVisual();
        StartCoroutine(FadeGameOver());
    }

    private IEnumerator FadeGameOver()
    {
        float t = 0f;
        gameOverPanel.blocksRaycasts = true;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            gameOverPanel.alpha = Mathf.Lerp(0f, gameOverMaxAlpha, t / fadeDuration);
            yield return null;
        }

        gameOverPanel.alpha = gameOverMaxAlpha;
        isSelecting = true;
    }

    private void UpdateSelectionVisual()
    {
        restartText.color = (selectedIndex == 0) ? selectedColor : normalColor;
        titleText.color = (selectedIndex == 1) ? selectedColor : normalColor;

        if (cursorIcon != null)
        {
            RectTransform target = (selectedIndex == 0) ? restartCursorPos : titleCursorPos;
            if (target != null)
            {
                cursorIcon.position = target.position;
            }
        }
    }

    private void ConfirmSelection()
    {
        isSelecting = false;

        if (selectedIndex == 0)
        {
            OnRestartButton();
        }
        else
        {
            OnTitleButton();
        }
    }

    public void OnRestartButton()
    {
        RestoreBgmVolumes();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnTitleButton()
    {
        RestoreBgmVolumes();
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }

    private void RestoreBgmVolumes()
    {
        if (bgmSources == null || bgmOriginalVolumes == null) return;

        for (int i = 0; i < bgmSources.Length; i++)
        {
            if (bgmSources[i] != null)
            {
                bgmSources[i].volume = bgmOriginalVolumes[i];
            }
        }
    }

    // ---- 被弾(残機はまだ残っている) ----
    public void FlashHit()
    {
        if (hitFlashPanel == null) return;

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
        }
        hitFlashCoroutine = StartCoroutine(FlashHitRoutine());
    }

    private IEnumerator FlashHitRoutine()
    {
        float t = 0f;
        while (t < hitFlashInDuration)
        {
            t += Time.unscaledDeltaTime;
            hitFlashPanel.alpha = Mathf.Lerp(0f, hitFlashMaxAlpha, t / hitFlashInDuration);
            yield return null;
        }
        hitFlashPanel.alpha = hitFlashMaxAlpha;

        t = 0f;
        while (t < hitFlashOutDuration)
        {
            t += Time.unscaledDeltaTime;
            hitFlashPanel.alpha = Mathf.Lerp(hitFlashMaxAlpha, 0f, t / hitFlashOutDuration);
            yield return null;
        }
        hitFlashPanel.alpha = 0f;
        hitFlashCoroutine = null;
    }
}