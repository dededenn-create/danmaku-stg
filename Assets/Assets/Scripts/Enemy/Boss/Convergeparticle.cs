using UnityEngine;

/// <summary>
/// ボス登場演出の「粒」1つ分。当たり判定は持たない(Collider不要、見た目だけ)。
/// ExplosionParticleの逆で、指定した中心座標へ向かって飛んでいき、
/// 近づくにつれて加速する。中心に到達したら消える。
/// </summary>
public class ConvergeParticle : MonoBehaviour
{
    private Vector2 target;
    private float speed;
    private float acceleration;
    private float arriveThreshold;
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// 生成直後にBossConvergeIntro側から呼ぶ。
    /// </summary>
    public void Init(Vector2 targetPos, float initialSpeed, float accel, float threshold)
    {
        target = targetPos;
        speed = initialSpeed;
        acceleration = accel;
        arriveThreshold = threshold;

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        speed += acceleration * Time.deltaTime;

        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) < arriveThreshold)
        {
            Destroy(gameObject);
        }
    }
}