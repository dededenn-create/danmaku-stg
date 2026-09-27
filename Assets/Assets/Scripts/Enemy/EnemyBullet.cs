using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private Vector2 velocity;
    private bool isBeingSucked = false;
    private Transform player;
    public float suckSpeed = 15f;
    public float reachDistance = 0.1f; // このくらい近づいたら「到達」とみなす

    public static List<EnemyBullet> activeBullets = new List<EnemyBullet>();

    [Header("発射音(任意)")]
    [Tooltip("ONにすると、この弾が生成された時に発射音を鳴らす")]
    public bool playSpawnSound = false;
    [Tooltip("鳴らす音源。基本的には全弾幕で同じクリップを使い回し、Pitchだけ弾の種類ごとに変える運用を想定")]
    public AudioClip spawnSound;
    [Tooltip("この弾の再生ピッチ。種類ごとに変えることで、同じ音源でも違う音に聞こえさせられる")]
    public float pitch = 1f;
    [Tooltip("発射音の音量")]
    [Range(0f, 1f)]
    public float spawnSoundVolume = 1f;

    // 全弾幕・全弾種で共有するクールダウン。どれだけ多くの弾が同時に生成されても、
    // 実際に音が鳴るのはこの間隔に1回だけになる(音の洪水を防ぐ)。
    private static float lastSoundPlayTime = -999f;
    public static float soundCooldown = 0.08f;

    void OnEnable()
    {
        activeBullets.Add(this);

        if (playSpawnSound && spawnSound != null)
        {
            TryPlaySpawnSound();
        }
    }

    void OnDisable()
    {
        activeBullets.Remove(this);
    }

    public void SetVelocity(Vector2 vel)
    {
        velocity = vel;
    }

    void Update()
    {
        if (isBeingSucked)
        {
            if (player == null)
            {
                Destroy(gameObject); // プレイヤーが取れなければ念のため消す
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, player.position, suckSpeed * Time.deltaTime);

            // プレイヤーに到達したら消える
            if (Vector3.Distance(transform.position, player.position) < reachDistance)
            {
                Destroy(gameObject);
            }
            return;
        }

        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    public void StartSuckToPlayer(Transform playerTransform)
    {
        isBeingSucked = true;
        player = playerTransform;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Destroy(gameObject, 0.3f) は削除、到達判定で消えるように変更
    }

    /// <summary>
    /// 画面上に存在する全ての敵弾を即座に消す。
    /// プレイヤーがミスした瞬間のフラッシュ演出と合わせて呼ぶ想定
    /// (例: プレイヤー側の被弾処理から EnemyBullet.ClearAllBullets(); を呼ぶ)。
    /// </summary>
    public static void ClearAllBullets()
    {
        // ToArray()でコピーしてから回す(Destroy中にactiveBulletsの中身が変化するため)
        foreach (EnemyBullet bullet in activeBullets.ToArray())
        {
            if (bullet != null)
            {
                Destroy(bullet.gameObject);
            }
        }
    }

    /// <summary>
    /// 発射音を鳴らす。全弾共通のクールダウンを経過していなければ何もしない。
    /// 一時的なAudioSourceを生成して鳴らすので、シーンに専用のAudioSourceを常設する必要がない。
    /// </summary>
    private void TryPlaySpawnSound()
    {
        if (Time.time - lastSoundPlayTime < soundCooldown) return;
        lastSoundPlayTime = Time.time;

        GameObject temp = new GameObject("BulletSpawnSE");
        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = spawnSound;
        src.pitch = pitch;
        src.volume = spawnSoundVolume;
        src.spatialBlend = 0f; // 2Dサウンド(位置による音量変化なし)
        src.Play();

        float safePitch = Mathf.Max(0.01f, Mathf.Abs(pitch));
        Destroy(temp, spawnSound.length / safePitch);
    }
}