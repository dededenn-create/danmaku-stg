using System.Collections;
using UnityEngine;

/// <summary>
/// ボス登場演出。FakeExplosionBurst(撃破演出)の逆で、
/// 周囲のランダムな位置に生まれた粒が、このオブジェクトの位置(=ボスの登場位置)へ向かって集まってくる。
/// 演出が始まった瞬間に、任意でSEも鳴らせる。
/// 
/// 使い方:
/// 1. 粒用のプレハブを作る(空のGameObject + SpriteRenderer + ConvergeParticle.cs、Colliderは付けない)
/// 2. このスクリプト(BossConvergeIntro.cs)を別の空のGameObjectにアタッチしてプレハブ化
/// 3. Particle Prefab に①で作ったプレハブを設定
/// 4. ボスが登場する位置(bossSpawnPointなど)にこのプレハブをInstantiateし、
///    直後にBegin(duration)を呼ぶ(durationはボスが実際に出現するまでの残り秒数)。
///    こうすることで、ボスが出現する瞬間まで、waveIntervalおきに途切れず粒が出続ける。
/// 
/// Begin()を呼ばずに放置した場合は、フォールバックとしてfallbackDurationを使って自動的に開始する。
/// </summary>
public class BossConvergeIntro : MonoBehaviour
{
    [Header("粒")]
    [Tooltip("ConvergeParticleが付いた粒のプレハブ(Colliderは不要)")]
    public GameObject particlePrefab;
    [Tooltip("粒が生まれる範囲の半径(この中心=このオブジェクトの位置からランダムに配置される)")]
    public float spawnRadius = 6f;
    [Tooltip("粒の初速")]
    public float initialSpeed = 2f;
    [Tooltip("粒が中心に近づくにつれてどれだけ加速するか")]
    public float acceleration = 15f;
    [Tooltip("中心からこの距離未満になったら「到達」とみなして消える")]
    public float arriveThreshold = 0.1f;

    [Header("波")]
    [Tooltip("波と波の間隔(秒)。小さいほど途切れず連続して出続ける")]
    public float waveInterval = 0.12f;
    [Tooltip("1波あたり何粒出すか")]
    public int particlesPerWave = 8;

    [Tooltip("Begin()が呼ばれずに放置された場合に使う、演出時間のフォールバック値(秒)")]
    public float fallbackDuration = 1.8f;

    [Header("開始音")]
    [Tooltip("演出が続いている間ずっとループで鳴らす音。AudioSourceは不要(一時的なAudioSourceを自動生成して鳴らす)")]
    public AudioClip startSound;
    [Range(0f, 1f)]
    public float startSoundVolume = 1f;
    [Tooltip("再生ピッチ。1が通常")]
    public float startSoundPitch = 1f;
    [Tooltip("演出(粒が出る時間)が終わった後、さらに何秒この音を延長して鳴らすか")]
    public float startSoundExtraDuration = 0f;

    private bool started = false;

    void Start()
    {
        // 外部からBegin()が呼ばれていなければ、フォールバックの時間で自動開始する
        if (!started)
        {
            Begin(fallbackDuration);
        }
    }

    /// <summary>
    /// 演出を開始する。durationにボスが実際に出現するまでの秒数を渡すと、
    /// waveIntervalの間隔で、その時間いっぱいまでずっと途切れず波が出続ける。
    /// </summary>
    public void Begin(float duration)
    {
        if (started) return;
        started = true;

        float safeDuration = Mathf.Max(0.1f, duration);

        PlayStartSound(safeDuration);

        StartCoroutine(ConvergeRoutine(safeDuration));
    }

    /// <summary>
    /// 開始音を鳴らす。一時的なAudioSourceを生成して鳴らすので、
    /// あらかじめシーンやプレハブにAudioSourceを用意しておく必要がない。
    /// 粒が出続けてる間ずっとループ再生し、演出終了(duration経過)後もstartSoundExtraDuration秒だけ延長して鳴らす。
    /// </summary>
    private void PlayStartSound(float duration)
    {
        if (startSound == null) return;

        GameObject temp = new GameObject("ConvergeStartSE");
        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = startSound;
        src.volume = startSoundVolume;
        src.pitch = startSoundPitch;
        src.spatialBlend = 0f; // 2Dサウンド
        src.loop = true; // 演出が続いてる間、途切れず鳴り続ける
        src.Play();

        Destroy(temp, duration + startSoundExtraDuration);
    }

    private IEnumerator ConvergeRoutine(float totalDuration)
    {
        float elapsed = 0f;

        // durationいっぱいまで、waveIntervalおきにひたすら波を出し続ける
        while (elapsed < totalDuration)
        {
            SpawnWave();
            yield return new WaitForSeconds(waveInterval);
            elapsed += waveInterval;
        }

        // 粒はそれぞれConvergeParticle側で中心到達時に自動的に消えるので、
        // ここではこの管理役のオブジェクトだけ消す
        Destroy(gameObject);
    }

    private void SpawnWave()
    {
        if (particlePrefab == null) return;

        Vector2 center = transform.position;

        for (int i = 0; i < particlesPerWave; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * spawnRadius;
            offset *= Random.Range(0.7f, 1f);

            Vector2 spawnPos = center + offset;

            GameObject obj = Instantiate(particlePrefab, spawnPos, Quaternion.identity);
            ConvergeParticle particle = obj.GetComponent<ConvergeParticle>();
            if (particle != null)
            {
                particle.Init(center, initialSpeed, acceleration, arriveThreshold);
            }
        }
    }
}