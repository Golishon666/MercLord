using UnityEngine;

namespace MercLord.Global.Rendering
{
    public sealed class GlobalCellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer biomeRenderer;
        [SerializeField] private SpriteRenderer influenceOverlayRenderer;
        [SerializeField] private Transform markerRoot;

        public int CellId { get; private set; }
        public SpriteRenderer BiomeRenderer => biomeRenderer;
        public SpriteRenderer InfluenceOverlayRenderer => influenceOverlayRenderer;
        public Transform MarkerRoot => markerRoot;

        public void Bind(int cellId, Color biomeColor, Color influenceColor)
        {
            CellId = cellId;

            if (biomeRenderer != null)
            {
                biomeRenderer.color = biomeColor;
            }

            if (influenceOverlayRenderer != null)
            {
                influenceOverlayRenderer.color = influenceColor;
            }
        }
    }
}
