using UnityEngine;

public class ParticleSpeedController : MonoBehaviour
{
    private ParticleSystem ps;
    private float baseSimulationSpeed;
    private float targetMultiplier = 1f;
    private float currentMultiplier = 1f;
    public float transitionDuration = 2f;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        baseSimulationSpeed = ps.main.simulationSpeed;
    }

    void Update()
    {
        currentMultiplier = Mathf.MoveTowards(
            currentMultiplier,
            targetMultiplier,
            Time.deltaTime / transitionDuration
        );

        var main = ps.main;
        main.simulationSpeed = baseSimulationSpeed * currentMultiplier;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        targetMultiplier = multiplier;
    }
}