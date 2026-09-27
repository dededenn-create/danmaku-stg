using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スペルカード「ばらまき→一斉ホーミング」の管理スクリプト。
/// ボスのGameObjectにアタッチして使う。
/// 
/// 「層(レイヤー)」という概念を導入し、中心・中間・外側など、
/// 半径帯ごとに別の弾プレハブ(速度や加速度が違う弾)を割り当てられる。
/// 層はInspectorで好きなだけ登録でき、登録した順番でループする。
/// (例: 中心 → 中間 → 外側 → 中心 → ...)
/// 
/// ループ(1層あたり):
/// 1. ばらまき相: 敵の周囲、その層のradiusMin〜radiusMaxの範囲(輪っか状)に
///    ランダムな位置を目的地として、その層のプレハブで弾を次々に生成する
/// 2. 溜め相: 少し待つ(演出・SE・色変化などを入れるタイミング)
/// 3. 発射相: その時点で生きている弾を、その瞬間のプレイヤー座標(を少しブレさせた点)へ
///    一斉にロックオンして発射する
/// 4. 休憩相: 次のばらまきまで少し待つ
/// </summary>
public class BossSwarmSpellCard : MonoBehaviour
{
    /// <summary>
    /// 1つの層(レイヤー)の設定。中心用・中間用・外側用など、層ごとに1つ作る。
    /// </summary>
    [System.Serializable]
    public class SwarmLayer
    {
        [Tooltip("この層の名前(見分け用、動作には影響しない)")]
        public string layerName = "Layer";

        [Tooltip("この層で使うSwarmBulletプレハブ。速度や加速度はプレハブ側で個別に設定する")]
        public GameObject bulletPrefab;

        [Tooltip("敵中心からの最小距離(この層の内側の境界)")]
        public float radiusMin = 0f;
        [Tooltip("敵中心からの最大距離(この層の外側の境界)")]
        public float radiusMax = 1f;

        [Tooltip("この層で1回のばらまきに出す弾の数")]
        public int bulletCount = 12;

        [Tooltip("弾が出現してから目的地に到着して止まるまでの時間(秒)")]
        public float bulletMoveDuration = 0.8f;
        [Tooltip("bulletMoveDurationにこの範囲でランダムなブレを加える(0で無効)")]
        public float bulletMoveDurationJitter = 0.2f;
    }

    [Header("参照")]
    [Tooltip("プレイヤーのTransform")]
    public Transform playerTransform;

    [Header("層(中心・中間・外側など、登録した順にループする)")]
    public SwarmLayer[] layers;

    [Header("ばらまき相 共通設定")]
    [Tooltip("弾を1発生成してから次の1発を生成するまでの間隔(秒)")]
    public float scatterInterval = 0.05f;

    [Header("溜め相・休憩相")]
    [Tooltip("ばらまき終了後、発射するまでの溜め時間。bulletMoveDurationより長めにすること")]
    public float chargeDuration = 1.0f;
    [Tooltip("発射後、次のばらまきが始まるまでの休憩時間")]
    public float restDuration = 1.0f;

    [Header("発射方向")]
    [Tooltip("trueなら発射の瞬間のプレイヤー方向を基準にする(狙い撃ちに近くなる)。falseなら常に真下(270度)を基準にする")]
    public bool aimAtPlayerAsBase = true;
    [Tooltip("基準方向からこの角度の範囲でランダムにブレさせる。小さいほど狙い撃ちに近く、大きいほどばらまきに近くなる")]
    public float launchAngleRange = 60f;

    private Coroutine loopCoroutine;
    private bool stopRequested = false;
    private readonly List<SwarmBullet> allSpawnedBullets = new List<SwarmBullet>();

    void OnEnable()
    {
        stopRequested = false;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("BossSwarmSpellCard: PlayerタグのついたGameObjectが見つかりませんでした。playerTransformを手動で設定するか、プレイヤーにPlayerタグを付けてください。");
            }
        }

        loopCoroutine = StartCoroutine(SpellCardLoop());
    }

    void OnDisable()
    {
        // フェーズ切り替えでこのコンポーネントが無効化された時、ループだけ止める(既存の弾は消さない)
        StopSpellCard(false);
    }

    /// <summary>
    /// フェーズ管理システムから呼ぶ。このスペルカードのループを止める。
    /// destroyRemainingBulletsをtrueにすると、画面に残ってる弾(ばらまき中/発射済み問わず)も一緒に消す。
    /// </summary>
    public void StopSpellCard(bool destroyRemainingBullets = false)
    {
        stopRequested = true;

        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }

        if (destroyRemainingBullets)
        {
            foreach (SwarmBullet bullet in allSpawnedBullets)
            {
                if (bullet != null)
                {
                    Destroy(bullet.gameObject);
                }
            }
        }

        allSpawnedBullets.Clear();
    }

    private IEnumerator SpellCardLoop()
    {
        if (layers == null || layers.Length == 0)
        {
            Debug.LogWarning("BossSwarmSpellCard: layersが設定されていません。");
            yield break;
        }

        int layerIndex = 0;

        while (!stopRequested)
        {
            SwarmLayer layer = layers[layerIndex];

            // --- ①ばらまき相 ---
            List<SwarmBullet> currentBatch = new List<SwarmBullet>(layer.bulletCount);

            for (int i = 0; i < layer.bulletCount; i++)
            {
                Vector2 origin = transform.position;
                Vector2 target = origin + RandomPointInRing(layer.radiusMin, layer.radiusMax);

                GameObject obj = Instantiate(layer.bulletPrefab, origin, Quaternion.identity);
                SwarmBullet bullet = obj.GetComponent<SwarmBullet>();

                float duration = layer.bulletMoveDuration + Random.Range(-layer.bulletMoveDurationJitter, layer.bulletMoveDurationJitter);
                duration = Mathf.Max(0.1f, duration); // 0以下防止

                bullet.Init(origin, target, duration);
                currentBatch.Add(bullet);
                allSpawnedBullets.Add(bullet);

                yield return new WaitForSeconds(scatterInterval);
            }

            // --- ②溜め相 ---
            // ここでボスの色を変える、SEを鳴らす等の「来るぞ」演出を入れると避けゲーとして親切になる
            yield return new WaitForSeconds(chargeDuration);

            // --- ③一斉発射相(基準方向 ± 角度のブレで発射) ---
            foreach (SwarmBullet bullet in currentBatch)
            {
                if (bullet != null)
                {
                    float baseAngle;
                    if (aimAtPlayerAsBase && playerTransform != null)
                    {
                        // 発射する瞬間、その弾から見たプレイヤー方向を基準にする
                        Vector2 toPlayer = (Vector2)playerTransform.position - (Vector2)bullet.transform.position;
                        baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
                    }
                    else
                    {
                        baseAngle = 270f; // 真下
                    }

                    float angle = (baseAngle + Random.Range(-launchAngleRange / 2f, launchAngleRange / 2f)) * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    bullet.LaunchInDirection(dir);
                }
            }

            // --- ④休憩相 ---
            yield return new WaitForSeconds(restDuration);

            // 次の層へ(最後まで行ったら最初の層に戻る)
            layerIndex = (layerIndex + 1) % layers.Length;
        }
    }

    /// <summary>
    /// 中心からradiusMin〜radiusMaxの輪っか状の範囲内で、ランダムな点を1つ返す。
    /// </summary>
    private Vector2 RandomPointInRing(float radiusMin, float radiusMax)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(radiusMin, radiusMax);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
}