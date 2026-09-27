using UnityEngine;

/// <summary>
/// 弾の種類を指定する。Inspector上でBullet/EnemyBulletに設定する。
/// </summary>
public enum BulletOwner
{
    Player,
    Enemy
}

/// <summary>
/// Bullet.cs / EnemyBullet.cs にアタッチする当たり判定用コンポーネント。
/// 既存のBullet/EnemyBulletスクリプトとは別で追加すればOK
/// （移動ロジックはそのまま、当たり判定だけ担当）。
///
/// 前提:
/// - Player, Enemyのオブジェクトに Collider2D(isTrigger=true) と Health を付ける
/// - PlayerタグとEnemyタグを設定しておく
/// - 弾のColliderもisTrigger=trueにする
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BulletDamage : MonoBehaviour
{
    [SerializeField] private BulletOwner owner = BulletOwner.Enemy;
    [SerializeField] private int damage = 1;
    [SerializeField] private GameObject hitEffectPrefab;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        string targetTag = owner == BulletOwner.Enemy ? "Player" : "Enemy";

        if (!other.CompareTag(targetTag)) return;

        var health = other.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // 弾は当たったら消える（貫通させたい場合はここを条件分岐）
        Destroy(gameObject);
    }
}