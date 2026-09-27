using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class BombController : MonoBehaviour
{
    [Header("References")]
    public GameObject cutInPanel;
    public CanvasGroup whiteFlashGroup;

    [Header("Timing")]
    public float invincibleDuration = 4.0f;
    public float cutInDuration = 1.0f;
    public int flickerCount = 3;
    public float flickerInterval = 0.08f;
    public float whiteHoldDuration = 0.5f;
    public float whiteFadeOutDuration = 0.3f;
    public float flashAlpha = 0.7f;

    [Header("ダメージ")]
    [Tooltip("フラッシュが来た瞬間、画面上の敵(雑魚敵・ボス)に与えるダメージ量")]
    public float bombDamage = 50f;
    [Tooltip("bombDamageを一発で入れず、この秒数かけて高速で細かく撃ち込む")]
    public float bombDamageDuration = 0.4f;

    [Header("発動音")]
    [Tooltip("ボムを発動した瞬間に鳴らす音。AudioSourceは不要(一時的なAudioSourceを自動生成して鳴らす)")]
    public AudioClip bombSound;
    [Range(0f, 1f)]
    public float bombSoundVolume = 1f;
    public float bombSoundPitch = 1f;

    [Header("フラッシュ音")]
    [Tooltip("フラッシュが来た瞬間に鳴らす音")]
    public AudioClip flashSound;
    [Range(0f, 1f)]
    public float flashSoundVolume = 1f;
    public float flashSoundPitch = 1f;

    private bool isBombing = false;

    void Update()
    {
        if (PlayerLives.Instance != null && !PlayerLives.Instance.IsAlive) return;
        
        if (Keyboard.current.xKey.wasPressedThisFrame && !isBombing)
        {
            TryUseBomb();
        }
    }

    void TryUseBomb()
    {
        var status = PlayerStatus.Instance;
        if (status == null) return;

        if (status.UseBomb())
        {
            PlayBombSound();
            StartCoroutine(BombSequence());
        }
    }

    /// <summary>
    /// ボム発動音を鳴らす。一時的なAudioSourceを生成して鳴らすので、
    /// あらかじめシーンやプレハブにAudioSourceを用意しておく必要がない。
    /// </summary>
    private void PlayBombSound()
    {
        PlayOneShotSound(bombSound, bombSoundVolume, bombSoundPitch, "BombSE");
    }

    /// <summary>
    /// フラッシュ音を鳴らす。
    /// </summary>
    private void PlayFlashSound()
    {
        PlayOneShotSound(flashSound, flashSoundVolume, flashSoundPitch, "BombFlashSE");
    }

    /// <summary>
    /// SEを再生する共通処理。一時的なAudioSourceを生成して鳴らすので、
    /// あらかじめシーンやプレハブにAudioSourceを用意しておく必要がない。
    /// </summary>
    private void PlayOneShotSound(AudioClip clip, float volume, float pitch, string tempObjectName)
    {
        if (clip == null) return;

        GameObject temp = new GameObject(tempObjectName);
        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.spatialBlend = 0f; // 2Dサウンド
        src.Play();

        float safePitch = Mathf.Max(0.01f, Mathf.Abs(pitch));
        Destroy(temp, clip.length / safePitch);
    }

    IEnumerator BombSequence()
    {
        isBombing = true;

        PlayerLives.Instance.ActivateBombInvincible(invincibleDuration);

        if (cutInPanel != null)
        {
            cutInPanel.SetActive(true);
            yield return new WaitForSeconds(cutInDuration);
            cutInPanel.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(cutInDuration);
        }

        if (whiteFlashGroup != null)
        {
            for (int i = 0; i < flickerCount; i++)
            {
                whiteFlashGroup.alpha = flashAlpha;
                yield return new WaitForSeconds(flickerInterval);
                whiteFlashGroup.alpha = 0f;
                yield return new WaitForSeconds(flickerInterval);
            }

            whiteFlashGroup.alpha = flashAlpha;

            // フラッシュが本格的に来たこのタイミングで、音とダメージを1回だけ発動
            PlayFlashSound();
            DealBombDamage();

            float holdTimer = 0f;
            while (holdTimer < whiteHoldDuration)
            {
                ClearAllBullets();
                holdTimer += Time.deltaTime;
                yield return null;
            }

            float elapsed = 0f;
            while (elapsed < whiteFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                whiteFlashGroup.alpha = Mathf.Lerp(flashAlpha, 0f, elapsed / whiteFadeOutDuration);
                yield return null;
            }
            whiteFlashGroup.alpha = 0f;
        }
        else
        {
            // whiteFlashGroupが無い場合も、このタイミングで音とダメージだけは発動する
            PlayFlashSound();
            DealBombDamage();
            ClearAllBullets();
        }

        float elapsedSoFar = cutInDuration + (flickerCount * flickerInterval * 2) + whiteHoldDuration + whiteFadeOutDuration;
        float remaining = invincibleDuration - elapsedSoFar;
        if (remaining > 0)
        {
            yield return new WaitForSeconds(remaining);
        }

        isBombing = false;
    }

    /// <summary>
    /// 画面上の全ての雑魚敵・ボスに、bombDamage分のダメージを与える。
    /// 一発で入れるのではなく、bombDamageDuration秒かけて高速で細かく撃ち込む。
    /// </summary>
    void DealBombDamage()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
        {
            enemy.TakeDamageOverTime(Mathf.RoundToInt(bombDamage), bombDamageDuration);
        }

        BossController[] bosses = FindObjectsByType<BossController>(FindObjectsSortMode.None);
        foreach (BossController boss in bosses)
        {
            boss.TakeDamageOverTime(bombDamage, bombDamageDuration);
        }
    }

    void ClearAllBullets()
    {
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
        foreach (GameObject bullet in bullets)
        {
            Destroy(bullet);
        }
    }
}