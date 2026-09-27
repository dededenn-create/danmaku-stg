using UnityEngine;

/// <summary>
/// 爆発演出の「粒」1つ分。当たり判定は持たない(Collider不要、見た目だけ)。
/// 生成された方向へ直進しながら、だんだん透明になって消える。
/// 飛んでいく方向に合わせて、スプライト自体も回転する(十字架などの向きがある絵に対応)。
/// </summary>
public class ExplosionParticle : MonoBehaviour
{
    [Tooltip("素材の元の向きを補正するための角度(度)。例えば絵が「上向き」で描かれているなら90を入れる")]
    public float rotationOffsetDegrees = 0f;

    private Vector2 velocity;
    private float lifetime;
    private float age = 0f;
    private SpriteRenderer spriteRenderer;
    private Color baseColor;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 生成直後にFakeExplosionBurst側から呼ぶ。
    /// </summary>
    public void Init(Vector2 vel, float life, float startAlpha)
    {
        velocity = vel;
        lifetime = life;
        age = 0f;

        // 飛んでいく方向を向くように回転させる
        float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffsetDegrees);

        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
            baseColor.a = startAlpha;
            spriteRenderer.color = baseColor;
        }
    }

    void Update()
    {
        age += Time.deltaTime;

        transform.position += (Vector3)(velocity * Time.deltaTime);

        if (spriteRenderer != null)
        {
            float t = Mathf.Clamp01(age / lifetime);
            Color c = baseColor;
            c.a = Mathf.Lerp(baseColor.a, 0f, t); // だんだん透明になる
            spriteRenderer.color = c;
        }

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}