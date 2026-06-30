using System;
using MercLord.Battle.Combat;
using MercLord.Battle.Generation;
using MercLord.Economy.Credits;
using MercLord.Economy.Trading;
using MercLord.Game.Configs;
using MercLord.Game.Save;
using MercLord.Game.Services;
using MercLord.Game.StateMachine;
using MercLord.Game.StateMachine.States;
using MercLord.Global.Cells;
using MercLord.Global.Generation;
using MercLord.Global.Rendering;
using MercLord.Global.Time;
using MercLord.Infrastructure.Pooling;
using MercLord.Player.Inventory;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MercLord.Bootstrap
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private ConfigDatabase configDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            if (configDatabase == null)
            {
                throw new InvalidOperationException("ConfigDatabase must be assigned on GameLifetimeScope.");
            }

            builder.RegisterInstance(configDatabase);
            builder.Register<GameStateMachine>(Lifetime.Singleton).AsSelf().As<IGameStateMachine>();
            builder.Register<UnitySceneLoader>(Lifetime.Singleton).As<ISceneLoader>();
            builder.Register<SaveService>(Lifetime.Singleton).As<ISaveService>();
            builder.Register<CreditsService>(Lifetime.Singleton);
            builder.Register<TradingService>(Lifetime.Singleton).As<ITradingService>();
            builder.Register<PlayerInventoryService>(Lifetime.Singleton).As<IInventoryService>();
            builder.Register<GlobalTimeService>(Lifetime.Singleton);
            builder.Register<InfluenceService>(Lifetime.Singleton).As<IInfluenceService>();
            builder.Register<ConfigDrivenWorldGenerator>(Lifetime.Singleton).As<IWorldGenerator>();
            builder.Register<ConfiguredGridWorldCellLayout>(Lifetime.Singleton).As<IWorldCellLayout>();
            builder.Register<GlobalMapPresenter>(Lifetime.Singleton).As<IGlobalMapPresenter>();

            builder.Register<BattleGenerationRequestFactory>(Lifetime.Singleton).As<IBattleGenerationRequestFactory>();
            builder.Register<ConfigDrivenBattleMapGenerator>(Lifetime.Singleton).As<IBattleMapGenerator>();
            builder.Register<BattleEntityFactory>(Lifetime.Singleton).As<IBattleEntityFactory>();
            builder.Register<ConfigDrivenBattleWorldFactory>(Lifetime.Singleton).As<IBattleWorldFactory>();
            builder.Register<BattlePlayerSpawner>(Lifetime.Singleton).As<IBattlePlayerSpawner>();
            builder.Register<BattleVehicleSpawner>(Lifetime.Singleton).As<IBattleVehicleSpawner>();
            builder.Register<BattleSessionService>(Lifetime.Singleton).As<IBattleSessionService>();
            builder.Register<BattlePipeline>(Lifetime.Singleton).As<IBattlePipeline>();
            builder.Register<BattleResultApplier>(Lifetime.Singleton).As<IBattleResultApplier>();
            builder.Register<DamageSystem>(Lifetime.Singleton).As<IDamageSystem>();
            builder.Register<PrefabFactory>(Lifetime.Singleton).As<IPrefabFactory>();

            builder.Register<BootstrapState>(Lifetime.Singleton);
            builder.Register<MainMenuState>(Lifetime.Singleton);
            builder.Register<GenerateWorldState>(Lifetime.Singleton);
            builder.Register<LoadGlobalState>(Lifetime.Singleton);
            builder.Register<GlobalMapState>(Lifetime.Singleton);
            builder.Register<EnterBattleState>(Lifetime.Singleton);
            builder.Register<BattleState>(Lifetime.Singleton);
            builder.Register<ExitBattleState>(Lifetime.Singleton);
            builder.Register<SaveLoadState>(Lifetime.Singleton);

            builder.RegisterEntryPoint<GameBootstrapper>();
        }
    }
}
