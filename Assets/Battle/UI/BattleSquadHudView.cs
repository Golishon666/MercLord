using System;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MercLord.Battle.UI
{
    public sealed class BattleSquadHudView : MonoBehaviour
    {
        private const int MaxVisibleSquads = 6;
        private const float PanelWidth = 300f;
        private const float LabelHeight = 22f;

        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI[] squadLabels;
        [SerializeField] private Image backgroundImage;

        private readonly BattleSquadHudPresenter presenter = new BattleSquadHudPresenter();

        public TextMeshProUGUI TitleLabel => titleLabel;
        public TextMeshProUGUI[] SquadLabels => squadLabels;

        public static BattleSquadHudView CreateRuntime()
        {
            var canvasObject = new GameObject("Battle Squad HUD", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 102;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var view = canvasObject.AddComponent<BattleSquadHudView>();
            view.CreateDefaultLayout();
            return view;
        }

        public void Bind(BattleSession session)
        {
            EnsureLayout();
            presenter.Bind(session);
            Refresh();
        }

        public void Refresh()
        {
            EnsureLayout();
            Render(presenter.BuildSnapshot());
        }

        public void Clear()
        {
            presenter.Dispose();
        }

        private void OnDestroy()
        {
            presenter.Dispose();
        }

        private void Render(BattleSquadHudSnapshot snapshot)
        {
            titleLabel.text = snapshot.HasPlayer ? "Allied squads" : "Squads";
            ClearRows();

            if (!snapshot.HasPlayer)
            {
                squadLabels[0].text = "No player team";
                return;
            }

            if (!snapshot.HasSquads && !snapshot.HasVehicles)
            {
                squadLabels[0].text = "No allied squads";
                return;
            }

            var row = 0;
            if (snapshot.HasVehicles)
            {
                squadLabels[row].text = FormatVehicle(snapshot.Vehicles[0]);
                row++;
            }

            var visibleCount = Math.Min(snapshot.Squads.Count, MaxVisibleSquads - row);
            for (var index = 0; index < visibleCount; index++)
            {
                squadLabels[row + index].text = FormatSquad(snapshot.Squads[index]);
            }

            if (snapshot.Squads.Count > visibleCount && MaxVisibleSquads > 0)
            {
                squadLabels[MaxVisibleSquads - 1].text = $"+{snapshot.Squads.Count - visibleCount} squads";
            }
        }

        private void EnsureLayout()
        {
            if (titleLabel != null &&
                squadLabels != null &&
                squadLabels.Length == MaxVisibleSquads &&
                backgroundImage != null)
            {
                return;
            }

            CreateDefaultLayout();
        }

        private void CreateDefaultLayout()
        {
            var rect = transform as RectTransform;
            if (rect == null)
            {
                rect = gameObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -150f);
            rect.sizeDelta = new Vector2(PanelWidth, LabelHeight * (MaxVisibleSquads + 1) + 12f);

            backgroundImage ??= gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.02f, 0.025f, 0.03f, 0.68f);
            backgroundImage.raycastTarget = false;

            titleLabel ??= CreateLabel("Squad Title", 0, 16f, FontStyles.Bold);
            if (squadLabels == null || squadLabels.Length != MaxVisibleSquads)
            {
                squadLabels = new TextMeshProUGUI[MaxVisibleSquads];
            }

            for (var index = 0; index < MaxVisibleSquads; index++)
            {
                squadLabels[index] ??= CreateLabel($"Squad Row {index + 1}", index + 1, 14f, FontStyles.Normal);
            }
        }

        private TextMeshProUGUI CreateLabel(string labelName, int row, float fontSize, FontStyles style)
        {
            var labelObject = new GameObject(labelName, typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(10f, -6f - LabelHeight * row);
            rect.sizeDelta = new Vector2(PanelWidth - 20f, LabelHeight);

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Left;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private void ClearRows()
        {
            for (var index = 0; index < squadLabels.Length; index++)
            {
                squadLabels[index].text = string.Empty;
            }
        }

        private static string FormatSquad(BattleSquadHudEntry entry)
        {
            var order = entry.HasOrder ? FormatOrder(entry.Order) : "No order";
            var morale = entry.HasMorale
                ? $" M{Mathf.RoundToInt(entry.MoralePercent * 100f)}%"
                : string.Empty;
            var routed = entry.IsRouted ? " Routed" : string.Empty;
            return $"S{entry.SquadId} U{entry.UnitConfigId} {entry.AliveCount}/{entry.TotalCount} {order}{morale}{routed}";
        }

        private static string FormatVehicle(BattleVehicleHudEntry entry)
        {
            return $"Vehicle V{entry.VehicleConfigId} HP {entry.CurrentHealth}/{entry.MaxHealth} {FormatVehicleState(entry.State)}";
        }

        private static string FormatVehicleState(VehicleStateType state)
        {
            switch (state)
            {
                case VehicleStateType.PlayerControlled:
                    return "Player";
                case VehicleStateType.AIControlled:
                    return "AI";
                case VehicleStateType.Destroyed:
                    return "Destroyed";
                default:
                    return "Empty";
            }
        }

        private static string FormatOrder(SquadOrderType order)
        {
            switch (order)
            {
                case SquadOrderType.FollowPlayer:
                    return "Follow";
                case SquadOrderType.HoldPosition:
                    return "Hold";
                case SquadOrderType.Retreat:
                    return "Retreat";
                default:
                    return "Attack";
            }
        }
    }
}
