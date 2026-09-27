using UnityEngine;

public class BossPhase2Bullet : MonoBehaviour
{
    [Header("白線弾の設定(自機狙い基準・放射状)")]
    public float lineInterval = 1f;
    public int lineBulletCount = 5;
    public float lineSpacing = 0.3f;
    public int lineSymmetryCount = 20;

    [Header("3種類の弾(色違いPrefab)と速度")]
    public GameObject bulletPrefabSlow;   // 例: 紫
    public float speedSlow = 3.0f;

    public GameObject bulletPrefabMid;    // 例: 赤
    public float speedMid = 4.5f;

    public GameObject bulletPrefabFast;   // 例: シアン
    public float speedFast = 6.5f;

    private float lineTimer;
    private Transform player;

    void OnEnable()
    {
        lineTimer = 0f;
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        lineTimer += Time.deltaTime;
        if (lineTimer >= lineInterval)
        {
            lineTimer = 0f;
            FireLineShape(bulletPrefabSlow, speedSlow);
            FireLineShape(bulletPrefabMid, speedMid);
            FireLineShape(bulletPrefabFast, speedFast);
        }
    }

    void FireLineShape(GameObject bulletPrefab, float speed)
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
                    eb.SetVelocity(direction * speed);
                }
            }
        }
    }
}