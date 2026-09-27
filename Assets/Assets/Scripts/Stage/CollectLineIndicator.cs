using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CollectLineIndicator : MonoBehaviour
{
    [Header("参照元")]
    public FullCollectTrigger fullCollectTrigger;
    public Camera mainCamera;
    public RectTransform canvasRectTransform;

    [Header("見た目")]
    public RectTransform coreLine;
    public RectTransform glowLine;
    public Image coreImage;
    public Image glowImage;
    public Color lineColor = new Color(1f, 0.92f, 0.3f); // 黄色系

    [Header("テキスト")]
    public RectTransform labelTransform;
    public TextMeshProUGUI labelText;
    public float labelOffsetY = 30f; // ラインより上にどれだけずらすか

    [Header("タイミング")]
    public float displayDuration = 3f;
    public float fadeDuration = 1f;

    void Start()
    {
        if (fullCollectTrigger != null && mainCamera != null && canvasRectTransform != null)
        {
            float cameraY = mainCamera.transform.position.y;
            float orthoSize = mainCamera.orthographicSize;
            float canvasHeight = canvasRectTransform.rect.height;
            float pixelsPerUnit = canvasHeight / (orthoSize * 2f);

            float screenY = (fullCollectTrigger.fullCollectLineY - cameraY) * pixelsPerUnit;

            SetPosY(coreLine, screenY);
            SetPosY(glowLine, screenY);
            SetPosY(labelTransform, screenY + labelOffsetY); // ラインより少し上に配置
        }

        SetAlpha(coreImage, 0f);
        SetAlpha(glowImage, 0f);
        SetTextAlpha(labelText, 0f);

        StartCoroutine(ShowLineRoutine());
    }

    void SetPosY(RectTransform rt, float y)
    {
        if (rt == null) return;
        Vector2 pos = rt.anchoredPosition;
        pos.y = y;
        rt.anchoredPosition = pos;
    }

    IEnumerator ShowLineRoutine()
    {
        yield return StartCoroutine(FadeBoth(0f, 1f, fadeDuration));
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(FadeBoth(1f, 0f, fadeDuration));
    }

    IEnumerator FadeBoth(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            SetAlpha(coreImage, a);
            SetAlpha(glowImage, a * 0.5f);
            SetTextAlpha(labelText, a);
            yield return null;
        }
        SetAlpha(coreImage, to);
        SetAlpha(glowImage, to * 0.5f);
        SetTextAlpha(labelText, to);
    }

    void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = lineColor;
        c.a = alpha;
        img.color = c;
    }

    void SetTextAlpha(TextMeshProUGUI txt, float alpha)
    {
        if (txt == null) return;
        Color c = txt.color;
        c.a = alpha;
        txt.color = c;
    }
}