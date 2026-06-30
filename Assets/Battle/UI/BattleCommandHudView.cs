using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MercLord.Battle.UI
{
    public sealed class BattleCommandHudView : MonoBehaviour
    {
        private const float PanelWidth = 520f;
        private const float PanelHeight = 58f;

        [SerializeField] private TextMeshProUGUI currentOrderLabel;
        [SerializeField] private TextMeshProUGUI commandHelpLabel;

        private readonly BattleCommandHudPresenter presenter = new BattleCommandHudPresenter();

        public TextMeshProUGUI CurrentOrderLabel => currentOrderLabel;
        public TextMeshProUGUI CommandHelpLabel => commandHelpLabel;

        public static BattleCommandHudView CreateRuntime()
        {
            var canvasObject = new GameObject("Battle Command HUD", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 105;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var view = canvasObject.AddComponent<BattleCommandHudView>();
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

        private void Render(BattleCommandHudSnapshot snapshot)
        {
            currentOrderLabel.text = FormatCurrentOrder(snapshot);
            commandHelpLabel.text = "F1 Follow  F2 Hold  F3 Attack  F4 Retreat";
        }

        private void EnsureLayout()
        {
            if (currentOrderLabel != null && commandHelpLabel != null)
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

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 18f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var image = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.06f, 0.78f);
            image.raycastTarget = false;

            currentOrderLabel ??= CreateLabel("Current Order", 0f, 18f);
            commandHelpLabel ??= CreateLabel("Command Help", -25f, 15f);
        }

        private TextMeshProUGUI CreateLabel(string labelName, float y, float fontSize)
        {
            var labelObject = new GameObject(labelName, typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 0f);
            rect.offsetMax = new Vector2(-12f, 0f);
            rect.anchoredPosition = new Vector2(0f, y);

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static string FormatCurrentOrder(BattleCommandHudSnapshot snapshot)
        {
            if (!snapshot.HasPlayer)
            {
                return "Squad command: no player";
            }

            if (!snapshot.HasSquads)
            {
                return "Squad command: no squads";
            }

            var suffix = snapshot.MixedOrderCount > 0 ? " mixed" : string.Empty;
            return $"Squad command: {FormatOrder(snapshot.CurrentOrder)} ({snapshot.FriendlySquadCount} squads{suffix})";
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
