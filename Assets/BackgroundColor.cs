using UnityEngine;

public class CameraBackgroundShift : MonoBehaviour
{
    public Camera targetCamera;
    public Color[] colors; // 変化させたい色のリスト(穏やか→禍々しい、の順で並べる)
    public float transitionDuration = 5f; // 1色から次の色までの時間(秒)。短くしたければここを調整

    private int currentIndex = 0;
    private float timer = 0f;
    private bool reachedFinalColor = false; // 最後の色に到達したかどうか

    void Start()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
    }

    void Update()
    {
        // すでに最後の色に到達していたら、それ以上は何もしない(固定)
        if (reachedFinalColor) return;

        timer += Time.deltaTime;
        float t = timer / transitionDuration;

        int nextIndex = currentIndex + 1;

        // 最後の色に到達する手前の遷移中
        if (nextIndex < colors.Length)
        {
            targetCamera.backgroundColor = Color.Lerp(colors[currentIndex], colors[nextIndex], t);

            if (t >= 1f)
            {
                currentIndex = nextIndex;
                timer = 0f;

                // 最後の色に着いたらフラグを立てて固定
                if (currentIndex == colors.Length - 1)
                {
                    reachedFinalColor = true;
                    targetCamera.backgroundColor = colors[currentIndex];
                }
            }
        }
    }
}