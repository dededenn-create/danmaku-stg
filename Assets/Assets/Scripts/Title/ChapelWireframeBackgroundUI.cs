using System.Collections.Generic;
using UnityEngine;

namespace STG.Background
{
    /// <summary>
    /// タイトル画面用の背景ワイヤーフレーム。
    ///
    /// 画面の4辺それぞれで「角から対角へ向かう曲線」を複数本、
    /// 膨らみの深さをわずかにずらしながら密着させて重ねる。
    /// 中央付近は空白のまま残るので、タイトルロゴを乗せられる。
    ///
    /// 角の座標・膨らみの深さはすべて RectTransform の実サイズから
    /// 自動計算するため、Canvasの解像度が変わっても数値を手で
    /// 打ち直す必要がない(割合指定)。
    ///
    /// 使い方:
    ///   1. Canvas配下に、画面いっぱいに広げたRectTransformを持つ
    ///      空のGameObjectを作る(Anchor Presetsで"stretch"にするなど)
    ///   2. このスクリプトをアタッチする
    ///   3. Hierarchy上でTitleLogo等より上(=描画順が奥)に配置する
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CornerArchClusterBackgroundUI : MonoBehaviour
    {
        [Header("共通設定")]
        [SerializeField] private Color lineColor = new Color32(0xD4, 0xAF, 0x37, 0xFF);
        [SerializeField] private float lineWidth = 2f;

        [Header("アーチ設定")]
        [Tooltip("1辺あたりの本数(上下左右それぞれこの本数になる)")]
        [SerializeField, Range(1, 24)] private int archCountPerSide = 8;

        [Tooltip("画面の角からの余白(px)。角ぴったりから生やしたくない場合に使う")]
        [SerializeField] private float cornerInset = 20f;

        [Tooltip("膨らみの深さの下限(画面サイズに対する割合、0〜1)")]
        [SerializeField, Range(0f, 1f)] private float bulgeDepthMinFraction = 0.70f;

        [Tooltip("膨らみの深さの上限(画面サイズに対する割合、0〜1)。上限と下限を近づけるほど密着した束になる")]
        [SerializeField, Range(0f, 1f)] private float bulgeDepthMaxFraction = 0.88f;

        [Tooltip("曲線1本を何本の直線で近似するか。多いほど滑らかだが線分数が増える")]
        [SerializeField, Range(4, 40)] private int curveSegments = 20;

        [Header("安全策")]
        [Tooltip("生成した合計線分数・頂点数をConsoleにログ出力する。UIメッシュ1つの頂点上限は65,535。")]
        [SerializeField] private bool logSegmentCountOnBuild = true;

        private void Awake()
        {
            int totalSegments = Build();

            if (logSegmentCountOnBuild)
            {
                int totalVerts = totalSegments * 4; // 線分1本=四角形1枚=頂点4個
                Debug.Log($"[CornerArchClusterBackgroundUI] 直線近似の線分数: {totalSegments} " +
                          $"(頂点数 約{totalVerts} / UIメッシュ上限65,535)");
                if (totalVerts > 30000)
                {
                    Debug.LogWarning("[CornerArchClusterBackgroundUI] 頂点数がかなり多くなっています。" +
                                      "archCountPerSide や curveSegments を見直してください。");
                }
            }
        }

        private int Build()
        {
            var rectTransform = (RectTransform)transform;
            Rect r = rectTransform.rect;
            float halfW = r.width * 0.5f;
            float halfH = r.height * 0.5f;

            Vector2 topLeft = new Vector2(-halfW + cornerInset, halfH - cornerInset);
            Vector2 topRight = new Vector2(halfW - cornerInset, halfH - cornerInset);
            Vector2 bottomLeft = new Vector2(-halfW + cornerInset, -halfH + cornerInset);
            Vector2 bottomRight = new Vector2(halfW - cornerInset, -halfH + cornerInset);

            var segments = new List<(Vector2, Vector2)>();

            // 上辺：右上→左上、下辺：左下→右下、左辺：左上→左下、右辺：右上→右下
            AddArchSet(segments, topRight, topLeft, horizontalPair: true, refHalfSize: halfH);
            AddArchSet(segments, bottomLeft, bottomRight, horizontalPair: true, refHalfSize: halfH);
            AddArchSet(segments, topLeft, bottomLeft, horizontalPair: false, refHalfSize: halfW);
            AddArchSet(segments, topRight, bottomRight, horizontalPair: false, refHalfSize: halfW);

            var meshGo = new GameObject("ArchCluster", typeof(RectTransform));
            var meshRect = (RectTransform)meshGo.transform;
            meshRect.SetParent(transform, false);
            meshRect.anchorMin = new Vector2(0.5f, 0.5f);
            meshRect.anchorMax = new Vector2(0.5f, 0.5f);
            meshRect.pivot = new Vector2(0.5f, 0.5f);
            meshRect.anchoredPosition = Vector2.zero;
            meshRect.sizeDelta = Vector2.zero;

            var mesh = meshGo.AddComponent<UIWireframeMesh>();
            mesh.color = lineColor;
            mesh.raycastTarget = false;
            mesh.LineThickness = lineWidth;
            mesh.SetSegments(segments);

            return segments.Count;
        }

        /// <summary>
        /// start→end(角から角、固定)を結ぶ曲線を archCountPerSide 本、
        /// 膨らみの深さだけをわずかにずらしながら生成する。
        /// horizontalPair=true のときは上下辺用(制御点をY方向に動かす)、
        /// false のときは左右辺用(制御点をX方向に動かす)。
        /// </summary>
        private void AddArchSet(List<(Vector2, Vector2)> segs, Vector2 start, Vector2 end, bool horizontalPair, float refHalfSize)
        {
            int count = Mathf.Max(1, archCountPerSide);

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float depthFrac = Mathf.Lerp(bulgeDepthMinFraction, bulgeDepthMaxFraction, t);
                float depth = depthFrac * refHalfSize;

                Vector2 control;
                if (horizontalPair)
                {
                    float edgeY = start.y; // 上辺なら+、下辺なら-
                    float controlY = edgeY - Mathf.Sign(edgeY) * depth; // 中央(0)に向かって寄せる
                    control = new Vector2(0f, controlY);
                }
                else
                {
                    float edgeX = start.x; // 左辺なら-、右辺なら+
                    float controlX = edgeX - Mathf.Sign(edgeX) * depth;
                    control = new Vector2(controlX, 0f);
                }

                AddQuadBezierSegments(segs, start, control, end, curveSegments);
            }
        }

        /// <summary>
        /// 2次ベジェ曲線(始点p0・制御点p1・終点p2)を、直線の集まりとして近似する。
        /// UIWireframeMeshは直線しか扱えないため、ここで曲線を分割する。
        /// </summary>
        private static void AddQuadBezierSegments(List<(Vector2, Vector2)> segs, Vector2 p0, Vector2 p1, Vector2 p2, int segments)
        {
            Vector2 prev = p0;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float u = 1f - t;
                Vector2 p = u * u * p0 + 2f * u * t * p1 + t * t * p2;
                segs.Add((prev, p));
                prev = p;
            }
        }
    }
}