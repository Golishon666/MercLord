using System;
using MercLord.Game.Configs;
using Cysharp.Threading.Tasks;
using MercLord.Game.StateMachine;
using MercLord.Global.Cells;
using MercLord.Global.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using VContainer.Unity;

namespace MercLord.Global.Rendering
{
    public sealed class GlobalMapCellInfoController : ITickable
    {
        private readonly ProceduralGlobalMapRenderer mapRenderer;
        private readonly GlobalMapDebugController debugController;
        private readonly ConfigDatabase configDatabase;
        private readonly GlobalMapCellTooltipView tooltipView;
        private readonly Camera inputCamera;
        private readonly IGlobalBattleStarter battleStarter;

        private GlobalBattleEncounterPromptView encounterPrompt;
        private bool promptStartInProgress;
        private int selectedCellId = WorldIds.None;

        public ProceduralGlobalMapRenderer MapRenderer => mapRenderer;
        public GlobalMapDebugController DebugController => debugController;
        public GlobalMapCellTooltipView TooltipView => tooltipView;
        public Camera InputCamera => inputCamera;

        public GlobalMapCellInfoController(
            GlobalSceneRoot sceneRoot,
            ConfigDatabase configDatabase,
            IObjectResolver resolver)
        {
            mapRenderer = sceneRoot.ProceduralMapRenderer;
            debugController = sceneRoot.DebugController;
            this.configDatabase = configDatabase;
            tooltipView = sceneRoot.TooltipView;
            inputCamera = sceneRoot.InputCamera;
            battleStarter = resolver.TryResolve<IGlobalBattleStarter>(out var resolvedBattleStarter)
                ? resolvedBattleStarter
                : null;
            RefreshDate();
        }

        public void Tick()
        {
            RefreshDate();

            if (!Application.isPlaying || mapRenderer == null || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (debugController != null && debugController.IsPointerOverDebugButton(Input.mousePosition))
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (inputCamera == null)
            {
                return;
            }

            var ray = inputCamera.ScreenPointToRay(Input.mousePosition);
            if (!mapRenderer.TryPickCell(ray, out var cellId) ||
                !mapRenderer.TryGetCell(cellId, out var cell))
            {
                ClearSelection();
                return;
            }

            selectedCellId = cellId;
            mapRenderer.SelectCell(cellId);
            tooltipView?.Show(cell, GetFactionName(cell.OwnerFactionId), Input.mousePosition);
            ShowEncounterPromptIfAvailable(cellId);
        }

        public void ClearSelection()
        {
            selectedCellId = WorldIds.None;
            mapRenderer?.ClearSelection();
            tooltipView?.Hide();
            encounterPrompt?.Hide();
        }

        private void RefreshDate()
        {
            var world = mapRenderer?.CurrentWorld;
            if (world == null)
            {
                return;
            }

            tooltipView?.SetDate(world.CurrentDay);
        }

        private string GetFactionName(int factionId)
        {
            if (factionId == WorldIds.None)
            {
                return "\u043d\u0435\u0442";
            }

            return configDatabase != null && configDatabase.TryGetFaction(factionId, out var faction)
                ? faction.DisplayName
                : $"Faction {factionId}";
        }

        private void ShowEncounterPromptIfAvailable(int cellId)
        {
            if (battleStarter == null ||
                !battleStarter.TryGetPlayerBattleEncounter(cellId, out var encounter))
            {
                encounterPrompt?.Hide();
                return;
            }

            EnsureEncounterPrompt().Show(
                encounter,
                GetFactionName(encounter.OpponentFactionId),
                () => StartBattleFromPrompt(encounter.OpponentArmyId),
                () => encounterPrompt?.Hide());
        }

        private GlobalBattleEncounterPromptView EnsureEncounterPrompt()
        {
            encounterPrompt ??= GlobalBattleEncounterPromptView.CreateRuntime();
            return encounterPrompt;
        }

        private void StartBattleFromPrompt(int opponentArmyId)
        {
            if (promptStartInProgress)
            {
                return;
            }

            StartBattleFromPromptAsync(opponentArmyId).Forget();
        }

        private async UniTaskVoid StartBattleFromPromptAsync(int opponentArmyId)
        {
            promptStartInProgress = true;
            encounterPrompt?.SetInteractable(false);
            try
            {
                var started = battleStarter != null &&
                              await battleStarter.TryStartPlayerBattleAsync(opponentArmyId);
                if (!started)
                {
                    promptStartInProgress = false;
                    encounterPrompt?.SetInteractable(true);
                    encounterPrompt?.Hide();
                }
            }
            catch (Exception exception)
            {
                promptStartInProgress = false;
                encounterPrompt?.SetInteractable(true);
                Debug.LogException(exception);
            }
        }
    }
}
