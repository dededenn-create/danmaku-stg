using UnityEngine;

// Enemy_Bossにアタッチする。雑魚敵用のEnemy.csとは別系統で、
// 弾が当たったらBossController.TakeDamage()を呼ぶだけのシンプルな役割。
[RequireComponent(typeof(BossController))]
public class BossDamageReceiver : MonoBehaviour
{
    [Tooltip("弾1発あたりのダメージ量")]
    public float damagePerHit = 1f;

    private BossController bossController;

    void Awake()
    {
        bossController = GetComponent<BossController>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Bullet")) return;

        Destroy(other.gameObject);
        bossController.TakeDamage(damagePerHit);
    }
}