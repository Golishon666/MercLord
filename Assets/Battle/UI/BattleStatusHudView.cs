using System;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MercLord.Battle.UI
{
    public sealed class BattleStatusHudView : MonoBehaviour
    {
        private const float PanelWidth = 280f;
        private const float LabelHeight = 24f;

        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private TextMeshProUGUI attackerLabel;
        [SerializeField] private TextMeshProUGUI defenderLabel;
        [SerializeField] private TextMeshProUGUI playerLabel;
        [SerializeField] private TextMeshProUGUI resultLabel;

        private readonly BattleStatusHudPresenter presenter = new BattleStatusHudPresenter();

        public TextMeshProUGUI StatusLabel => statusLabel;
        public TextMeshProUGUI AttackerLabel => attackerLabel;
        public TextMeshProUGUI DefenderLabel => defenderLabel;
        public TextMeshProUGUI PlayerLabel => playerLabel;
        public TextMeshProUGUI ResultLabel => resultLabel;

        public static BattleStatusHudView CreateRuntime()
        {
            var canvasObject = new GameObject("Battle Status HUD", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var view = canvasObject.AddComponent<BattleStatusHudView>();
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

        private void Render(BattleStatusHudSnapshot snapshot)
        {
            var attackerName = GetTeamDisplayName(BattleTeamType.Attacker, snapshot);
            var defenderName = GetTeamDisplayName(BattleTeamType.Defender, snapshot);
            statusLabel.text = snapshot.IsCompleted ? "Battle complete" : "Battle running";
            attackerLabel.text = FormatTeam(attackerName, snapshot.Attacker);
            defenderLabel.text = FormatTeam(defenderName, snapshot.Defender);
            playerLabel.text = snapshot.HasPlayer
                ? $"Player HP {snapshot.PlayerCurrentHealth}/{snapshot.PlayerMaxHealth}"
                : "Player HP -";
            resultLabel.text = snapshot.IsCompleted
                ? $"Result: {FormatOutcome(snapshot.Outcome)}"
                : string.Empty;
        }

        private void EnsureLayout()
        {
            if (statusLabel != null &&
                attackerLabel != null &&
                defenderLabel != null &&
                playerLabel != null &&
                resultLabel != null)
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
            rect.anchoredPosition = new Vector2(12f, -12f);
            rect.sizeDelta = new Vector2(PanelWidth, LabelHeight * 5f);

            statusLabel ??= CreateLabel("Status", 0);
            attackerLabel ??= CreateLabel("Attacker Count", 1);
            defenderLabel ??= CreateLabel("Defender Count", 2);
            playerLabel ??= CreateLabel("Player Health", 3);
            resultLabel ??= CreateLabel("Result", 4);
        }

        private TextMeshProUGUI CreateLabel(string labelName, int row)
        {
            var labelObject = new GameObject(labelName, typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -LabelHeight * row);
            rect.sizeDelta = new Vector2(PanelWidth, LabelHeight);

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = 16f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Left;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static string FormatTeam(string name, BattleTeamHudSnapshot snapshot)
        {
            return $"{name}: {snapshot.Alive}/{snapshot.Total} Lost {snapshot.Lost}";
        }

        private static string GetTeamDisplayName(BattleTeamType team, BattleStatusHudSnapshot snapshot)
        {
            if (!snapshot.HasPlayer)
            {
                return team == BattleTeamType.Attacker ? "Attackers" : "Defenders";
            }

            return team == snapshot.PlayerTeam ? "Allies" : "Enemies";
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
                    return "None";
            }
        }
    }
}
