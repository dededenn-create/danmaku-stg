using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    public float updateInterval = 0.5f;

    private float timeSinceLastUpdate = 0f;
    private int frameCount = 0;

    void Update()
    {
        frameCount++;
        timeSinceLastUpdate += Time.unscaledDeltaTime;

        if (timeSinceLastUpdate >= updateInterval)
        {
            float fps = frameCount / timeSinceLastUpdate;
            if (fpsText != null)
            {
                fpsText.text = Mathf.RoundToInt(fps).ToString();
            }

            frameCount = 0;
            timeSinceLastUpdate = 0f;
        }
    }
}