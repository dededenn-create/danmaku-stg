using UnityEngine;

public class EnemyOffScreenDestroy : MonoBehaviour
{
    [Header("プレイエリアの範囲（画面に対する割合、0〜1）")]
    [Range(0f, 1f)] [SerializeField] private float viewportMinX = 0.023f;
    [Range(0f, 1f)] [SerializeField] private float viewportMaxX = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float viewportMinY = -0.2f;  // 敵は上に少しはみ出て出現するので下限を負に
    [Range(0f, 1f)] [SerializeField] private float viewportMaxY = 1.2f;   // 出現時に画面外(上)にいる分、上限を広げる

    public float graceTime = 1.5f;

    private Camera mainCamera;
    private float minX, maxX, minY, maxY;
    private float timer = 0f;

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
        timer += Time.deltaTime;
        if (timer < graceTime) return;

        Vector3 pos = transform.position;
        if (pos.x < minX || pos.x > maxX || pos.y < minY || pos.y > maxY)
        {
            Destroy(gameObject);
        }
    }
}