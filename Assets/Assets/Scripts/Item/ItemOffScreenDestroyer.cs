using UnityEngine;
public class ItemOffScreenDestroyer : MonoBehaviour
{
    [Range(-0.5f, 1.5f)] [SerializeField] private float viewportMinX = -0.1f;
    [Range(-0.5f, 1.5f)] [SerializeField] private float viewportMaxX = 1.1f;
    [Range(-0.5f, 1.5f)] [SerializeField] private float viewportMinY = -0.1f; 
    // maxYチェックはそもそも書かない（上方向は消さない）

    private Camera mainCamera;
    private float minX, maxX, minY;

    private void Start()
    {
        mainCamera = Camera.main;
        float z = -mainCamera.transform.position.z;
        Vector3 min = mainCamera.ViewportToWorldPoint(new Vector3(viewportMinX, viewportMinY, z));
        Vector3 max = mainCamera.ViewportToWorldPoint(new Vector3(viewportMaxX, 1.5f, z));
        minX = min.x;
        maxX = max.x;
        minY = min.y;
    }

    private void Update()
    {
        Vector3 pos = transform.position;
        // 下・左右にはみ出た場合だけ消す。上方向はチェックしない
        if (pos.x < minX || pos.x > maxX || pos.y < minY)
        {
            Destroy(gameObject);
        }
    }
}