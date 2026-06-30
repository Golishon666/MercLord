using System;
using MercLord.Game.StateMachine;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MercLord.Global.UI
{
    public sealed class GlobalBattleEncounterPromptView : MonoBehaviour
    {
        private const float PanelWidth = 430f;
        private const float PanelHeight = 166f;
        private const float Padding = 16f;
        private const float ButtonWidth = 128f;
        private const float ButtonHeight = 34f;

        [SerializeField] private RectTransform promptRoot;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI bodyLabel;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button cancelButton;

        private Action attackCallback;
        private Action cancelCallback;

        public RectTransform PromptRoot => promptRoot;
        public TextMeshProUGUI TitleLabel => titleLabel;
        public TextMeshProUGUI BodyLabel => bodyLabel;
        public Button AttackButton => attackButton;
        public Button CancelButton => cancelButton;
        public GlobalBattleEncounter CurrentEncounter { get; private set; }
        public bool IsVisible => promptRoot != null && promptRoot.gameObject.activeSelf;

        public static GlobalBattleEncounterPromptView CreateRuntime()
        {
            EnsureEventSystem();

            var canvasObject = new GameObject("Global Battle Encounter Prompt", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var view = canvasObject.AddComponent<GlobalBattleEncounterPromptView>();
            view.CreateDefaultLayout();
            view.Hide();
            return view;
        }

        public void Show(
            GlobalBattleEncounter encounter,
            string opponentFactionName,
            Action onAttack,
            Action onCancel)
        {
            EnsureLayout();
            CurrentEncounter = encounter;
            attackCallback = onAttack;
            cancelCallback = onCancel;

            titleLabel.text = $"Enemy army {encounter.OpponentArmyId}";
            bodyLabel.text =
                $"Faction: {opponentFactionName}\n" +
                $"Forces: {encounter.PlayerUnitCount} vs {encounter.OpponentUnitCount}\n" +
                $"Cell: {encounter.CellId}";
            promptRoot.gameObject.SetActive(true);
            SetInteractable(true);
        }

        public void Hide()
        {
            attackCallback = null;
            cancelCallback = null;
            if (promptRoot != null)
            {
                promptRoot.gameObject.SetActive(false);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (attackButton != null)
            {
                attackButton.interactable = interactable;
            }

            if (cancelButton != null)
            {
                cancelButton.interactable = interactable;
            }
        }

        private void EnsureLayout()
        {
            if (promptRoot != null &&
                titleLabel != null &&
                bodyLabel != null &&
                attackButton != null &&
                cancelButton != null)
            {
                return;
            }

            CreateDefaultLayout();
        }

        private void CreateDefaultLayout()
        {
            promptRoot ??= CreatePromptRoot();
            titleLabel ??= CreateLabel("Title", new Vector2(Padding, -Padding), PanelWidth - Padding * 2f, 28f, 20f);
            bodyLabel ??= CreateLabel("Body", new Vector2(Padding, -48f), PanelWidth - Padding * 2f, 58f, 15f);
            attackButton ??= CreateButton("Attack Button", "Attack", new Vector2(PanelWidth - Padding - ButtonWidth, Padding));
            cancelButton ??= CreateButton("Cancel Button", "Cancel", new Vector2(PanelWidth - Padding * 2f - ButtonWidth * 2f, Padding));

            attackButton.onClick.RemoveListener(OnAttackClicked);
            cancelButton.onClick.RemoveListener(OnCancelClicked);
            attackButton.onClick.AddListener(OnAttackClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private RectTransform CreatePromptRoot()
        {
            var rootObject = new GameObject("Encounter Panel", typeof(RectTransform));
            rootObject.transform.SetParent(transform, false);
            var rect = rootObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 24f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var background = rootObject.AddComponent<Image>();
            background.color = new Color(0.05f, 0.06f, 0.07f, 0.88f);
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
            labelObject.transform.SetParent(promptRoot, false);
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
            buttonObject.transform.SetParent(promptRoot, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.21f, 0.23f, 0.96f);
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

        private void OnAttackClicked()
        {
            attackCallback?.Invoke();
        }

        private void OnCancelClicked()
        {
            cancelCallback?.Invoke();
            Hide();
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
