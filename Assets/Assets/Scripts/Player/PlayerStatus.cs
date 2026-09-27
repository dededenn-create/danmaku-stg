using System;
using UnityEngine;
using TMPro;

public class PlayerStatus : MonoBehaviour
{
    public static PlayerStatus Instance;

    public int power = 0;
    public int maxPower = 135;
    public int[] powerThresholds = { 0, 15, 55, 135 };

    public int ShotLevel { get; private set; } = 1;
    public int minPowerAfterHit = 0;

    [Header("Bomb")]
    public int bombCount = 3;
    public int maxBombCount = 3;
    public int BombUsedCount { get; private set; } = 0; // 累計ボム使用回数(クリアボーナス計算用)

    [Header("Power MAX演出")]
    public TextMeshProUGUI powerMaxText;
    private float powerMaxTimer = 0f;
    private bool isPowerMaxDisplaying = false;

    public event Action<int> OnBombCountChanged;

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        UpdateShotLevel();
    }

    void Update()
    {
        if (isPowerMaxDisplaying)
        {
            powerMaxTimer -= Time.deltaTime;

            float alpha = Mathf.Clamp01(powerMaxTimer / 1.0f);
            if (powerMaxText != null)
            {
                Color c = powerMaxText.color;
                powerMaxText.color = new Color(c.r, c.g, c.b, alpha);
            }

            if (powerMaxTimer <= 0f)
            {
                isPowerMaxDisplaying = false;
                if (powerMaxText != null) powerMaxText.gameObject.SetActive(false);
            }
        }
    }
    
    public void AddPower(int value)
    {
        bool wasMaxed = IsPowerMaxed();
        power = Mathf.Min(power + value, maxPower);
        UpdateShotLevel();

        if (!wasMaxed && IsPowerMaxed())
        {
            TriggerPowerMaxEffect();
        }
    }

    void TriggerPowerMaxEffect()
    {
        foreach (var bullet in EnemyBullet.activeBullets.ToArray())
        {
            bullet.StartSuckToPlayer(transform);
        }

        ShowPowerMaxText();
    }

    void ShowPowerMaxText()
    {
        if (powerMaxText == null) return;

        powerMaxText.gameObject.SetActive(true);
        powerMaxText.color = new Color(powerMaxText.color.r, powerMaxText.color.g, powerMaxText.color.b, 1.0f);
        powerMaxTimer = 1.5f;
        isPowerMaxDisplaying = true;
    }

    public bool IsPowerMaxed()
    {
        return power >= maxPower;
    }

    void UpdateShotLevel()
    {
        int level = 1;
        for (int i = 0; i < powerThresholds.Length; i++)
        {
            if (power >= powerThresholds[i]) level = i + 1;
        }

        if (level != ShotLevel)
        {
            ShotLevel = level;
            PlayerMove.Instance.OnLevelChanged(ShotLevel);
        }
    }

    public void OnPlayerHit()
    {
        power -=20;
        power = Mathf.Max(power, minPowerAfterHit);
        UpdateShotLevel();

        bombCount = maxBombCount;
        OnBombCountChanged?.Invoke(bombCount);
    }

    public bool UseBomb()
    {
        if (bombCount <= 0) return false;
        bombCount--;
        BombUsedCount++;
        OnBombCountChanged?.Invoke(bombCount);
        return true;
    }

    public int CurrentSegmentNumerator
    {
        get
        {
            for (int i = 0; i < powerThresholds.Length - 1; i++)
            {
                if (power < powerThresholds[i + 1])
                {
                    return power - powerThresholds[i];
                }
            }
            return maxPower - powerThresholds[powerThresholds.Length - 1];
        }
    }

    public int CurrentSegmentDenominator
    {
        get
        {
            for (int i = 0; i < powerThresholds.Length - 1; i++)
            {
                if (power < powerThresholds[i + 1])
                {
                    return powerThresholds[i + 1] - powerThresholds[i];
                }
            }
            return maxPower - powerThresholds[powerThresholds.Length - 1];
        }
    }
}