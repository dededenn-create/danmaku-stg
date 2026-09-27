using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// プレイヤーの残機を管理する。Health.cs(1発死判定)と組み合わせて使う。
///
/// 流れ:
/// 敵弾がPlayerに当たる → Health.TakeDamage(1) → HP0 → Health.OnDeath発火
/// → PlayerLivesがそれを拾って「残機を1減らす」→
///   残機が残っていれば一定時間後に復活(無敵付き)
///   残機が0ならゲームオーバー
///
/// 注意: 見た目(Renderer)の表示/非表示は SetActive ではなく Renderer.enabled で行う。
/// これは playerVisual に Player自身を指定した場合でも、
/// PlayerLives自身(コルーチン)が動き続けられるようにするため。
///
/// [DefaultExecutionOrder(-100)]:
/// Instance を他スクリプトのOnEnable/Startより確実に先に用意するため、
/// このスクリプトのAwake()が他より早く実行されるようにしている。
/// これが無いと、GameOverEventBridge側のOnEnable()がこちらのAwake()より
/// 先に走ってしまい、PlayerLives.Instanceがまだnullのままイベント購読に
/// 失敗するケースがある。
/// </summary>
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Health))]
public class PlayerLives : MonoBehaviour
{
    public static PlayerLives Instance;

    [Header("残機設定")]
    [SerializeField] private int startLives = 2;
    [SerializeField] private float respawnDelay = 1.0f;
    [SerializeField] private float invincibleDuration = 2.0f;
    [SerializeField] private Vector2 respawnPosition = new Vector2(0, -6);

    [Header("参照")]
    [SerializeField] private GameObject playerVisual;
    [SerializeField] private MonoBehaviour playerController;
    [SerializeField] private GameObject explosionPrefab;
    [Tooltip("既存の爆発に加えて出す、半透明の粒バースト(FakeExplosionBurstなど)。任意")]
    [SerializeField] private GameObject deathBurstPrefab;

    public int CurrentLives { get; private set; }
    public int DeathCount { get; private set; } = 0; // 死亡回数(クリアボーナス計算用)
    public bool IsInvincible { get; private set; }
    public bool IsAlive { get; private set; } = true;

    private Coroutine currentInvincibleCoroutine;

    public event Action<int> OnLivesChanged;
    public event Action OnGameOver;

    private Health health;
    private Collider2D hitCollider;
    private Renderer visualRenderer;

    private void Awake()
    {
        Instance = this;

        health = GetComponent<Health>();
        hitCollider = GetComponent<Collider2D>();
        CurrentLives = startLives;

        GameObject visualTarget = playerVisual != null ? playerVisual : gameObject;
        visualRenderer = visualTarget.GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        DeathCount++;
        CurrentLives--;
        OnLivesChanged?.Invoke(CurrentLives);
        IsAlive = false;

        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.OnPlayerHit();
        }

        Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // 既存の爆発に加えて、半透明の粒バーストも追加で出す
        if (deathBurstPrefab != null)
        {
            Instantiate(deathBurstPrefab, transform.position, Quaternion.identity);
        }

        // 死亡フラッシュと同時に、画面上の敵弾を全て消す
        EnemyBullet.ClearAllBullets();

        if (playerController is PlayerMove move)
        {
            move.HideHitboxImmediately();
        }

        if (visualRenderer != null) visualRenderer.enabled = false;
        if (playerController != null) playerController.enabled = false;
        if (hitCollider != null) hitCollider.enabled = false;

        if (CurrentLives >= 0)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            OnGameOver?.Invoke();
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        transform.position = respawnPosition;

        if (visualRenderer != null) visualRenderer.enabled = true;
        if (playerController != null) playerController.enabled = true;
        IsAlive = true;

        health.ResetHP();

        currentInvincibleCoroutine = StartCoroutine(InvincibleRoutine());
    }

    private IEnumerator InvincibleRoutine()
    {
        IsInvincible = true;
        if (hitCollider != null) hitCollider.enabled = false;

        float timer = 0f;
        while (timer < invincibleDuration)
        {
            if (visualRenderer != null) visualRenderer.enabled = !visualRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }
        if (visualRenderer != null) visualRenderer.enabled = true;

        if (hitCollider != null) hitCollider.enabled = true;
        IsInvincible = false;
        currentInvincibleCoroutine = null;
    }

    public void ActivateBombInvincible(float duration)
    {
        if (currentInvincibleCoroutine != null)
        {
            StopCoroutine(currentInvincibleCoroutine);
        }
        if (visualRenderer != null) visualRenderer.enabled = true;
        currentInvincibleCoroutine = StartCoroutine(BombInvincibleRoutine(duration));
    }

    private IEnumerator BombInvincibleRoutine(float duration)
    {
        IsInvincible = true;
        if (hitCollider != null) hitCollider.enabled = false;

        float timer = 0f;
        while (timer < duration)
        {
            if (visualRenderer != null) visualRenderer.enabled = !visualRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }
        if (visualRenderer != null) visualRenderer.enabled = true;

        if (hitCollider != null) hitCollider.enabled = true;
        IsInvincible = false;
        currentInvincibleCoroutine = null;
    }
}