using UnityEngine;

public class OffScreenDestroyer : MonoBehaviour
{
    [Header("プレイエリアの範囲（画面に対する割合、0〜1）")]
    [Range(-0.5f, 1.5f)] [SerializeField] private float viewportMinX = -0.05f;
    [Range(-0.5f, 1.5f)] [SerializeField] private float viewportMaxX = 1.05f;
    [Range(-0.5f, 1.5f)] [SerializeField] private float viewportMinY = -0.05f;
    [Range(-0.5f, 1.5f)] [SerializeField] private float viewportMaxY = 1.05f;

    private Camera mainCamera;
    private float minX, maxX, minY, maxY;

    private void Start()
    {
        mainCamera = Camera.main;
        float z = -mainCamera.transform.position.z;
        Vector3 min = mainCamera.ViewportToWorldPoint(new Vector3(viewportMinX, viewportMinY, z));
        Vector3 max = mainCamera.ViewportToWorldPoint(new Vector3(viewportMaxX, viewportMaxY, z));
        minX = min.x;
        maxX = max.x;
        minY = min.y;
        maxY = max.y;
    }

    private void Update()
    {
        Vector3 pos = transform.position;
        if (pos.x < minX || pos.x > maxX || pos.y < minY || pos.y > maxY)
        {
            Destroy(gameObject);
        }
    }
}