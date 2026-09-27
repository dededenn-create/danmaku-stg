using System.Collections;
using UnityEngine;

/// <summary>
/// 爆発演出。当たり判定のない半透明の粒を、くっそ速く何波か連続でばらまく。
/// 
/// 使い方:
/// 1. 粒用のプレハブを作る(空のGameObject + SpriteRenderer + ExplosionParticle.cs、Colliderは付けない)
/// 2. このスクリプト(FakeExplosionBurst.cs)を別の空のGameObjectにアタッチしてプレハブ化
/// 3. Particle Prefab に①で作ったプレハブを設定
/// 4. Enemy.csやBossControllerのexplosionBigPrefabに、③のプレハブを設定
/// 
/// 生成された瞬間から自動的に発動し、全部のばらまきが終わったら自動でDestroyされる。
/// </summary>
public class FakeExplosionBurst : MonoBehaviour
{
    [Header("粒")]
    [Tooltip("ExplosionParticleが付いた粒のプレハブ(Colliderは不要)")]
    public GameObject particlePrefab;
    [Tooltip("粒の速度の最小値")]
    public float minSpeed = 10f;
    [Tooltip("粒の速度の最大値")]
    public float maxSpeed = 18f;
    [Tooltip("粒が消えるまでの寿命(秒)")]
    public float particleLifetime = 0.25f;
    [Tooltip("粒の透明度の初期値(0〜1、半透明にしたいなら1未満)")]
    public float particleStartAlpha = 0.7f;

    [Header("波")]
    [Tooltip("何波ばらまくか")]
    public int waveCount = 3;
    [Tooltip("1波あたり何粒出すか")]
    public int particlesPerWave = 20;
    [Tooltip("波と波の間隔(秒)。短いほど連続してバババッと出る")]
    public float waveInterval = 0.04f;

    void Start()
    {
        StartCoroutine(BurstRoutine());
    }

    private IEnumerator BurstRoutine()
    {
        for (int wave = 0; wave < waveCount; wave++)
        {
            SpawnWave();
            yield return new WaitForSeconds(waveInterval);
        }

        // 全ての波を出し終えたら、この管理役自体は不要なので消える
        // (粒はそれぞれExplosionParticle側の寿命で自動的に消える)
        Destroy(gameObject);
    }

    private void SpawnWave()
    {
        if (particlePrefab == null) return;

        for (int i = 0; i < particlesPerWave; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float speed = Random.Range(minSpeed, maxSpeed);

            GameObject obj = Instantiate(particlePrefab, transform.position, Quaternion.identity);
            ExplosionParticle particle = obj.GetComponent<ExplosionParticle>();
            if (particle != null)
            {
                particle.Init(direction * speed, particleLifetime, particleStartAlpha);
            }
        }
    }
}