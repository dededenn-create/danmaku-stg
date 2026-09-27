using UnityEngine;

/// <summary>
/// プレイヤーの移動範囲を、画面上のUI枠(Frame_Top/Left/Bottom)の内側に制限する。
/// CanvasのUI座標(px)ではなく、Viewport座標(0〜1の割合)でプレイエリアを指定し、
/// カメラを通してワールド座標に変換してからClampする。
///
/// 例: 左枠が画面幅の2.3%、上下枠が画面高さの3.1%、右側UIが60%の位置から始まる場合
///   viewportMinX = 0.023
///   viewportMaxX = 0.6
///   viewportMinY = 0.031
///   viewportMaxY = 0.969 (1 - 0.031)
/// </summary>
public class PlayerMovementLimiter : MonoBehaviour
{
    [Header("プレイエリアの範囲（画面に対する割合、0〜1）")]
    [Range(0f, 1f)] [SerializeField] private float viewportMinX = 0.023f;
    [Range(0f, 1f)] [SerializeField] private float viewportMaxX = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float viewportMinY = 0.031f;
    [Range(0f, 1f)] [SerializeField] private float viewportMaxY = 0.969f;

    private Camera mainCamera;
    private float minX, maxX, minY, maxY;

    private void Start()
    {
        mainCamera = Camera.main;
        RecalculateBounds();
    }

    // 画面サイズが変わる可能性がある場合に呼び直す（普段はStartの1回だけでOK）
    private void RecalculateBounds()
    {
        // カメラがOrthographic前提。Zはカメラからの距離、Playerがいる平面(z=0)に合わせて調整
        float z = -mainCamera.transform.position.z;

        Vector3 min = mainCamera.ViewportToWorldPoint(new Vector3(viewportMinX, viewportMinY, z));
        Vector3 max = mainCamera.ViewportToWorldPoint(new Vector3(viewportMaxX, viewportMaxY, z));

        minX = min.x;
        maxX = max.x;
        minY = min.y;
        maxY = max.y;
    }
    [SerializeField] private float playerHalfWidth = 0.3f;  // Playerスプライトの幅の半分
    [SerializeField] private float playerHalfHeight = 0.3f; // Playerスプライトの高さの半分
    private void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX + playerHalfWidth, maxX - playerHalfWidth);
        pos.y = Mathf.Clamp(pos.y, minY + playerHalfHeight, maxY - playerHalfHeight);
        transform.position = pos;
    }

    // Sceneビューで実際の制限範囲を確認できるようにする
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || mainCamera == null) return;

        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minX + maxX) / 2, (minY + maxY) / 2, 0);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0);
        Gizmos.DrawWireCube(center, size);
    }
}