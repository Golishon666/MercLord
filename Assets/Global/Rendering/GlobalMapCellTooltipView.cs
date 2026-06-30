using System.Text;
using MercLord.Global.Cells;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MercLord.Global.Rendering
{
    public sealed class GlobalMapCellTooltipView : MonoBehaviour
    {
        private const float TooltipWidth = 250f;
        private const float TooltipPadding = 12f;
        private const float TooltipOffset = 18f;
        private const float MinTooltipHeight = 134f;
        private const string DatePrefix = "\u0414\u0430\u0442\u0430: \u0434\u0435\u043d\u044c ";
        private const string CellPrefix = "\u042f\u0447\u0435\u0439\u043a\u0430 ";
        private const string BiomeLabel = "\u0411\u0438\u043e\u043c: ";
        private const string FactionLabel = "\u0424\u0440\u0430\u043a\u0446\u0438\u044f: ";
        private const string ResourcesLabel = "\u0420\u0435\u0441\u0443\u0440\u0441\u044b: ";
        private const string MovementLabel = "\u0414\u0432\u0438\u0436\u0435\u043d\u0438\u0435: ";
        private const string ImpassableLabel = "\u043d\u0435\u043f\u0440\u043e\u0445\u043e\u0434\u0438\u043c\u043e";
        private const string RoadLabel = "\u0414\u043e\u0440\u043e\u0433\u0430: ";
        private const string RiverLabel = "\u0420\u0435\u043a\u0430: \u043f\u043e\u0442\u043e\u043a ";
        private const string WaterDistanceLabel = "\u0414\u043e \u0432\u043e\u0434\u044b: ";

        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform tooltipRoot;
        [SerializeField] private TMP_Text tooltipTitle;
        [SerializeField] private TMP_Text tooltipBody;
        [SerializeField] private TMP_Text dateLabel;

        private readonly StringBuilder bodyBuilder = new();

        public void Configure(Canvas parentCanvas, RectTransform root, TMP_Text title, TMP_Text body, TMP_Text date)
        {
            canvas = parentCanvas;
            tooltipRoot = root;
            tooltipTitle = title;
            tooltipBody = body;
            dateLabel = date;
            Hide();
        }

        public void SetDate(int day)
        {
            if (dateLabel == null)
            {
                return;
            }

            dateLabel.text = $"{DatePrefix}{day}";
        }

        public void Show(
            WorldCell cell,
            string factionName,
            Vector2 screenPosition)
        {
            if (tooltipRoot == null || tooltipTitle == null || tooltipBody == null)
            {
                return;
            }

            tooltipTitle.text = $"{CellPrefix}{cell.Id}";
            tooltipBody.text = BuildBody(cell, factionName);
            tooltipRoot.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);

            var height = Mathf.Max(
                MinTooltipHeight,
                tooltipTitle.preferredHeight + tooltipBody.preferredHeight + TooltipPadding * 3f);
            tooltipRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, TooltipWidth);
            tooltipRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            tooltipRoot.anchoredPosition = ClampTooltipPosition(screenPosition, height);
        }

        public void Hide()
        {
            if (tooltipRoot != null)
            {
                tooltipRoot.gameObject.SetActive(false);
            }
        }

        private string BuildBody(WorldCell cell, string factionName)
        {
            bodyBuilder.Clear();
            bodyBuilder.Append(BiomeLabel).Append(cell.Biome).AppendLine();
            bodyBuilder.Append(FactionLabel).Append(factionName).AppendLine();
            bodyBuilder.Append(ResourcesLabel).Append(cell.ResourceAmount).AppendLine();
            bodyBuilder.Append(MovementLabel);
            bodyBuilder.Append(cell.MovementCost >= WorldMovementCosts.ImpassableCost
                ? ImpassableLabel
                : cell.MovementCost.ToString());

            if (cell.HasRoad)
            {
                bodyBuilder.AppendLine().Append(RoadLabel).Append(cell.RoadType);
            }

            if (cell.HasRiver)
            {
                bodyBuilder.AppendLine().Append(RiverLabel).Append(cell.RiverFlow.ToString("0.0"));
            }

            if (cell.DistanceToWater != int.MaxValue)
            {
                bodyBuilder.AppendLine().Append(WaterDistanceLabel).Append(cell.DistanceToWater);
            }

            return bodyBuilder.ToString();
        }

        private static Vector2 ClampTooltipPosition(Vector2 screenPosition, float height)
        {
            var x = screenPosition.x + TooltipOffset;
            var y = screenPosition.y - TooltipOffset;
            x = Mathf.Clamp(x, 8f, Screen.width - TooltipWidth - 8f);
            y = Mathf.Clamp(y, height + 8f, Screen.height - 8f);
            return new Vector2(x, y);
        }
    }
}
