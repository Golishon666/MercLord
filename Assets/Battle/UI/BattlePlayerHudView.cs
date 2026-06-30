using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MercLord.Battle.UI
{
    public sealed class BattlePlayerHudView : MonoBehaviour
    {
        private const float PanelWidth = 330f;
        private const float LabelHeight = 22f;
        private const int RowCount = 6;

        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI healthLabel;
        [SerializeField] private TextMeshProUGUI weaponLabel;
        [SerializeField] private TextMeshProUGUI cooldownLabel;
        [SerializeField] private TextMeshProUGUI armorLabel;
        [SerializeField] private TextMeshProUGUI targetLabel;

        private readonly BattlePlayerHudPresenter presenter = new BattlePlayerHudPresenter();

        public TextMeshProUGUI TitleLabel => titleLabel;
        public TextMeshProUGUI HealthLabel => healthLabel;
        public TextMeshProUGUI WeaponLabel => weaponLabel;
        public TextMeshProUGUI CooldownLabel => cooldownLabel;
        public TextMeshProUGUI ArmorLabel => armorLabel;
        public TextMeshProUGUI TargetLabel => targetLabel;

        public static BattlePlayerHudView CreateRuntime()
        {
            var canvasObject = new GameObject("Battle Player HUD", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 104;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var view = canvasObject.AddComponent<BattlePlayerHudView>();
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

        private void Render(BattlePlayerHudSnapshot snapshot)
        {
            if (!snapshot.HasPlayer)
            {
                titleLabel.text = "Player";
                healthLabel.text = "HP -";
                weaponLabel.text = "Weapon -";
                cooldownLabel.text = "Ready -";
                armorLabel.text = "Armor -";
                targetLabel.text = "Target -";
                return;
            }

            titleLabel.text = FormatControlledEntity(snapshot);
            healthLabel.text = $"HP {snapshot.CurrentHealth}/{snapshot.MaxHealth}";
            weaponLabel.text = snapshot.HasWeapon
                ? $"Slot {snapshot.SelectedWeaponSlot + 1}  W{snapshot.WeaponConfigId} {FormatWeapon(snapshot.WeaponType)}"
                : $"Slot {snapshot.SelectedWeaponSlot + 1}  Weapon -";
            cooldownLabel.text = snapshot.IsWeaponReady
                ? $"Ready  Fire {(snapshot.FirePressed ? "on" : "idle")}"
                : $"Cooldown {snapshot.CooldownRemaining:0.0}s/{snapshot.CooldownDuration:0.0}s";
            armorLabel.text = $"Armor B{snapshot.BallisticArmor} E{snapshot.EnergyArmor} X{snapshot.ExplosionArmor}";
            targetLabel.text = snapshot.HasTarget
                ? $"{FormatTarget(snapshot)} HP {snapshot.TargetCurrentHealth}/{snapshot.TargetMaxHealth}"
                : "Target -";
        }

        private void EnsureLayout()
        {
            if (backgroundImage != null &&
                titleLabel != null &&
                healthLabel != null &&
                weaponLabel != null &&
                cooldownLabel != null &&
                armorLabel != null &&
                targetLabel != null)
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

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(12f, 16f);
            rect.sizeDelta = new Vector2(PanelWidth, LabelHeight * RowCount + 14f);

            backgroundImage ??= gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.025f, 0.028f, 0.032f, 0.76f);
            backgroundImage.raycastTarget = false;

            titleLabel ??= CreateLabel("Player Title", 0, 16f, FontStyles.Bold);
            healthLabel ??= CreateLabel("Player Health", 1, 15f, FontStyles.Normal);
            weaponLabel ??= CreateLabel("Player Weapon", 2, 14f, FontStyles.Normal);
            cooldownLabel ??= CreateLabel("Player Cooldown", 3, 14f, FontStyles.Normal);
            armorLabel ??= CreateLabel("Player Armor", 4, 14f, FontStyles.Normal);
            targetLabel ??= CreateLabel("Player Target", 5, 14f, FontStyles.Normal);
        }

        private TextMeshProUGUI CreateLabel(string labelName, int row, float fontSize, FontStyles style)
        {
            var labelObject = new GameObject(labelName, typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(10f, -7f - LabelHeight * row);
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

        private static string FormatControlledEntity(BattlePlayerHudSnapshot snapshot)
        {
            var prefix = snapshot.IsVehicle ? "Vehicle" : "Player";
            return snapshot.ControlledConfigId > 0
                ? $"{prefix} {snapshot.ControlledConfigId}"
                : prefix;
        }

        private static string FormatTarget(BattlePlayerHudSnapshot snapshot)
        {
            var prefix = snapshot.TargetIsVehicle ? "Target vehicle" : "Target";
            return snapshot.TargetConfigId > 0
                ? $"{prefix} {snapshot.TargetConfigId}"
                : prefix;
        }

        private static string FormatWeapon(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Sword:
                    return "Sword";
                case WeaponType.Shield:
                    return "Shield";
                case WeaponType.ArtilleryCannon:
                    return "Artillery";
                case WeaponType.TankCannon:
                    return "Tank cannon";
                default:
                    return "Rifle";
            }
        }
    }
}
