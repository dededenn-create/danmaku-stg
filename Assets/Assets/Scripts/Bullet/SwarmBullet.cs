using UnityEngine;

/// <summary>
/// スペルカード用「ばらまき玉」
/// 
/// 流れ:
/// 1. 生成時に決められた目的地(targetPos)へ向かって移動しながら減速する
/// 2. 目的地にピッタリ到着して停止(Idle)
/// 3. ボス側からLaunch()を呼ばれると、その瞬間のプレイヤー座標へ加速しながら直進する
/// </summary>
public class SwarmBullet : MonoBehaviour
{
    private enum State
    {
        MovingToTarget, // 目的地へ向かって減速移動中
        Idle,           // 目的地で静止、ロック待ち
        Launched        // 発射済み、加速直進中
    }

    [Header("移動フェーズ(出現→目的地で停止)")]
    [Tooltip("出現してから目的地に到着するまでの時間(秒)")]
    public float moveDuration = 0.8f;

    [Header("発射フェーズ(ロック→加速直進)")]
    [Tooltip("発射直後の初速")]
    public float initialLaunchSpeed = 1f;
    [Tooltip("発射後、1秒あたりどれだけ速度が上がるか")]
    public float acceleration = 6f;
    [Tooltip("発射後の最大速度")]
    public float maxSpeed = 8f;

    private State state = State.MovingToTarget;

    private Vector2 startPos;
    private Vector2 targetPos;
    private float moveTimer = 0f;

    private Vector2 launchDir;
    private float launchSpeed;

    /// <summary>
    /// ボス側が生成直後に必ず呼ぶ初期化処理。
    /// 開始位置(敵の位置)と、ばらまき先の目的地を渡す。
    /// </summary>
    public void Init(Vector2 start, Vector2 target, float duration)
    {
        startPos = start;
        targetPos = target;
        moveDuration = duration;

        transform.position = start;
        moveTimer = 0f;
        state = State.MovingToTarget;
    }

    /// <summary>
    /// ボス側から呼ぶ。呼ばれた瞬間のtargetLockPos(プレイヤー座標)へ向けて発射する。
    /// MovingToTarget中でもIdle中でも呼び出し可能。
    /// </summary>
    public void Launch(Vector2 targetLockPos)
    {
        launchDir = (targetLockPos - (Vector2)transform.position).normalized;
        launchSpeed = initialLaunchSpeed;
        state = State.Launched;
    }

    /// <summary>
    /// 狙い撃ちではなく、決まった方向へ発射したい場合に使う。
    /// 例: Launch(Vector2.down) で全弾真下に落とす。
    /// </summary>
    public void LaunchInDirection(Vector2 direction)
    {
        launchDir = direction.normalized;
        launchSpeed = initialLaunchSpeed;
        state = State.Launched;
    }

    public bool IsLaunched => state == State.Launched;

    void Update()
    {
        switch (state)
        {
            case State.MovingToTarget:
                UpdateMovingToTarget();
                break;

            case State.Idle:
                // 何もしない。ボスからのLaunch()呼び出しを待つだけ。
                break;

            case State.Launched:
                UpdateLaunched();
                break;
        }
    }

    private void UpdateMovingToTarget()
    {
        moveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(moveTimer / moveDuration);

        // イーズアウト: 最初は速く、目的地に近づくほどゆっくりになる
        float easedT = 1f - Mathf.Pow(1f - t, 3f);

        transform.position = Vector2.Lerp(startPos, targetPos, easedT);

        if (t >= 1f)
        {
            transform.position = targetPos; // 誤差なくピッタリ停止
            state = State.Idle;
        }
    }

    private void UpdateLaunched()
    {
        launchSpeed = Mathf.Min(launchSpeed + acceleration * Time.deltaTime, maxSpeed);
        transform.position += (Vector3)(launchDir * launchSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 画面外に出たら消えるための簡易処理。
    /// 実際のプロジェクトの画面境界に合わせて数値は調整してください。
    /// </summary>
    void LateUpdate()
    {
        const float margin = 1.5f;
        Vector2 pos = transform.position;
        if (pos.x < -margin - 8f || pos.x > margin + 8f ||
            pos.y < -margin - 5f || pos.y > margin + 5f)
        {
            Destroy(gameObject);
        }
    }
}