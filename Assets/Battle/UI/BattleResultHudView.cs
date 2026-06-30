using System;
using MercLord.Battle.Generation;
using MercLord.Global.Cells;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MercLord.Battle.UI
{
    public sealed class BattleResultHudView : MonoBehaviour
    {
        private const float PanelWidth = 420f;
        private const float PanelHeight = 214f;
        private const float Padding = 18f;
        private const float ButtonWidth = 140f;
        private const float ButtonHeight = 36f;

        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI summaryLabel;
        [SerializeField] private Button continueButton;

        private Action continueCallback;

        public RectTransform PanelRoot => panelRoot;
        public TextMeshProUGUI TitleLabel => titleLabel;
        public TextMeshProUGUI SummaryLabel => summaryLabel;
        public Button ContinueButton => continueButton;
        public bool IsVisible => panelRoot != null && panelRoot.gameObject.activeSelf;

        public static BattleResultHudView CreateRuntime()
        {
            EnsureEventSystem();

            var canvasObject = new GameObject("Battle Result HUD", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var view = canvasObject.AddComponent<BattleResultHudView>();
            view.CreateDefaultLayout();
            view.Hide();
            return view;
        }

        public void Show(BattleResult result, Action onContinue)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            EnsureLayout();
            continueCallback = onContinue;
            titleLabel.text = FormatOutcome(result.Outcome);
            summaryLabel.text = BuildSummary(result);
            panelRoot.gameObject.SetActive(true);
            SetInteractable(true);
        }

        public void Hide()
        {
            continueCallback = null;
            if (panelRoot != null)
            {
                panelRoot.gameObject.SetActive(false);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (continueButton != null)
            {
                continueButton.interactable = interactable;
            }
        }

        private void EnsureLayout()
        {
            if (panelRoot != null &&
                titleLabel != null &&
                summaryLabel != null &&
                continueButton != null)
            {
                return;
            }

            CreateDefaultLayout();
        }

        private void CreateDefaultLayout()
        {
            panelRoot ??= CreatePanelRoot();
            titleLabel ??= CreateLabel("Title", new Vector2(Padding, -Padding), PanelWidth - Padding * 2f, 34f, 22f);
            summaryLabel ??= CreateLabel("Summary", new Vector2(Padding, -58f), PanelWidth - Padding * 2f, 92f, 15f);
            continueButton ??= CreateButton(
                "Continue Button",
                "Continue",
                new Vector2(PanelWidth - Padding - ButtonWidth, Padding));

            continueButton.onClick.RemoveListener(OnContinueClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        private RectTransform CreatePanelRoot()
        {
            var rootObject = new GameObject("Result Panel", typeof(RectTransform));
            rootObject.transform.SetParent(transform, false);
            var rect = rootObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var background = rootObject.AddComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.06f, 0.92f);
            return rect;
        }

        private TextMeshProUGUI CreateLabel(
            string labelName,
            Vector2 anchoredPosition,
            float width,
            float height,
            float fontSize)
        {
            var labelObject = new GameObject(labelName, typeof(RectTransform));
            labelObject.transform.SetParent(panelRoot, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(width, height);

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Left;
            label.raycastTarget = false;
            return label;
        }

        private Button CreateButton(string buttonName, string labelText, Vector2 anchoredPosition)
        {
            var buttonObject = new GameObject(buttonName, typeof(RectTransform));
            buttonObject.transform.SetParent(panelRoot, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.22f, 0.24f, 0.96f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var label = CreateButtonLabel(buttonObject.transform, labelText);
            label.raycastTarget = false;
            return button;
        }

        private TextMeshProUGUI CreateButtonLabel(Transform parent, string labelText)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 15f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private void OnContinueClicked()
        {
            continueCallback?.Invoke();
        }

        private static string BuildSummary(BattleResult result)
        {
            var lootAmount = 0;
            var loot = result.Loot ?? Array.Empty<BattleLootEntry>();
            for (var lootIndex = 0; lootIndex < loot.Length; lootIndex++)
            {
                if (loot[lootIndex].Amount > 0)
                {
                    lootAmount += loot[lootIndex].Amount;
                }
            }

            var survivingSquads = 0;
            var party = result.PlayerParty ?? Array.Empty<SquadData>();
            for (var squadIndex = 0; squadIndex < party.Length; squadIndex++)
            {
                if (party[squadIndex].Count > 0)
                {
                    survivingSquads++;
                }
            }

            return
                $"Player survived: {(result.PlayerSurvived ? "Yes" : "No")}\n" +
                $"Credits: {result.CreditsReward}\n" +
                $"Loot items: {lootAmount}\n" +
                $"Surviving party squads: {survivingSquads}";
        }

        private static string FormatOutcome(BattleOutcome outcome)
        {
            switch (outcome)
            {
                case BattleOutcome.AttackerVictory:
                    return "Attacker victory";
                case BattleOutcome.DefenderVictory:
                    return "Defender victory";
                case BattleOutcome.Retreat:
                    return "Retreat";
                default:
                    return "Battle complete";
            }
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }
}
