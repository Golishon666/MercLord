using TMPro;
using MercLord.Global.Cells;
using UnityEngine;

namespace MercLord.Global.Rendering
{
    public sealed class GlobalArmyMarkerView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private TextMeshPro label;

        public SpriteRenderer BodyRenderer => bodyRenderer;
        public TextMeshPro Label => label;

        public void Bind(ArmyData army, Color factionColor)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = factionColor;
            }

            if (label != null)
            {
                label.text = army.Id.ToString();
            }
        }
    }
}
