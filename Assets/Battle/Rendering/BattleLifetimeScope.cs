using System;
using MercLord.Battle.Input;
using MercLord.Battle.ECS.Systems;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MercLord.Battle.Rendering
{
    public sealed class BattleLifetimeScope : LifetimeScope
    {
        [SerializeField] private BattleSceneRoot sceneRoot;
        [SerializeField] private BattleViewCatalog viewCatalog;
        [SerializeField] private BattleInputSource inputSource;
        [SerializeField] private BattleTilemapView tilemapView;

        public BattleSceneRoot SceneRoot => sceneRoot;
        public BattleViewCatalog ViewCatalog => viewCatalog;
        public BattleInputSource InputSource => inputSource;
        public BattleTilemapView TilemapView => tilemapView;

        protected override void Configure(IContainerBuilder builder)
        {
            if (sceneRoot == null)
            {
                throw new InvalidOperationException("BattleLifetimeScope requires a BattleSceneRoot reference.");
            }

            if (viewCatalog == null)
            {
                throw new InvalidOperationException("BattleLifetimeScope requires a BattleViewCatalog reference.");
            }

            if (inputSource == null)
            {
                throw new InvalidOperationException("BattleLifetimeScope requires a BattleInputSource reference.");
            }

            if (tilemapView == null)
            {
                throw new InvalidOperationException("BattleLifetimeScope requires a BattleTilemapView reference.");
            }

            builder.RegisterInstance(viewCatalog);
            builder.RegisterInstance(inputSource).As<IBattleInputSource>();
            builder.RegisterInstance(tilemapView);
            builder.Register<BattleTilemapRenderer>(Lifetime.Scoped).As<IBattleTilemapRenderer>();
            builder.Register<BattleViewRegistry>(Lifetime.Scoped);
            builder.Register<BattleViewFactory>(Lifetime.Scoped).As<IBattleViewFactory>();
            builder.Register<BattleViewSpawner>(Lifetime.Scoped).As<IBattleViewSpawner>();
            builder.Register<SpatialHashSystem>(Lifetime.Scoped);
            builder.Register<SpatialHashDebugViewSystem>(Lifetime.Scoped);
            builder.Register<BattleLodSystem>(Lifetime.Scoped);
            builder.Register<PlayerInputSystem>(Lifetime.Scoped);
            builder.Register<PlayerSquadCommandSystem>(Lifetime.Scoped);
            builder.Register<VehicleInputSystem>(Lifetime.Scoped);
            builder.Register<VehicleExitSystem>(Lifetime.Scoped);
            builder.Register<VehicleEnterSystem>(Lifetime.Scoped);
            builder.Register<VehicleAISystem>(Lifetime.Scoped);
            builder.Register<TargetSearchSystem>(Lifetime.Scoped);
            builder.Register<DecisionSystem>(Lifetime.Scoped);
            builder.Register<SquadMovementSystem>(Lifetime.Scoped);
            builder.Register<LocalSeparationSystem>(Lifetime.Scoped);
            builder.Register<BattleAudioCuePlayer>(Lifetime.Scoped).As<IBattleAudioCuePlayer>();
            builder.Register<WeaponSystem>(Lifetime.Scoped);
            builder.Register<BattleAudioCueSystem>(Lifetime.Scoped);
            builder.Register<ProjectileSystem>(Lifetime.Scoped);
            builder.Register<ParabolicProjectileSystem>(Lifetime.Scoped);
            builder.Register<ProjectileViewSystem>(Lifetime.Scoped);
            builder.Register<ArtilleryWarningSystem>(Lifetime.Scoped);
            builder.Register<HitscanTraceSystem>(Lifetime.Scoped);
            builder.Register<MovementSystem>(Lifetime.Scoped);
            builder.Register<MercLord.Battle.ECS.Systems.DamageSystem>(Lifetime.Scoped);
            builder.Register<BattleObjectiveSystem>(Lifetime.Scoped);
            builder.Register<SquadMoraleSystem>(Lifetime.Scoped);
            builder.Register<MercLord.Battle.Generation.BattleResultBuilder>(Lifetime.Scoped)
                .As<MercLord.Battle.Generation.IBattleResultBuilder>();
            builder.Register<BattleVictorySystem>(Lifetime.Scoped);
            builder.Register<ArtilleryWarningViewSystem>(Lifetime.Scoped);
            builder.Register<HitscanTraceViewSystem>(Lifetime.Scoped);
            builder.Register<ViewSyncSystem>(Lifetime.Scoped);
            builder.Register<BattleCameraFollowSystem>(Lifetime.Scoped);
            builder.Register<BattleCameraShakeSystem>(Lifetime.Scoped);
            builder.Register<BattleSystemRunner>(Lifetime.Scoped).As<IBattleSystemRunner>();
            builder.Register<BattleCompletionExitHandler>(Lifetime.Scoped).As<IBattleCompletionExitHandler>();
            builder.RegisterComponent(sceneRoot);
        }
    }
}
