using UnityEngine;
using TMPro;
using System.Collections;

public class StageIntroDisplay : MonoBehaviour
{
    [Header("Stage Title")]
    public CanvasGroup stageTitleGroup;
    public TextMeshProUGUI stageTitleText;
    public TextMeshProUGUI stageSubtitleText;
    public string stageTitle = "STAGE 1";
    public string stageSubtitle = "ステンドグラス回廊";

    [Header("Stage Title Timing")]
    public float titleStartDelay = 2.0f;
    public float titleFadeIn = 1.0f;
    public float titleHold = 2.0f;
    public float titleFadeOut = 1.0f;

    [Header("Road Song Name (道中曲)")]
    public CanvasGroup songNameGroup;
    public TextMeshProUGUI songNameText;
    public string songName = "曲名をここに";
    [Range(0f, 1f)] public float songTargetAlpha = 1f;

    [Header("Road Song Name Timing")]
    public float songStartDelay = 0f;
    public float songFadeIn = 1.0f;
    public float songHold = 4.0f;
    public float songFadeOut = 1.0f;

    [Header("Boss Song Name (ボス曲) - 別UI/別フォント/別位置用")]
    public CanvasGroup bossSongNameGroup;
    public TextMeshProUGUI bossSongNameText;
    [Range(0f, 1f)] public float bossSongTargetAlpha = 1f;

    [Header("Boss Song Name Timing")]
    public float bossSongStartDelay = 0f;
    public float bossSongFadeIn = 1.0f;
    public float bossSongHold = 4.0f;
    public float bossSongFadeOut = 1.0f;

    private Coroutine songNameRoutine;
    private Coroutine bossSongNameRoutine;

    void Start()
    {
        stageTitleText.text = stageTitle;
        stageSubtitleText.text = stageSubtitle;

        stageTitleGroup.alpha = 0f;
        songNameGroup.alpha = 0f;

        if (bossSongNameGroup != null)
        {
            bossSongNameGroup.alpha = 0f;
        }

        StartCoroutine(PlayTitleSequence());

        // 道中の曲名は今まで通りStart直後に自動表示
        PlaySongName(songName);
    }

    IEnumerator PlayTitleSequence()
    {
        yield return new WaitForSeconds(titleStartDelay);
        yield return StartCoroutine(FadeCanvasGroup(stageTitleGroup, 0f, 1f, titleFadeIn));
        yield return new WaitForSeconds(titleHold);
        yield return StartCoroutine(FadeCanvasGroup(stageTitleGroup, 1f, 0f, titleFadeOut));
    }

    /// <summary>
    /// 道中曲名の表示(既存の見た目・位置・フォントを使う)。
    /// </summary>
    public void PlaySongName(string name)
    {
        songNameText.text = name;

        if (songNameRoutine != null)
        {
            StopCoroutine(songNameRoutine);
        }
        songNameGroup.alpha = 0f;

        songNameRoutine = StartCoroutine(PlaySongNameSequence());
    }

    IEnumerator PlaySongNameSequence()
    {
        yield return new WaitForSeconds(songStartDelay);
        yield return StartCoroutine(FadeCanvasGroup(songNameGroup, 0f, songTargetAlpha, songFadeIn));
        yield return new WaitForSeconds(songHold);
        yield return StartCoroutine(FadeCanvasGroup(songNameGroup, songTargetAlpha, 0f, songFadeOut));
    }

    /// <summary>
    /// ボス曲名の表示。専用のCanvasGroup/Textを使うので、
    /// Inspector上でフォント・位置(RectTransform)・アルファ・タイミングを道中曲と別々に設定できる。
    /// </summary>
    public void PlayBossSongName(string name)
    {
        if (bossSongNameGroup == null || bossSongNameText == null)
        {
            Debug.LogWarning("[StageIntroDisplay] bossSongNameGroup/Text が設定されていません。");
            return;
        }

        bossSongNameText.text = name;

        if (bossSongNameRoutine != null)
        {
            StopCoroutine(bossSongNameRoutine);
        }
        bossSongNameGroup.alpha = 0f;

        bossSongNameRoutine = StartCoroutine(PlayBossSongNameSequence());
    }

    IEnumerator PlayBossSongNameSequence()
    {
        yield return new WaitForSeconds(bossSongStartDelay);
        yield return StartCoroutine(FadeCanvasGroup(bossSongNameGroup, 0f, bossSongTargetAlpha, bossSongFadeIn));
        yield return new WaitForSeconds(bossSongHold);
        yield return StartCoroutine(FadeCanvasGroup(bossSongNameGroup, bossSongTargetAlpha, 0f, bossSongFadeOut));
    }

    IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }
}