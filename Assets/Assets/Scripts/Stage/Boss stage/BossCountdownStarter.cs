using UnityEngine;

public class BossCountdownStarter : MonoBehaviour
{
    public float delayAfterSpawn = 5.0f;

    private void Start()
    {
        BossSequenceManager.Instance.StartCountdown(delayAfterSpawn);
    }
}