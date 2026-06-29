using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MercLord.Game.Configs;
using MercLord.Game.Save;
using MercLord.Game.Services;
using MercLord.Global.Cells;
using MercLord.Global.Generation;
using UnityEngine.SceneManagement;

namespace MercLord.Game.StateMachine.States
{
    public sealed class BootstrapState : GameStateBase
    {
        public override GameStateId Id => GameStateId.Bootstrap;
    }

    public sealed class MainMenuState : GameStateBase
    {
        public override GameStateId Id => GameStateId.MainMenu;
    }

    public sealed class GenerateWorldState : GameStateBase
    {
        private readonly ConfigDatabase configDatabase;
        private readonly IWorldGenerator worldGenerator;
        private readonly ISaveService saveService;
        private readonly IGameStateMachine stateMachine;

        public GenerateWorldState(
            ConfigDatabase configDatabase,
            IWorldGenerator worldGenerator,
            ISaveService saveService,
            IGameStateMachine stateMachine)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
            this.worldGenerator = worldGenerator ?? throw new ArgumentNullException(nameof(worldGenerator));
            this.saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            this.stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        }

        public override GameStateId Id => GameStateId.GenerateWorld;

        public override async UniTask EnterAsync(GameStateContext context, CancellationToken cancellationToken)
        {
            var globalConfig = configDatabase.GlobalGeneration
                ?? throw new InvalidOperationException("GlobalGenerationConfig is required to generate a new world.");
            var request = context.GetPayloadOrDefault(new NewGameRequest(globalConfig.Seed));
            var seed = request.Seed ?? globalConfig.Seed;
            var worldModel = worldGenerator.Generate(new WorldGenerationRequest(seed, globalConfig.TargetCellCount));

            ApplyCultureStart(worldModel, request.CultureId);
            saveService.CreateNew(worldModel);

            await stateMachine.ChangeStateAsync(
                GameStateId.LoadGlobal,
                new LoadGlobalRequest(loadScene: true),
                cancellationToken);
        }

        private void ApplyCultureStart(WorldModel worldModel, int cultureId)
        {
            if (cultureId == WorldIds.None)
            {
                return;
            }

            if (!configDatabase.TryGetCulture(cultureId, out var cultureConfig))
            {
                throw new InvalidOperationException($"Culture config id {cultureId} is not registered.");
            }

            if (cultureConfig.StartingCellId < 0 || cultureConfig.StartingCellId >= worldModel.Cells.Length)
            {
                throw new InvalidOperationException($"{cultureConfig.DisplayName} starting cell id must point to a generated world cell.");
            }

            worldModel.Player.CultureId = cultureConfig.Id;
            worldModel.Player.CellId = cultureConfig.StartingCellId;
            worldModel.Player.Credits = cultureConfig.StartingCredits;
        }
    }

    public sealed class LoadGlobalState : GameStateBase
    {
        private readonly ISaveService saveService;
        private readonly ISceneLoader sceneLoader;
        private readonly IGameStateMachine stateMachine;

        public LoadGlobalState(
            ISaveService saveService,
            ISceneLoader sceneLoader,
            IGameStateMachine stateMachine)
        {
            this.saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            this.sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            this.stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        }

        public override GameStateId Id => GameStateId.LoadGlobal;

        public override async UniTask EnterAsync(GameStateContext context, CancellationToken cancellationToken)
        {
            if (saveService.Current == null)
            {
                throw new InvalidOperationException("Cannot load global map without an active save model.");
            }

            var request = context.GetPayloadOrDefault(new LoadGlobalRequest(loadScene: true));
            if (request.LoadScene)
            {
                await sceneLoader.LoadSceneAsync(SceneNames.Global, LoadSceneMode.Single, cancellationToken);
            }

            await stateMachine.ChangeStateAsync(GameStateId.GlobalMap, cancellationToken: cancellationToken);
        }
    }

    public sealed class GlobalMapState : GameStateBase
    {
        public override GameStateId Id => GameStateId.GlobalMap;
    }

    public sealed class EnterBattleState : GameStateBase
    {
        public override GameStateId Id => GameStateId.EnterBattle;
    }

    public sealed class BattleState : GameStateBase
    {
        public override GameStateId Id => GameStateId.Battle;
    }

    public sealed class ExitBattleState : GameStateBase
    {
        public override GameStateId Id => GameStateId.ExitBattle;
    }

    public sealed class SaveLoadState : GameStateBase
    {
        public override GameStateId Id => GameStateId.SaveLoad;
    }
}
