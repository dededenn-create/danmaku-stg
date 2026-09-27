using UnityEngine;
public class EnemyZakoBloom : MonoBehaviour
{
    [Header("弾の設定")]
    public GameObject bulletPrefab;
    public float bulletSpeed;
    [Header("バーストの設定")]
    public int bulletCount;
    public float burstInterval;
    public float startDelay;
    [Header("回転の設定")]
    public float angleOffsetPerBurst;
    private float timer;
    private float currentAngleOffset = 0f;
    void Start()
    {
        timer = burstInterval - startDelay;
    }
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= burstInterval)
        {
            timer = 0f;
            FireBurst();
        }
    }
    void FireBurst()
    {
        float angleStep = 360f / bulletCount;
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = angleStep * i + currentAngleOffset;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                eb.SetVelocity(direction * bulletSpeed);
            }
        }
        currentAngleOffset += angleOffsetPerBurst;
    }
}