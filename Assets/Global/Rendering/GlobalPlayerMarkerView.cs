using UnityEngine;

namespace MercLord.Global.Rendering
{
    public sealed class GlobalPlayerMarkerView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bodyRenderer;

        public SpriteRenderer BodyRenderer => bodyRenderer;

        public void Bind(Color markerColor)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = markerColor;
            }
        }
    }
}
