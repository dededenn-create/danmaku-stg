using UnityEngine;
using TMPro;

public class PowerUIDisplay : MonoBehaviour
{
    public TextMeshProUGUI powerLabelText;
    public TextMeshProUGUI powerValueText;

    void Start()
    {
        powerLabelText.text = "Power";
    }

    void Update()
    {
        var status = PlayerStatus.Instance;
        if (status == null) return;

        string fraction;
        string levelText;

        if (status.power >= status.maxPower)
        {
            fraction = $"{status.maxPower}/{status.maxPower}";
            levelText = "MAX";
        }
        else
        {
            fraction = $"{status.CurrentSegmentNumerator}/{status.CurrentSegmentDenominator}";
            levelText = $"Lv.{status.ShotLevel}";
        }

        powerValueText.text = $"{levelText}  {fraction}";
    }
}