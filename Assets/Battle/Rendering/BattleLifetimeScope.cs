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

        public BattleSceneRoot SceneRoot => sceneRoot;
        public BattleViewCatalog ViewCatalog => viewCatalog;
        public BattleInputSource InputSource => inputSource;

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

            builder.RegisterInstance(viewCatalog);
            builder.RegisterInstance(inputSource).As<IBattleInputSource>();
            builder.Register<BattleViewRegistry>(Lifetime.Scoped);
            builder.Register<BattleViewFactory>(Lifetime.Scoped).As<IBattleViewFactory>();
            builder.Register<BattleViewSpawner>(Lifetime.Scoped).As<IBattleViewSpawner>();
            builder.Register<SpatialHashSystem>(Lifetime.Scoped);
            builder.Register<PlayerInputSystem>(Lifetime.Scoped);
            builder.Register<VehicleInputSystem>(Lifetime.Scoped);
            builder.Register<VehicleExitSystem>(Lifetime.Scoped);
            builder.Register<VehicleEnterSystem>(Lifetime.Scoped);
            builder.Register<TargetSearchSystem>(Lifetime.Scoped);
            builder.Register<DecisionSystem>(Lifetime.Scoped);
            builder.Register<WeaponSystem>(Lifetime.Scoped);
            builder.Register<ProjectileSystem>(Lifetime.Scoped);
            builder.Register<ParabolicProjectileSystem>(Lifetime.Scoped);
            builder.Register<MovementSystem>(Lifetime.Scoped);
            builder.Register<MercLord.Battle.ECS.Systems.DamageSystem>(Lifetime.Scoped);
            builder.Register<ViewSyncSystem>(Lifetime.Scoped);
            builder.Register<BattleSystemRunner>(Lifetime.Scoped).As<IBattleSystemRunner>();
            builder.RegisterComponent(sceneRoot);
        }
    }
}
