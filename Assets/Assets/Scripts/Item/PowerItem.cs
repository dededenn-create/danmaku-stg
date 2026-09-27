using System.Collections.Generic;
using UnityEngine;

public class PowerItem : MonoBehaviour
{
    public int powerValue = 1;
    public float popSpeed = 3f;
    public float fallSpeed = 1.5f;
    public float decelTime = 0.3f;
    public float attractRange = 1.5f;
    public float attractSpeed = 8f;
    public float collectDistance = 0.2f;

    [Header("スコア")]
    public int baseScore = 200; // 小アイテム200 / 大アイテム1000
    public int itemWeight = 1;  // 小アイテム=1 / 大アイテム=5(全回収ライン倍率の重み)

    [Header("取得音")]
    [Tooltip("取得時に鳴らす音。AudioSourceは不要(一時的なAudioSourceを自動生成して鳴らす)")]
    public AudioClip collectSound;
    [Range(0f, 1f)]
    public float collectSoundVolume = 1f;
    [Tooltip("再生ピッチ。1が通常、下げると低く、上げると高くなる")]
    public float collectSoundPitch = 1f;
    [Tooltip("毎回このピッチ値をランダムでブレさせる幅。連続取得時に音が団子状に混ざって聞こえにくくなるのを防ぐ")]
    public float collectSoundPitchVariation = 0.3f;

    public static List<PowerItem> activeItems = new List<PowerItem>();

    private float velocityY;
    private float timer = 0f;
    private Transform player;
    private bool isFullyCollecting = false;
    private bool isCollected = false;
    private int fullCollectTotalWeight = 0;

    void OnEnable()
    {
        activeItems.Add(this);
    }

    void OnDisable()
    {
        activeItems.Remove(this);
    }

    void Start()
    {
        velocityY = popSpeed;
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (isCollected) return; 

        float distance = Vector3.Distance(transform.position, player.position);

        if (isFullyCollecting || distance < attractRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position, attractSpeed * Time.deltaTime);
        }
        else
        {
            timer += Time.deltaTime;
            if (timer < decelTime)
            {
                velocityY = Mathf.Lerp(popSpeed, -fallSpeed, timer / decelTime);
            }
            else
            {
                velocityY = -fallSpeed;
            }
            transform.position += new Vector3(0, velocityY, 0) * Time.deltaTime;
        }

        distance = Vector3.Distance(transform.position, player.position);
        if (distance < collectDistance)
        {
            Collect();
        }
    }

    void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        bool isMaxed = PlayerStatus.Instance.IsPowerMaxed();

        // スコア加算(通常時/上限後、全回収ラインなら発動時点で確定した重み合計で倍率適用)
        ScoreManager.Instance.AddItemScore(baseScore, isMaxed, isFullyCollecting, fullCollectTotalWeight);

        // Power上限に達していなければPowerを加算
        if (!isMaxed)
        {
            PlayerStatus.Instance.AddPower(powerValue);
        }

        PlayCollectSound();

        Destroy(gameObject);
    }

    /// <summary>
    /// 取得音を鳴らす。一時的なAudioSourceを生成して鳴らすので、
    /// このオブジェクトがDestroyされても音は最後まで再生される。
    /// 毎回ピッチをわずかにランダムでブレさせることで、連続取得時に音が団子状に混ざるのを防ぐ。
    /// </summary>
    private void PlayCollectSound()
    {
        if (collectSound == null) return;

        float randomizedPitch = collectSoundPitch + Random.Range(-collectSoundPitchVariation, collectSoundPitchVariation);

        GameObject temp = new GameObject("ItemCollectSE");
        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = collectSound;
        src.priority = 0; // 最優先(0)にして、他の音に埋もれて間引かれるのを防ぐ
        src.volume = collectSoundVolume;
        src.pitch = randomizedPitch;
        src.spatialBlend = 0f; // 2Dサウンド
        src.Play();

        float safePitch = Mathf.Max(0.01f, Mathf.Abs(randomizedPitch));
        Destroy(temp, collectSound.length / safePitch);
    }

    public void TriggerFullCollect(int totalWeight)
    {
        isFullyCollecting = true;
        fullCollectTotalWeight = totalWeight;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Collect(); 
        }
    }
}