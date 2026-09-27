using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using unityroom.Api;

/// <summary>
/// ボス撃破後の流れを管理する。
/// 
/// 1. 撃破後、少し間を置く
/// 2. 指定したスクリプト(シューティング、ボムなど)を無効化して操作を止める
/// 3. さらに少し間を置いてから、リザルト用のテキストをフェード+浮遊アニメーションで表示する
/// 4. 少し間を置いてから「Press Z Button」を表示し、押したらタイトルシーンへ戻る
/// 
/// シーン上に配置して使う(ボスはプレハブから生成されるため、こちらは常設のオブジェクトにする)。
/// BossSequenceManager側から、ボス撃破時にHandleBossDefeated()を呼んでもらう想定。
/// </summary>
public class ResultDisplayController : MonoBehaviour
{
    [Header("撃破後、入力を止めるまでの間")]
    public float delayBeforeLockInput = 1.0f;

    [Header("無効化する入力スクリプト(シューティング、ボムなど)")]
    [Tooltip("ここに登録したスクリプトが、撃破後に自動でenabled=falseになる")]
    public MonoBehaviour[] inputScriptsToDisable;

    [Header("結果テキスト")]
    [Tooltip("フェードイン対象のCanvasGroup")]
    public CanvasGroup resultTextGroup;
    [Tooltip("浮かび上がる動きをさせたい場合のRectTransform(省略可、フェードのみでもよい)")]
    public RectTransform resultTextTransform;
    [Tooltip("入力を止めてから、テキストが出始めるまでの間")]
    public float delayBeforeTextAppear = 0.3f;
    [Tooltip("どれだけ下から浮かび上がってくるか(ピクセル)")]
    public float floatUpDistance = 30f;
    [Tooltip("フェード+浮遊にかかる時間")]
    public float fadeInDuration = 1.0f;

    [Header("ボーナス設定")]
    [Tooltip("初期の最大ボム数(ScoreManager.CalculateClearBonusのmaxBombs引数に渡す)")]
    public int maxBombsForBonus = 9;

    [Header("内訳テキスト・最終スコア(あらかじめ計算前の空欄でよい)")]
    public TextMeshProUGUI noDeathBonusText;
    public TextMeshProUGUI noBombBonusText;
    public TextMeshProUGUI finalScoreText;

    [Header("ハイスコア")]
    [Tooltip("今回ハイスコアを更新した時だけ表示する「NEW RECORD」などのオブジェクト(任意)")]
    public GameObject newRecordObject;

    [Header("タイトルへ戻る「Press Z Button」")]
    [Tooltip("リザルト表示が終わってから、Press Z Buttonが出るまでの間")]
    public float delayBeforePressZAppear = 1.0f;
    [Tooltip("フェードイン対象のCanvasGroup")]
    public CanvasGroup pressZReturnGroup;
    [Tooltip("Press Z Buttonのフェードインにかかる時間")]
    public float pressZFadeInDuration = 0.6f;
    [Tooltip("戻る先のシーン名")]
    public string titleSceneName = "Title";

    [Header("SE(決定音と同じものを設定する想定)")]
    public AudioClip confirmSound;
    [Range(0f, 1f)]
    public float confirmSoundVolume = 1f;
    public float confirmSoundPitch = 1f;

    private bool waitingForReturnInput = false;

    /// <summary>
    /// BossSequenceManagerなど、ボス撃破イベントを持つスクリプトから呼ぶ。
    /// </summary>
    public void HandleBossDefeated()
    {
        Debug.Log("[ResultDisplay] HandleBossDefeated呼び出し");
        StartCoroutine(ResultSequence());
    }

    private IEnumerator ResultSequence()
    {
        Debug.Log("[ResultDisplay] ResultSequence開始");

        // ①撃破後、少し間を置く
        yield return new WaitForSeconds(delayBeforeLockInput);

        Debug.Log("[ResultDisplay] delayBeforeLockInput終了、入力を止めます");

        // ②入力を止める
        foreach (MonoBehaviour script in inputScriptsToDisable)
        {
            if (script != null)
            {
                script.enabled = false;
            }
        }

        // ③ボーナスを計算してスコアに加算する
        CalculateAndApplyBonuses();

        // ④さらに少し間を置いてから、リザルトテキストを表示する
        yield return new WaitForSeconds(delayBeforeTextAppear);

        Debug.Log($"[ResultDisplay] ShowResultTextを呼びます。resultTextGroup={resultTextGroup}");

        yield return ShowResultText();

        Debug.Log("[ResultDisplay] ShowResultText完了");

        // ⑤リザルト表示後、少し間を置いてから「Press Z Button」を表示する
        yield return new WaitForSeconds(delayBeforePressZAppear);
        yield return ShowPressZReturnText();

        // ⑥ここからZ入力待ち(Update側で監視する)
        waitingForReturnInput = true;
    }

    void Update()
    {
        if (!waitingForReturnInput) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.zKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
        {
            waitingForReturnInput = false;
            PlayConfirmSound();
            SceneManager.LoadScene(titleSceneName);
        }
    }

    /// <summary>
    /// SEを再生する。一時的なAudioSourceを生成して鳴らすので、
    /// あらかじめシーンやプレハブにAudioSourceを用意しておく必要がない。
    /// </summary>
    private void PlayConfirmSound()
    {
        if (confirmSound == null) return;

        GameObject temp = new GameObject("ResultConfirmSE");
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

    private IEnumerator ShowPressZReturnText()
    {
        if (pressZReturnGroup == null) yield break;

        pressZReturnGroup.gameObject.SetActive(true);
        pressZReturnGroup.alpha = 0f;

        float t = 0f;
        while (t < pressZFadeInDuration)
        {
            t += Time.deltaTime;
            pressZReturnGroup.alpha = Mathf.Clamp01(t / pressZFadeInDuration);
            yield return null;
        }

        pressZReturnGroup.alpha = 1f;
    }

    /// <summary>
    /// ScoreManagerの残機/ボムボーナス計算を使ってスコアに加算し、
    /// 内訳テキストと最終スコアに反映する。
    /// </summary>
    private void CalculateAndApplyBonuses()
    {
        int bombUsedCount = (PlayerStatus.Instance != null) ? PlayerStatus.Instance.BombUsedCount : 0;
        int livesRemaining = (PlayerLives.Instance != null) ? Mathf.Max(0, PlayerLives.Instance.CurrentLives) : 0;
        int bombsUnused = Mathf.Max(0, maxBombsForBonus - bombUsedCount);

        if (ScoreManager.Instance == null) return;

        long lifeBonus = ScoreManager.Instance.GetNoDeathBonus(livesRemaining);
        long bombBonus = ScoreManager.Instance.GetNoBombBonus(bombUsedCount, maxBombsForBonus);

        // 実際のスコアへの加算は、ScoreManager側の正式な計算(内訳の合計と必ず一致する)を使う
        ScoreManager.Instance.AddClearBonus(livesRemaining, bombUsedCount, maxBombsForBonus);

        if (noDeathBonusText != null)
        {
            noDeathBonusText.text = $"Life Bonus  (Lives Remaining: {livesRemaining})  {lifeBonus}";
        }
        if (noBombBonusText != null)
        {
            noBombBonusText.text = $"Bomb Bonus  (Bombs Unused: {bombsUnused}/{maxBombsForBonus})  {bombBonus}";
        }
        if (finalScoreText != null)
        {
            finalScoreText.text = ScoreManager.Instance.score.ToString("D7");
        }

        // 最終スコアが確定したこのタイミングで、ハイスコアの更新チェックを行う
        // (更新されればScoreManager側のHighScoreテキストも自動的に反映される)
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

    private IEnumerator ShowResultText()
    {
        if (resultTextGroup == null) yield break;

        resultTextGroup.gameObject.SetActive(true);
        resultTextGroup.alpha = 0f;

        Vector2 finalPos = Vector2.zero;
        Vector2 startPos = Vector2.zero;
        bool animatePosition = resultTextTransform != null;

        if (animatePosition)
        {
            finalPos = resultTextTransform.anchoredPosition;
            startPos = finalPos - new Vector2(0f, floatUpDistance);
            resultTextTransform.anchoredPosition = startPos;
        }

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeInDuration);

            resultTextGroup.alpha = p;

            if (animatePosition)
            {
                resultTextTransform.anchoredPosition = Vector2.Lerp(startPos, finalPos, p);
            }

            yield return null;
        }

        resultTextGroup.alpha = 1f;
        if (animatePosition)
        {
            resultTextTransform.anchoredPosition = finalPos;
        }
    }
}