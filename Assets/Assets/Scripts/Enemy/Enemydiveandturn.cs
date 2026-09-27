using System.Collections;
using UnityEngine;

public class EnemyDiveAndTurn : MonoBehaviour
{
    [Header("下降設定")]
    public float downSpeed = 2f;      // 下降速度
    public float downDistance = 3f;   // どれだけ下がったら旋回開始するか

    [Header("旋回設定")]
    public float turnRadius = 1.5f;   // 旋回半径（カーブの大きさ）
    public float turnAngleSpeed = 180f; // 旋回の速さ（deg/sec）大きいほど急旋回
    public bool turnRight = true;     // true:→へ旋回、false:←へ旋回

    [Header("横移動設定")]
    public float sideSpeed = 3f;      // 旋回後の横移動速度
    public float destroyAfterSideTime = 4f; // 横移動を続ける時間(画面外で消す用)

    void Start()
    {
        StartCoroutine(MoveRoutine());
    }

    IEnumerator MoveRoutine()
    {
        // ① まっすぐ下降
        float startY = transform.position.y;
        while (startY - transform.position.y < downDistance)
        {
            transform.position += Vector3.down * downSpeed * Time.deltaTime;
            yield return null;
        }

        // ② 四分円(90度)を描いて旋回
        float dir = turnRight ? 1f : -1f;
        // 旋回の中心は現在位置から進行方向(右 or 左)に半径分ずらした点
        Vector3 center = transform.position + Vector3.right * dir * turnRadius;

        // 開始角度180度(現在位置)→終了角度90度(下向き成分がゼロになる位置)
        float angle = 180f;
        float endAngle = 270f;

        while (Mathf.Abs(angle - endAngle) > 0.5f)
        {
            angle = Mathf.MoveTowards(angle, endAngle, turnAngleSpeed * Time.deltaTime);
            float rad = angle * Mathf.Deg2Rad;
            transform.position = center + new Vector3(Mathf.Cos(rad) * dir, Mathf.Sin(rad), 0) * turnRadius;
            yield return null;
        }

        // ③ 横方向へ直進
        float t = 0f;
        while (t < destroyAfterSideTime)
        {
            transform.position += Vector3.right * dir * sideSpeed * Time.deltaTime;
            t += Time.deltaTime;
            yield return null;
        }

        // 画面外に出たら消す(必要に応じて)
        Destroy(gameObject);
    }
}