using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public long score = 0;
    public TextMeshProUGUI scoreText;
    private long displayedScore = 0; // 画面に表示している値(カウントアップ演出用)

    [Header("ハイスコア")]
    private const string HighScoreKey = "HighScore";
    public long HighScore { get; private set; } = 0;
    [Tooltip("Scoreの下などに、常時ハイスコアを表示するテキスト")]
    public TextMeshProUGUI highScoreText;

    [Header("全回収ライン倍率テーブル")]
    public int[] collectThresholds = { 5, 10, 15 };   // 個数の境界
    public float[] collectMultipliers = { 1.0f, 2.0f, 4.0f, 8.0f }; // 0:〜4個, 1:5〜9, 2:10〜14, 3:15以上

    [Header("Collect倍率表示")]
    public TextMeshProUGUI multiplierText;
    private float multiplierResetTimer = 0f;
    private bool isMultiplierDisplaying = false;

    void Awake()
    {
        Instance = this;
        LoadHighScore();
        UpdateHighScoreUI();
    }

    /// <summary>
    /// PlayerPrefsに保存されてるハイスコアを読み込む。
    /// PlayerPrefsはlong型を直接扱えないので、文字列として保存/読み込みする。
    /// </summary>
    private void LoadHighScore()
    {
        string saved = PlayerPrefs.GetString(HighScoreKey, "0");
        if (!long.TryParse(saved, out long loaded))
        {
            loaded = 0;
        }
        HighScore = loaded;
    }

    private void UpdateHighScoreUI()
    {
        if (highScoreText != null)
        {
            highScoreText.text = HighScore.ToString("D7");
        }
    }

    /// <summary>
    /// 現在のscoreがハイスコアを上回っていれば更新して保存する。
    /// 更新された場合はtrueを返す(「NEW RECORD」表示などの判定に使える)。
    /// </summary>
    public bool TryUpdateHighScore()
    {
        if (score > HighScore)
        {
            HighScore = score;
            PlayerPrefs.SetString(HighScoreKey, HighScore.ToString());
            PlayerPrefs.Save();
            UpdateHighScoreUI();
            return true;
        }
        return false;
    }

    void Update()
    {
        // Collect倍率表示のタイマー
        if (isMultiplierDisplaying)
        {
            multiplierResetTimer -= Time.deltaTime;
            if (multiplierResetTimer <= 0f)
            {
                multiplierText.text = "x1.0";
                isMultiplierDisplaying = false;
            }
        }

        // スコアのカウントアップ演出
        if (displayedScore < score)
        {
            long diff = score - displayedScore;
            long increment = System.Math.Max(1, (long)(diff * 0.3f));
            displayedScore += increment;
            if (displayedScore > score) displayedScore = score;

            UpdateScoreUI();
        }
    }

    // 弾ヒット時
    public void AddHitScore(int amount = 750)
    {
        AddScore(amount);
    }

    // 敵撃破時
    public void AddKillScore(int amount)
    {
        AddScore(amount);
    }

    // Powerアイテム取得時(通常拾い or 全回収ライン吸い込み)
    public void AddItemScore(int baseScore, bool isPowerMaxed, bool isFullCollect, int fullCollectCount = 0)
    {
        int value = baseScore * (isPowerMaxed ? 2 : 1);

        if (isFullCollect)
        {
            float multiplier = GetCollectMultiplier(fullCollectCount);
            value = Mathf.RoundToInt(value * multiplier);
        }

        AddScore(value);
    }

    public float GetCollectMultiplier(int count)
    {
        if (count >= collectThresholds[2]) return collectMultipliers[3]; // 15個以上
        if (count >= collectThresholds[1]) return collectMultipliers[2]; // 10〜14
        if (count >= collectThresholds[0]) return collectMultipliers[1]; // 5〜9
        return collectMultipliers[0]; // 〜4個
    }

    // Collect欄に倍率を表示(全回収ライン発動時)
    public void ShowMultiplier(float multiplier)
    {
        if (multiplierText == null) return;

        multiplierText.text = $"x{multiplier:F1}";
        multiplierResetTimer = 1.5f;
        isMultiplierDisplaying = true;
    }

    /// <summary>
    /// ノーデスボーナス部分だけを計算する(内訳表示用に独立させたもの)。
    /// <summary>
    /// 残機ボーナス部分だけを計算する(内訳表示用に独立させたもの)。
    /// 残機1つにつき450,000点。
    /// </summary>
    public long GetNoDeathBonus(int livesRemaining)
    {
        return (long)livesRemaining * 450000;
    }

    /// <summary>
    /// ボムボーナス部分だけを計算する(内訳表示用に独立させたもの)。
    /// 合計maxBombs個使える想定で、900,000点を基準に、使った分だけ1個あたり100,000点引かれる。
    /// 全部使い切ったら0点(マイナスにはならない)。
    /// </summary>
    public long GetNoBombBonus(int bombUsedCount, int maxBombs = 9)
    {
        long perBombPenalty = 100000;
        long bonus = (long)(maxBombs - bombUsedCount) * perBombPenalty;
        return System.Math.Max(0L, bonus);
    }

    // クリア時ボーナス(残機・ボム使用回数から算出)
    public long CalculateClearBonus(int livesRemaining, int bombUsedCount, int maxBombs = 9)
    {
        return GetNoDeathBonus(livesRemaining) + GetNoBombBonus(bombUsedCount, maxBombs);
    }

    public void AddClearBonus(int livesRemaining, int bombUsedCount, int maxBombs = 9)
    {
        AddScore(CalculateClearBonus(livesRemaining, bombUsedCount, maxBombs));
    }

    void AddScore(long amount)
    {
        score += amount;
        // 表示はUpdate()内でdisplayedScoreが自動的に追いつく
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            long roundedScore = (displayedScore / 10) * 10;
            scoreText.text = roundedScore.ToString("D7");
        }
    }
}