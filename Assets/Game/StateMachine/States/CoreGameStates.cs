using System;
using System.Threading;
using MercLord.Battle.Generation;
using Cysharp.Threading.Tasks;
using MercLord.Game.Configs;
using MercLord.Game.Save;
using MercLord.Game.Services;
using MercLord.Global.Cells;
using MercLord.Global.Generation;
using MercLord.Player.Inventory;
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

            var saveModel = saveService.CreateNew(worldModel);
            ApplyCultureStart(saveModel, request.CultureId);

            await stateMachine.ChangeStateAsync(
                GameStateId.LoadGlobal,
                new LoadGlobalRequest(loadScene: true),
                cancellationToken);
        }

        private void ApplyCultureStart(SaveModel saveModel, int cultureId)
        {
            if (cultureId == WorldIds.None)
            {
                return;
            }

            if (saveModel == null)
            {
                throw new ArgumentNullException(nameof(saveModel));
            }

            var worldModel = saveModel.World
                ?? throw new InvalidOperationException("Cannot apply culture start without a world model.");

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

            saveModel.Equipment ??= new MercLord.Player.Equipment.PlayerEquipment();
            saveModel.Equipment.WeaponSlot1 = CreateEquippedItem(FindWeaponItem(cultureConfig.StartingWeapon).Id);
            saveModel.Equipment.BodyArmor = CreateEquippedItem(FindArmorItem(cultureConfig.StartingArmor).Id);
        }

        private ItemConfig FindWeaponItem(WeaponConfig weaponConfig)
        {
            if (weaponConfig == null)
            {
                throw new InvalidOperationException("Culture starting weapon is missing.");
            }

            foreach (var item in configDatabase.Items)
            {
                if (item != null &&
                    item.Category == ItemCategory.Weapon &&
                    item.Weapon != null &&
                    item.Weapon.Id == weaponConfig.Id)
                {
                    return item;
                }
            }

            throw new InvalidOperationException($"No ItemConfig is mapped to starting WeaponConfig id {weaponConfig.Id}.");
        }

        private ItemConfig FindArmorItem(ArmorConfig armorConfig)
        {
            if (armorConfig == null)
            {
                throw new InvalidOperationException("Culture starting armor is missing.");
            }

            foreach (var item in configDatabase.Items)
            {
                if (item != null &&
                    item.Category == ItemCategory.Armor &&
                    item.Armor != null &&
                    item.Armor.Id == armorConfig.Id)
                {
                    return item;
                }
            }

            throw new InvalidOperationException($"No ItemConfig is mapped to starting ArmorConfig id {armorConfig.Id}.");
        }

        private static ItemInstance CreateEquippedItem(int itemConfigId)
        {
            return new ItemInstance
            {
                ConfigId = itemConfigId,
                Amount = 1,
                Durability = ItemInstance.DurabilityNotTracked
            };
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
        private readonly ISaveService saveService;
        private readonly ISceneLoader sceneLoader;
        private readonly IBattleGenerationRequestFactory requestFactory;
        private readonly IBattlePipeline battlePipeline;
        private readonly IBattleSessionService battleSessionService;
        private readonly IGameStateMachine stateMachine;

        public EnterBattleState(
            ISaveService saveService,
            ISceneLoader sceneLoader,
            IBattleGenerationRequestFactory requestFactory,
            IBattlePipeline battlePipeline,
            IBattleSessionService battleSessionService,
            IGameStateMachine stateMachine)
        {
            this.saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            this.sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            this.requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));
            this.battlePipeline = battlePipeline ?? throw new ArgumentNullException(nameof(battlePipeline));
            this.battleSessionService = battleSessionService ?? throw new ArgumentNullException(nameof(battleSessionService));
            this.stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        }

        public override GameStateId Id => GameStateId.EnterBattle;

        public override async UniTask EnterAsync(GameStateContext context, CancellationToken cancellationToken)
        {
            if (!context.TryGetPayload<EnterBattleRequest>(out var enterRequest))
            {
                throw new InvalidOperationException("EnterBattleState requires EnterBattleRequest payload.");
            }

            var saveModel = saveService.Current
                ?? throw new InvalidOperationException("Cannot enter battle without an active save model.");
            var worldModel = saveModel.World
                ?? throw new InvalidOperationException("Cannot enter battle without an active world model.");
            var sourceCell = FindCell(worldModel, enterRequest.SourceCellId);
            var seed = enterRequest.Seed ?? worldModel.Seed;
            var nearSettlement = enterRequest.NearSettlement || sourceCell.SettlementId != WorldIds.None;

            var generationRequest = requestFactory.Create(sourceCell, seed, nearSettlement);
            BattleSession session = null;
            try
            {
                session = await battlePipeline.StartBattleAsync(
                    generationRequest,
                    enterRequest.Attacker,
                    enterRequest.Defender,
                    cancellationToken);

                battleSessionService.SetCurrent(session);

                if (enterRequest.LoadScene)
                {
                    await sceneLoader.LoadSceneAsync(SceneNames.Battle, LoadSceneMode.Single, cancellationToken);
                }
            }
            catch
            {
                if (session != null)
                {
                    battlePipeline.StopBattle(session);
                    battleSessionService.Clear();
                }

                throw;
            }

            await stateMachine.ChangeStateAsync(
                GameStateId.Battle,
                new BattleStateRequest(session),
                cancellationToken);
        }

        private static WorldCell FindCell(WorldModel worldModel, int sourceCellId)
        {
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (cells[cellIndex].Id == sourceCellId)
                {
                    return cells[cellIndex];
                }
            }

            throw new InvalidOperationException($"Cannot enter battle from unknown world cell id {sourceCellId}.");
        }
    }

    public sealed class BattleState : GameStateBase
    {
        private readonly IBattleSessionService battleSessionService;

        public BattleState(IBattleSessionService battleSessionService)
        {
            this.battleSessionService = battleSessionService ?? throw new ArgumentNullException(nameof(battleSessionService));
        }

        public override GameStateId Id => GameStateId.Battle;

        public override UniTask EnterAsync(GameStateContext context, CancellationToken cancellationToken)
        {
            var session = context.TryGetPayload<BattleStateRequest>(out var request)
                ? request.Session
                : battleSessionService.Current;

            if (session == null)
            {
                throw new InvalidOperationException("BattleState requires an active BattleSession.");
            }

            battleSessionService.SetCurrent(session);
            return UniTask.CompletedTask;
        }
    }

    public sealed class ExitBattleState : GameStateBase
    {
        private readonly ISaveService saveService;
        private readonly ISceneLoader sceneLoader;
        private readonly IBattlePipeline battlePipeline;
        private readonly IBattleSessionService battleSessionService;
        private readonly IBattleResultApplier resultApplier;
        private readonly IGameStateMachine stateMachine;

        public ExitBattleState(
            ISaveService saveService,
            ISceneLoader sceneLoader,
            IBattlePipeline battlePipeline,
            IBattleSessionService battleSessionService,
            IBattleResultApplier resultApplier,
            IGameStateMachine stateMachine)
        {
            this.saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            this.sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            this.battlePipeline = battlePipeline ?? throw new ArgumentNullException(nameof(battlePipeline));
            this.battleSessionService = battleSessionService ?? throw new ArgumentNullException(nameof(battleSessionService));
            this.resultApplier = resultApplier ?? throw new ArgumentNullException(nameof(resultApplier));
            this.stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        }

        public override GameStateId Id => GameStateId.ExitBattle;

        public override async UniTask EnterAsync(GameStateContext context, CancellationToken cancellationToken)
        {
            if (!context.TryGetPayload<ExitBattleRequest>(out var exitRequest))
            {
                throw new InvalidOperationException("ExitBattleState requires ExitBattleRequest payload.");
            }

            if (exitRequest.Result == null)
            {
                throw new InvalidOperationException("ExitBattleState requires a BattleResult.");
            }

            var session = battleSessionService.ConsumeCurrent();
            try
            {
                battlePipeline.StopBattle(session);
            }
            finally
            {
                battleSessionService.Clear();
            }

            var saveModel = saveService.Current
                ?? throw new InvalidOperationException("Cannot apply battle result without an active save model.");
            resultApplier.Apply(saveModel, exitRequest.Result);

            if (exitRequest.LoadGlobalScene)
            {
                await sceneLoader.LoadSceneAsync(SceneNames.Global, LoadSceneMode.Single, cancellationToken);
            }

            await stateMachine.ChangeStateAsync(GameStateId.GlobalMap, cancellationToken: cancellationToken);
        }
    }

    public sealed class SaveLoadState : GameStateBase
    {
        public override GameStateId Id => GameStateId.SaveLoad;
    }
}
