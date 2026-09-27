using UnityEngine;

public class ExplosionAutoDestroy : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 0.3f); // アニメの尺に合わせて調整可
    }
}