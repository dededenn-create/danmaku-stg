using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace STG.Background
{
    /// <summary>
    /// 複数の線分(始点・終点のペア)を、1枚のメッシュとしてUI上に描画する
    /// 軽量な「UI版LineRenderer」。CanvasのRectTransform配下で使う。
    ///
    /// 通常のLineRenderer(ワールド空間)と違い、
    ///   ・Canvas Scalerの解像度スケーリングに自動追従する
    ///   ・親Canvasの描画順(Sibling Index)にそのまま従う
    ///   ・何本線があっても1つのCanvasRenderer/ドローコールにまとめられる
    /// というメリットがある。
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIWireframeMesh : MaskableGraphic
    {
        [SerializeField] private float lineThickness = 3f; // UI単位(px相当)

        private readonly List<(Vector2 a, Vector2 b)> _segments = new List<(Vector2, Vector2)>();

        public float LineThickness
        {
            get => lineThickness;
            set { lineThickness = value; SetVerticesDirty(); }
        }

        /// <summary>
        /// 描画する線分をまとめて設定し、メッシュを再構築する。
        /// 生成時に一度だけ呼ぶ想定(毎フレーム呼ばない)。
        /// </summary>
        public void SetSegments(List<(Vector2 a, Vector2 b)> segments)
        {
            _segments.Clear();
            _segments.AddRange(segments);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Color32 col = color;
            foreach (var (a, b) in _segments)
            {
                AddLineQuad(vh, a, b, lineThickness, col);
            }
        }

        private static void AddLineQuad(VertexHelper vh, Vector2 a, Vector2 b, float width, Color32 col)
        {
            Vector2 dir = b - a;
            float len = dir.magnitude;
            if (len < 0.0001f) return;
            dir /= len;
            Vector2 normal = new Vector2(-dir.y, dir.x) * (width * 0.5f);

            int idx = vh.currentVertCount;
            vh.AddVert(a - normal, col, Vector2.zero);
            vh.AddVert(a + normal, col, Vector2.zero);
            vh.AddVert(b + normal, col, Vector2.zero);
            vh.AddVert(b - normal, col, Vector2.zero);

            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }
    }
}
