using UnityEngine;

public class PillarLoopManager : MonoBehaviour
{
    public Transform pillar1;
    public Transform pillar2;
    public float scrollSpeed = 2f;
    public float pillarHeight = 10f; // 柱1枚の高さ
    public float disappearY = -10f;  // 画面から消えるY座標

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
        currentMultiplier = Mathf.MoveTowards(
            currentMultiplier,
            targetMultiplier,
            Time.deltaTime / transitionDuration
        );

        float actualSpeed = baseSpeed * currentMultiplier;

        MoveAndLoop(pillar1, actualSpeed);
        MoveAndLoop(pillar2, actualSpeed);
    }

    void MoveAndLoop(Transform pillar, float speed)
    {
        pillar.position += Vector3.down * speed * Time.deltaTime;

        if (pillar.position.y < disappearY)
        {
            Vector3 pos = pillar.position;
            pos.y += pillarHeight * 2; // 2枚分上に戻す
            pillar.position = pos;
        }
    }

    // 中ボス撃破時などに、外部からこれを呼び出す
    public void SetSpeedMultiplier(float multiplier)
    {
        targetMultiplier = multiplier;
    }
}