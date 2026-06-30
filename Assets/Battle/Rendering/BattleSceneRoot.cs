using System;
using Cysharp.Threading.Tasks;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using MercLord.Battle.UI;
using UnityEngine;
using VContainer;

namespace MercLord.Battle.Rendering
{
    public sealed class BattleSceneRoot : MonoBehaviour
    {
        [SerializeField] private Transform unitViewRoot;
        [SerializeField] private BattleStatusHudView statusHud;
        [SerializeField] private BattleSquadHudView squadHud;
        [SerializeField] private BattleCommandHudView commandHud;
        [SerializeField] private BattleMinimapHudView minimapHud;
        [SerializeField] private BattlePlayerHudView playerHud;
        [SerializeField] private BattleResultHudView resultHud;

        private IBattleSessionService battleSessionService;
        private IBattlePlayerSpawner battlePlayerSpawner;
        private IBattleVehicleSpawner battleVehicleSpawner;
        private IBattleViewSpawner battleViewSpawner;
        private IBattleTilemapRenderer battleTilemapRenderer;
        private IBattleSystemRunner battleSystemRunner;
        private IBattleCompletionExitHandler battleCompletionExitHandler;
        private bool completionPresented;
        private bool exitTransitionStarted;

        public Transform UnitViewRoot => unitViewRoot;
        public BattleStatusHudView StatusHud => statusHud;
        public BattleSquadHudView SquadHud => squadHud;
        public BattleCommandHudView CommandHud => commandHud;
        public BattleMinimapHudView MinimapHud => minimapHud;
        public BattlePlayerHudView PlayerHud => playerHud;
        public BattleResultHudView ResultHud => resultHud;

        [Inject]
        public void Construct(
            IBattleSessionService battleSessionService,
            IBattlePlayerSpawner battlePlayerSpawner,
            IBattleVehicleSpawner battleVehicleSpawner,
            IBattleViewSpawner battleViewSpawner,
            IBattleTilemapRenderer battleTilemapRenderer,
            IBattleSystemRunner battleSystemRunner,
            IBattleCompletionExitHandler battleCompletionExitHandler)
        {
            this.battleSessionService = battleSessionService ?? throw new ArgumentNullException(nameof(battleSessionService));
            this.battlePlayerSpawner = battlePlayerSpawner ?? throw new ArgumentNullException(nameof(battlePlayerSpawner));
            this.battleVehicleSpawner = battleVehicleSpawner ?? throw new ArgumentNullException(nameof(battleVehicleSpawner));
            this.battleViewSpawner = battleViewSpawner ?? throw new ArgumentNullException(nameof(battleViewSpawner));
            this.battleTilemapRenderer = battleTilemapRenderer ?? throw new ArgumentNullException(nameof(battleTilemapRenderer));
            this.battleSystemRunner = battleSystemRunner ?? throw new ArgumentNullException(nameof(battleSystemRunner));
            this.battleCompletionExitHandler = battleCompletionExitHandler ?? throw new ArgumentNullException(nameof(battleCompletionExitHandler));
        }

        private void Start()
        {
            if (unitViewRoot == null)
            {
                throw new InvalidOperationException("BattleSceneRoot requires a unit view root.");
            }

            var session = battleSessionService?.Current
                ?? throw new InvalidOperationException("BattleSceneRoot requires an active battle session.");
            battleTilemapRenderer.Render(session);
            battlePlayerSpawner.SpawnPlayer(session);
            battleVehicleSpawner.SpawnVehicles(session);
            battleViewSpawner.SpawnMissingViews(session, unitViewRoot);
            battleSystemRunner.Start(session);
            statusHud = statusHud != null ? statusHud : BattleStatusHudView.CreateRuntime();
            statusHud.Bind(session);
            squadHud = squadHud != null ? squadHud : BattleSquadHudView.CreateRuntime();
            squadHud.Bind(session);
            commandHud = commandHud != null ? commandHud : BattleCommandHudView.CreateRuntime();
            commandHud.Bind(session);
            minimapHud = minimapHud != null ? minimapHud : BattleMinimapHudView.CreateRuntime();
            minimapHud.Bind(session);
            playerHud = playerHud != null ? playerHud : BattlePlayerHudView.CreateRuntime();
            playerHud.Bind(session);
        }

        private void Update()
        {
            if (exitTransitionStarted)
            {
                return;
            }

            battleSystemRunner?.Tick(Time.deltaTime);
            statusHud?.Refresh();
            squadHud?.Refresh();
            commandHud?.Refresh();
            minimapHud?.Refresh();
            playerHud?.Refresh();
            TryPresentCompletionIfBattleCompleted();
        }

        private void OnDestroy()
        {
            battleSystemRunner?.Stop();
            statusHud?.Clear();
            squadHud?.Clear();
            commandHud?.Clear();
            minimapHud?.Clear();
            playerHud?.Clear();
            resultHud?.Hide();
            battleViewSpawner?.ReleaseAll();
            battleTilemapRenderer?.Clear();
        }

        private void TryPresentCompletionIfBattleCompleted()
        {
            var session = battleSessionService?.Current;
            if (completionPresented ||
                session == null ||
                !session.Completion.IsCompleted)
            {
                return;
            }

            completionPresented = true;
            battleSystemRunner?.Stop();
            statusHud?.Refresh();
            resultHud = resultHud != null ? resultHud : BattleResultHudView.CreateRuntime();
            resultHud.Show(session.Completion.Result, () => RequestExitFromResult(session));
        }

        private void RequestExitFromResult(BattleSession session)
        {
            if (exitTransitionStarted)
            {
                return;
            }

            exitTransitionStarted = true;
            resultHud?.SetInteractable(false);
            RequestExitAsync(session).Forget();
        }

        private async UniTaskVoid RequestExitAsync(BattleSession session)
        {
            try
            {
                await battleCompletionExitHandler.TryRequestExitAsync(session);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
