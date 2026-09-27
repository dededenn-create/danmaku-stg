using UnityEngine;

/// <summary>
/// ボスの弾幕をまとめたスクリプト。以下の弾幕を同時に管理する。
/// 
/// ①偽ビームストリーム: レーザー本体(LineRenderer)は使わず、弾を高速連射することで
/// 「レーザーのような線」に見せる。左右のストリームを3セット登録できる
/// (1・2セット目は左右3本ずつ、3セット目は左右2本ずつ)。
/// 弾のプレハブ・速度・連射間隔・羽ばたきの速さは、セットごとに完全に独立して設定できる。
/// 各ストリームの設定はシンプルに3つ: baseAngle(最初に飛ぶ方向)、swayAngle(振れ幅)、startDelay(動き出す遅延)。
/// 
/// ②自機狙い弾: プレイヤー方向へ一定間隔で弾を発射する、シンプルな狙い撃ち弾。1・2セット独立、2セット目は遅延開始も可能。
/// 
/// ③ばらまき弾: 一定間隔で、指定した角度範囲(全方位も可)にまとめて弾をばらまく。
/// 
/// ④体力連動の弾数・頻度変化: 同じGameObject(または親)にあるBossControllerのHP割合を監視し、
/// フェーズ内のHPが一定割合を下回ったら、ばらまき弾・自機狙い弾それぞれの「発射数」と「発射間隔」を
/// Tier2, Tier3の2段階で、倍率ではなく実際の数値として個別に指定できる。
/// セットごとにこの体力連動を適用するかどうかも選べる。
/// 
/// 弾のプレハブにはEnemyBulletコンポーネント(SetVelocity(Vector2)を持つ)が必要。
/// BossPhase1Bulletで使っているものと同じ弾プレハブを流用できる。
/// 
/// このスクリプトをボスにアタッチするだけで動く(手動セットアップ不要)。
/// </summary>
public class BossWingBulletStream : MonoBehaviour
{
    /// <summary>
    /// ストリーム1本分の設定。
    /// </summary>
    [System.Serializable]
    public class StreamSlot
    {
        [Tooltip("このストリームの名前(見分け用、動作には影響しない)")]
        public string slotName = "Stream";
        [Tooltip("弾が生成される場所(ボス中心からの相対オフセット)。真ん中から出したいなら(0,0)のままでよい")]
        public Vector2 originOffset = Vector2.zero;
        [Tooltip("最初に飛ぶ方向(度)。0=水平右、90=真上、180=水平左、270=真下")]
        public float baseAngle = 180f;
        [Tooltip("baseAngleを中心に、上下(左右)にどれだけ振れるか(度)")]
        public float swayAngle = 40f;
        [Tooltip("このストリームが動き出すまでの遅延時間(秒)")]
        public float startDelay = 0f;

        [System.NonSerialized] public float fireTimer = 0f;
    }

    [Header("ストリーム(左右それぞれ好きな本数を登録)")]
    public StreamSlot[] leftStreams = new StreamSlot[]
    {
        new StreamSlot { slotName = "Left1", baseAngle = 180f, swayAngle = 30f, startDelay = 0f },
        new StreamSlot { slotName = "Left2", baseAngle = 180f, swayAngle = 40f, startDelay = 0.2f },
        new StreamSlot { slotName = "Left3", baseAngle = 180f, swayAngle = 50f, startDelay = 0.4f },
    };
    public StreamSlot[] rightStreams = new StreamSlot[]
    {
        new StreamSlot { slotName = "Right1", baseAngle = 0f, swayAngle = 30f, startDelay = 0f },
        new StreamSlot { slotName = "Right2", baseAngle = 0f, swayAngle = 40f, startDelay = 0.2f },
        new StreamSlot { slotName = "Right3", baseAngle = 0f, swayAngle = 50f, startDelay = 0.4f },
    };

    [Header("ストリーム2セット目(もう1組、同じ仕組みで追加)")]
    public StreamSlot[] leftStreams2 = new StreamSlot[]
    {
        new StreamSlot { slotName = "Left2-1", baseAngle = 180f, swayAngle = 30f, startDelay = 0f },
        new StreamSlot { slotName = "Left2-2", baseAngle = 180f, swayAngle = 40f, startDelay = 0.2f },
        new StreamSlot { slotName = "Left2-3", baseAngle = 180f, swayAngle = 50f, startDelay = 0.4f },
    };
    public StreamSlot[] rightStreams2 = new StreamSlot[]
    {
        new StreamSlot { slotName = "Right2-1", baseAngle = 0f, swayAngle = 30f, startDelay = 0f },
        new StreamSlot { slotName = "Right2-2", baseAngle = 0f, swayAngle = 40f, startDelay = 0.2f },
        new StreamSlot { slotName = "Right2-3", baseAngle = 0f, swayAngle = 50f, startDelay = 0.4f },
    };

    [Header("ストリーム3セット目(左右2本ずつ、計4本)")]
    public StreamSlot[] leftStreams3 = new StreamSlot[]
    {
        new StreamSlot { slotName = "Left3-1", baseAngle = 180f, swayAngle = 30f, startDelay = 0f },
        new StreamSlot { slotName = "Left3-2", baseAngle = 180f, swayAngle = 45f, startDelay = 0.2f },
    };
    public StreamSlot[] rightStreams3 = new StreamSlot[]
    {
        new StreamSlot { slotName = "Right3-1", baseAngle = 0f, swayAngle = 30f, startDelay = 0f },
        new StreamSlot { slotName = "Right3-2", baseAngle = 0f, swayAngle = 45f, startDelay = 0.2f },
    };

    [Header("弾(1セット目)")]
    [Tooltip("EnemyBulletコンポーネントが付いた弾のプレハブ")]
    public GameObject bulletPrefab;
    [Tooltip("弾の速度")]
    public float bulletSpeed = 10f;
    [Tooltip("弾を撃つ間隔(秒)。短いほど線が途切れずレーザーっぽく見える")]
    public float fireInterval = 0.03f;
    [Tooltip("羽ばたきの速さ(大きいほど速く動く)")]
    public float flapSpeed = 2f;

    [Header("弾(2セット目)")]
    [Tooltip("EnemyBulletコンポーネントが付いた弾のプレハブ")]
    public GameObject bulletPrefab2;
    [Tooltip("弾の速度")]
    public float bulletSpeed2 = 10f;
    [Tooltip("弾を撃つ間隔(秒)。短いほど線が途切れずレーザーっぽく見える")]
    public float fireInterval2 = 0.03f;
    [Tooltip("羽ばたきの速さ(大きいほど速く動く)")]
    public float flapSpeed2 = 2f;

    [Header("弾(3セット目)")]
    [Tooltip("EnemyBulletコンポーネントが付いた弾のプレハブ")]
    public GameObject bulletPrefab3;
    [Tooltip("弾の速度")]
    public float bulletSpeed3 = 10f;
    [Tooltip("弾を撃つ間隔(秒)。短いほど線が途切れずレーザーっぽく見える")]
    public float fireInterval3 = 0.03f;
    [Tooltip("羽ばたきの速さ(大きいほど速く動く)")]
    public float flapSpeed3 = 2f;

    [Header("自機狙い弾(1セット目) - 基本(HP100%〜Tier2閾値まで)")]
    [Tooltip("EnemyBulletコンポーネントが付いた弾のプレハブ")]
    public GameObject aimedBulletPrefab;
    [Tooltip("弾の速度")]
    public float aimedBulletSpeed = 6f;
    [Tooltip("弾を撃つ間隔(秒)")]
    public float aimedFireInterval = 0.5f;
    [Tooltip("1回の発射で何発同時に撃つか(2発以上ならaimedShotSpreadAngleで広げる)")]
    public int aimedShotCount = 1;
    [Tooltip("aimedShotCountが2以上の時、扇状に広げる全体の角度(度)")]
    public float aimedShotSpreadAngle = 20f;
    [Tooltip("弾が生成される場所(ボス中心からの相対オフセット)")]
    public Vector2 aimedOriginOffset = Vector2.zero;
    [Tooltip("ONにすると、下記Tier2/Tier3の数値がこのセットに適用される。OFFなら常にこの基本値のまま変化しない")]
    public bool applyHpScalingToAimedSet1 = true;

    [Header("自機狙い弾(1セット目) - Tier2/Tier3個別設定")]
    [Tooltip("Tier2(HPがhpThresholdTier2を下回った時)の発射数")]
    public int aimedShotCountTier2 = 3;
    [Tooltip("Tier2の発射間隔(秒)")]
    public float aimedFireIntervalTier2 = 0.4f;
    [Tooltip("Tier3(HPがhpThresholdTier3を下回った時)の発射数")]
    public int aimedShotCountTier3 = 5;
    [Tooltip("Tier3の発射間隔(秒)")]
    public float aimedFireIntervalTier3 = 0.3f;

    [Header("自機狙い弾(2セット目) - 基本(HP100%〜Tier2閾値まで)")]
    [Tooltip("EnemyBulletコンポーネントが付いた弾のプレハブ")]
    public GameObject aimedBulletPrefab2;
    [Tooltip("弾の速度")]
    public float aimedBulletSpeed2 = 6f;
    [Tooltip("弾を撃つ間隔(秒)")]
    public float aimedFireInterval2 = 0.5f;
    [Tooltip("1回の発射で何発同時に撃つか(2発以上ならaimedShotSpreadAngle2で広げる)")]
    public int aimedShotCount2 = 1;
    [Tooltip("aimedShotCount2が2以上の時、扇状に広げる全体の角度(度)")]
    public float aimedShotSpreadAngle2 = 20f;
    [Tooltip("弾が生成される場所(ボス中心からの相対オフセット)")]
    public Vector2 aimedOriginOffset2 = Vector2.zero;
    [Tooltip("このセットが撃ち始めるまでの遅延時間(秒)")]
    public float aimedStartDelay2 = 0.25f;
    [Tooltip("ONにすると、下記Tier2/Tier3の数値がこのセットに適用される。OFFなら常にこの基本値のまま変化しない")]
    public bool applyHpScalingToAimedSet2 = true;

    [Header("自機狙い弾(2セット目) - Tier2/Tier3個別設定")]
    public int aimedShotCount2Tier2 = 3;
    public float aimedFireInterval2Tier2 = 0.4f;
    public int aimedShotCount2Tier3 = 5;
    public float aimedFireInterval2Tier3 = 0.3f;

    [Header("ばらまき弾 - 基本(HP100%〜Tier2閾値まで)")]
    [Tooltip("EnemyBulletコンポーネントが付いた弾のプレハブ")]
    public GameObject scatterBulletPrefab;
    [Tooltip("弾の速度")]
    public float scatterBulletSpeed = 5f;
    [Tooltip("何秒おきに1回ばらまくか")]
    public float scatterInterval = 1.2f;
    [Tooltip("1回のばらまきで何発出すか")]
    public int scatterCountPerWave = 16;
    [Tooltip("ばらまく範囲の開始角度(度)。0=水平右")]
    public float scatterMinAngle = 0f;
    [Tooltip("ばらまく範囲の終了角度(度)。0〜360にすると全方位")]
    public float scatterMaxAngle = 360f;
    [Tooltip("弾が生成される場所(ボス中心からの相対オフセット)")]
    public Vector2 scatterOriginOffset = Vector2.zero;
    [Tooltip("1回ばらまくごとに、次の角度をどれだけ回転させるか(度)。0なら回転しない、螺旋にしたいならここを調整")]
    public float scatterRotationPerWave = 15f;
    [Tooltip("ONにすると、下記Tier2/Tier3の数値がこのセットに適用される。OFFなら常にこの基本値のまま変化しない")]
    public bool applyHpScalingToScatter = true;

    [Header("ばらまき弾 - Tier2/Tier3個別設定")]
    [Tooltip("Tier2の1回あたりの発射数")]
    public int scatterCountTier2 = 24;
    [Tooltip("Tier2のばらまき間隔(秒)")]
    public float scatterIntervalTier2 = 0.9f;
    [Tooltip("Tier3の1回あたりの発射数")]
    public int scatterCountTier3 = 32;
    [Tooltip("Tier3のばらまき間隔(秒)")]
    public float scatterIntervalTier3 = 0.6f;

    [Header("体力の閾値(全セット共通)")]
    [Tooltip("このフェーズのHPがこの割合を下回ったらTier2に切り替わる(0〜1)")]
    [Range(0f, 1f)]
    public float hpThresholdTier2 = 0.5f;
    [Tooltip("このフェーズのHPがこの割合を下回ったらTier3に切り替わる(hpThresholdTier2よりさらに低い値にすること)")]
    [Range(0f, 1f)]
    public float hpThresholdTier3 = 0.25f;

    private enum HpTier { Normal, Tier2, Tier3 }
    private HpTier currentHpTier = HpTier.Normal;
    private BossController bossController;

    private float scatterTimer = 0f;
    private float scatterRotationOffset = 0f;

    private Transform playerTransform;
    private float aimedFireTimer = 0f;
    private float aimedFireTimer2 = 0f;
    private float aimedElapsedTime2 = 0f;

    void OnEnable()
    {
        foreach (StreamSlot slot in leftStreams) slot.fireTimer = 0f;
        foreach (StreamSlot slot in rightStreams) slot.fireTimer = 0f;
        foreach (StreamSlot slot in leftStreams2) slot.fireTimer = 0f;
        foreach (StreamSlot slot in rightStreams2) slot.fireTimer = 0f;
        foreach (StreamSlot slot in leftStreams3) slot.fireTimer = 0f;
        foreach (StreamSlot slot in rightStreams3) slot.fireTimer = 0f;

        FindPlayer();
        aimedFireTimer = 0f;
        aimedFireTimer2 = 0f;
        aimedElapsedTime2 = 0f;
        scatterTimer = 0f;
        scatterRotationOffset = 0f;
        currentHpTier = HpTier.Normal;

        // HP割合の変化を購読して、Tierを切り替える
        bossController = GetComponent<BossController>();
        if (bossController == null)
        {
            bossController = GetComponentInParent<BossController>();
        }
        if (bossController != null)
        {
            bossController.onDisplayHPChanged += HandleHPChanged;
        }
        else
        {
            Debug.LogWarning("BossWingBulletStream: 同じGameObject(または親)にBossControllerが見つかりません。体力連動の変化が無効になります。");
        }
    }

    void OnDisable()
    {
        if (bossController != null)
        {
            bossController.onDisplayHPChanged -= HandleHPChanged;
        }
    }

    private void HandleHPChanged(float hpFraction)
    {
        if (hpFraction <= hpThresholdTier3)
        {
            currentHpTier = HpTier.Tier3;
        }
        else if (hpFraction <= hpThresholdTier2)
        {
            currentHpTier = HpTier.Tier2;
        }
        else
        {
            currentHpTier = HpTier.Normal;
        }
    }

    /// <summary>
    /// applyがfalse、またはNormalの時は基本値を、Tier2/Tier3の時はそれぞれ指定した値を返す。
    /// 倍率計算ではなく、指定した実数値をそのまま使う。
    /// </summary>
    private void ResolveTierValues(
        bool apply,
        int baseCount, float baseInterval,
        int tier2Count, float tier2Interval,
        int tier3Count, float tier3Interval,
        out int count, out float interval)
    {
        if (!apply || currentHpTier == HpTier.Normal)
        {
            count = baseCount;
            interval = baseInterval;
            return;
        }

        if (currentHpTier == HpTier.Tier2)
        {
            count = tier2Count;
            interval = tier2Interval;
            return;
        }

        count = tier3Count;
        interval = tier3Interval;
    }

    private void FindPlayer()
    {
        if (playerTransform != null) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
        else Debug.LogWarning("BossWingBulletStream: PlayerタグのGameObjectが見つかりません(自機狙い弾に必要)。");
    }

    void Update()
    {
        UpdateStreams(leftStreams, Time.time, 1f, bulletPrefab, bulletSpeed, fireInterval, flapSpeed);
        UpdateStreams(rightStreams, Time.time, -1f, bulletPrefab, bulletSpeed, fireInterval, flapSpeed);
        UpdateStreams(leftStreams2, Time.time, 1f, bulletPrefab2, bulletSpeed2, fireInterval2, flapSpeed2);
        UpdateStreams(rightStreams2, Time.time, -1f, bulletPrefab2, bulletSpeed2, fireInterval2, flapSpeed2);
        UpdateStreams(leftStreams3, Time.time, 1f, bulletPrefab3, bulletSpeed3, fireInterval3, flapSpeed3);
        UpdateStreams(rightStreams3, Time.time, -1f, bulletPrefab3, bulletSpeed3, fireInterval3, flapSpeed3);

        ResolveTierValues(
            applyHpScalingToAimedSet1,
            aimedShotCount, aimedFireInterval,
            aimedShotCountTier2, aimedFireIntervalTier2,
            aimedShotCountTier3, aimedFireIntervalTier3,
            out int resolvedCount1, out float resolvedInterval1);
        UpdateAimedShot(ref aimedFireTimer, resolvedInterval1, aimedOriginOffset, resolvedCount1, aimedShotSpreadAngle, aimedBulletPrefab, aimedBulletSpeed);

        aimedElapsedTime2 += Time.deltaTime;
        if (aimedElapsedTime2 >= aimedStartDelay2)
        {
            ResolveTierValues(
                applyHpScalingToAimedSet2,
                aimedShotCount2, aimedFireInterval2,
                aimedShotCount2Tier2, aimedFireInterval2Tier2,
                aimedShotCount2Tier3, aimedFireInterval2Tier3,
                out int resolvedCount2, out float resolvedInterval2);
            UpdateAimedShot(ref aimedFireTimer2, resolvedInterval2, aimedOriginOffset2, resolvedCount2, aimedShotSpreadAngle2, aimedBulletPrefab2, aimedBulletSpeed2);
        }

        UpdateScatterShot();
    }

    /// <summary>
    /// 一定間隔で、指定した角度範囲にまとめて弾をばらまく。
    /// scatterRotationPerWaveが0でなければ、ばらまくたびに角度が少しずつ回転していく(螺旋状)。
    /// </summary>
    private void UpdateScatterShot()
    {
        ResolveTierValues(
            applyHpScalingToScatter,
            scatterCountPerWave, scatterInterval,
            scatterCountTier2, scatterIntervalTier2,
            scatterCountTier3, scatterIntervalTier3,
            out int count, out float interval);

        scatterTimer += Time.deltaTime;
        if (scatterTimer < interval) return;
        scatterTimer = 0f;

        if (count <= 0) return;

        Vector2 origin = (Vector2)transform.position + scatterOriginOffset;
        float angleRange = scatterMaxAngle - scatterMinAngle;

        // 全方位(360度)の場合は最後の1発が最初と重ならないよう均等割りする
        bool isFullCircle = Mathf.Approximately(Mathf.Abs(angleRange), 360f);
        float angleStep = isFullCircle
            ? angleRange / count
            : (count > 1 ? angleRange / (count - 1) : 0f);

        for (int i = 0; i < count; i++)
        {
            float angle = scatterMinAngle + scatterRotationOffset + angleStep * i;
            FireBullet(origin, DirectionFromAngle(angle), scatterBulletPrefab, scatterBulletSpeed);
        }

        // 次のばらまきに向けて、角度を少しずつ回転させる(螺旋状に見える)
        scatterRotationOffset += scatterRotationPerWave;
    }

    /// <summary>
    /// プレイヤーを狙う弾を、一定間隔で発射する。1セット目・2セット目どちらもこれを使う。
    /// fireIntervalとshotCountは、呼び出し側でResolveTierValues()により解決済みの値を渡す。
    /// </summary>
    private void UpdateAimedShot(ref float fireTimer, float fireInterval, Vector2 originOffset, int shotCount, float shotSpreadAngle, GameObject prefab, float speed)
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        fireTimer += Time.deltaTime;
        if (fireTimer < fireInterval) return;
        fireTimer = 0f;

        Vector2 origin = (Vector2)transform.position + originOffset;
        Vector2 toPlayer = (Vector2)playerTransform.position - origin;
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        if (shotCount <= 1)
        {
            FireBullet(origin, DirectionFromAngle(baseAngle), prefab, speed);
            return;
        }

        // 複数発の場合は、プレイヤー方向を中心に扇状に広げる
        float startAngle = baseAngle - shotSpreadAngle / 2f;
        float angleStep = shotCount > 1 ? shotSpreadAngle / (shotCount - 1) : 0f;

        for (int i = 0; i < shotCount; i++)
        {
            float angle = startAngle + angleStep * i;
            FireBullet(origin, DirectionFromAngle(angle), prefab, speed);
        }
    }

    private Vector2 DirectionFromAngle(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    /// <summary>
    /// sway01Sign: 振れの符号。左右で「内側に来るタイミング」を揃えるために、
    /// 右側だけ-1にして反転させている(そうしないと片方が内側の時もう片方が外側になってしまう)。
    /// </summary>
    private void UpdateStreams(StreamSlot[] slots, float currentTime, float sway01Sign, GameObject prefab, float speed, float interval, float flapSpd)
    {
        foreach (StreamSlot slot in slots)
        {
            float t = currentTime - slot.startDelay;
            if (t < 0f) continue; // まだ動き出す時間になっていない: 何も撃たない

            slot.fireTimer += Time.deltaTime;
            if (slot.fireTimer < interval) continue;
            slot.fireTimer = 0f;

            float angle = slot.baseAngle + sway01Sign * Mathf.Sin(t * flapSpd) * slot.swayAngle;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            Vector2 origin = (Vector2)transform.position + slot.originOffset;

            FireBullet(origin, direction, prefab, speed);
        }
    }

    private void FireBullet(Vector2 origin, Vector2 direction, GameObject prefab, float speed)
    {
        if (prefab == null) return;

        GameObject bulletObj = Instantiate(prefab, origin, Quaternion.identity);
        EnemyBullet eb = bulletObj.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            eb.SetVelocity(direction * speed);
        }
    }
}