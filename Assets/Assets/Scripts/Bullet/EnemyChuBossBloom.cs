using UnityEngine;

public class EnemyChuBossBloom : MonoBehaviour
{
    [Header("大玉の設定(対称配置・回転)")]
    public GameObject bigBulletPrefab;
    public float bigBulletSpeed = 1.5f;
    public int symmetryCount = 12;
    public float burstInterval = 0.7f;
    public float angleOffsetPerBurst = 18f;

    [Header("白線弾の設定(自機狙い基準・放射状に連動回転)")]
    public GameObject bulletPrefab;
    public float lineBulletSpeed = 3.5f;
    public float lineInterval = 1f;
    public int lineBulletCount = 5;
    public float lineSpacing = 0.3f;
    public int lineSymmetryCount = 20;

    [Header("共通の初回発射遅延")]
    public float startDelay = 1f;

    private float timer;
    private float lineTimer;
    private float currentAngleOffset = 0f;
    private Transform player;

    void Start()
    {
        timer = burstInterval - startDelay;
        lineTimer = lineInterval - startDelay;
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= burstInterval)
        {
            timer = 0f;
            FireBigBulletSymmetry();
        }

        lineTimer += Time.deltaTime;
        if (lineTimer >= lineInterval)
        {
            lineTimer = 0f;
            FireLineShape();
        }
    }

    // 大玉を対称配置で発射(周囲に展開・回転)
    void FireBigBulletSymmetry()
    {
        float angleStep = 360f / symmetryCount;
        for (int i = 0; i < symmetryCount; i++)
        {
            float angle = angleStep * i + currentAngleOffset;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            GameObject bullet = Instantiate(bigBulletPrefab, transform.position, Quaternion.identity);
            EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                eb.SetVelocity(direction * bigBulletSpeed);
            }
        }
        currentAngleOffset += angleOffsetPerBurst;
    }

    // 白線弾を放射状に配置(1本は必ず自機方向、残りは連動して同じ角度分離れて回転)
    void FireLineShape()
    {
        if (player == null || bulletPrefab == null) return;

        Vector2 toPlayer = (player.position - transform.position).normalized;
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        float angleStep = 360f / lineSymmetryCount;

        for (int lineIndex = 0; lineIndex < lineSymmetryCount; lineIndex++)
        {
            float angle = baseAngle + angleStep * lineIndex;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            for (int i = 0; i < lineBulletCount; i++)
            {
                Vector3 spawnPos = transform.position + (Vector3)(direction * lineSpacing * i);
                GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
                EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
                if (eb != null)
                {
                    eb.SetVelocity(direction * lineBulletSpeed);
                }
            }
        }
    }
}