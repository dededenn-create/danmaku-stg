using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthGaugeUI : MonoBehaviour
{
    [SerializeField] private GameObject gaugeRoot;
    [SerializeField] private Image fillImage;

    [Header("Enemyラベルのフェードイン")]
    [SerializeField] private CanvasGroup enemyLabel; // "Enemy"の文字にCanvasGroupをつけてセット
    [SerializeField] private float labelFadeDuration = 0.5f;

    private void Awake()
    {
        if (gaugeRoot != null)
            gaugeRoot.SetActive(false);

        if (fillImage != null)
            fillImage.fillAmount = 0f;

        if (enemyLabel != null)
            enemyLabel.alpha = 0f;
    }

    public void Show()
    {
        if (gaugeRoot != null)
            gaugeRoot.SetActive(true);

        if (fillImage != null)
            fillImage.fillAmount = 0f;

        if (enemyLabel != null)
        {
            enemyLabel.alpha = 0f;
            StartCoroutine(FadeInLabel());
        }
    }

    private IEnumerator FadeInLabel()
    {
        float t = 0f;
        while (t < labelFadeDuration)
        {
            t += Time.deltaTime;
            enemyLabel.alpha = Mathf.Lerp(0f, 1f, t / labelFadeDuration);
            yield return null;
        }
        enemyLabel.alpha = 1f;
    }

    public void SetFill(float value)
    {
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(value);
    }

    public void Hide()
    {
        if (gaugeRoot != null)
            gaugeRoot.SetActive(false);

        if (enemyLabel != null)
            enemyLabel.alpha = 0f;
    }
}