using UnityEngine;

public class FrameRateSetter : MonoBehaviour
{
    void Awake()
    {
        Application.targetFrameRate = 60;
    }
}