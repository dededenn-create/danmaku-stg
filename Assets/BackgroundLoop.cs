using UnityEngine;

public class BackgroundLoop : MonoBehaviour
{
    public float scrollSpeed = 2f;
    public float disappearY = -35f;  // このY座標に到達したらワープ
    public float returnY = -5f;      // 戻る先のY座標

    [Header("速度変化(中ボス撃破時などに使用)")]
    public float transitionDuration = 2f; // 何秒かけてなめらかに変化するか

    private float baseSpeed;
    private float targetMultiplier = 1f;
    private float currentMultiplier = 1f;

    void Start()
    {
        baseSpeed = scrollSpeed; // 開始時点のscrollSpeedを基準として保存
    }

    void Update()
    {
        // 現在の倍率を目標倍率へ、なめらかに近づける
        currentMultiplier = Mathf.MoveTowards(
            currentMultiplier,
            targetMultiplier,
            Time.deltaTime / transitionDuration
        );

        float actualSpeed = baseSpeed * currentMultiplier;
        transform.position += Vector3.down * actualSpeed * Time.deltaTime;

        if (transform.position.y < disappearY)
        {
            Vector3 pos = transform.position;
            pos.y = returnY;
            transform.position = pos;
        }
    }

    // 中ボス撃破時などに、外部からこれを呼び出す
    public void SetSpeedMultiplier(float multiplier)
    {
        targetMultiplier = multiplier;
    }
}