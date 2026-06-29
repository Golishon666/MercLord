using System;
using System.Collections.Generic;
using MercLord.Battle.Combat;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using Scellecs.Morpeh;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class DamageSystem : IBattleRuntimeSystem
    {
        private readonly ConfigDatabase configDatabase;
        private readonly IDamageSystem damageSystem;
        private readonly List<Entity> requestBuffer = new List<Entity>();

        private World world;
        private Filter filter;
        private Stash<DamageRequestComponent> damageRequests;
        private Stash<HealthComponent> healths;
        private Stash<ArmorStatsComponent> armors;
        private Stash<DeadComponent> deadComponents;
        private Stash<BotStateComponent> botStates;

        public DamageSystem(
            ConfigDatabase configDatabase,
            IDamageSystem damageSystem)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
            this.damageSystem = damageSystem ?? throw new ArgumentNullException(nameof(damageSystem));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("DamageSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("DamageSystem cannot initialize on a disposed Morpeh world.");
            }

            if (configDatabase.CombatBalance == null)
            {
                throw new InvalidOperationException("DamageSystem requires CombatBalanceConfig.");
            }

            filter = world.Filter
                .With<DamageRequestComponent>()
                .Build();

            damageRequests = world.GetStash<DamageRequestComponent>();
            healths = world.GetStash<HealthComponent>();
            armors = world.GetStash<ArmorStatsComponent>();
            deadComponents = world.GetStash<DeadComponent>();
            botStates = world.GetStash<BotStateComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            requestBuffer.Clear();
            foreach (var entity in filter)
            {
                requestBuffer.Add(entity);
            }

            for (var requestIndex = 0; requestIndex < requestBuffer.Count; requestIndex++)
            {
                var requestEntity = requestBuffer[requestIndex];
                var request = damageRequests.Get(requestEntity);
                ApplyRequest(request);
                world.RemoveEntity(requestEntity);
            }

            requestBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            requestBuffer.Clear();
            filter = null;
            world = null;
            damageRequests = null;
            healths = null;
            armors = null;
            deadComponents = null;
            botStates = null;
        }

        private void ApplyRequest(DamageRequestComponent request)
        {
            if (request.Amount <= 0)
            {
                throw new InvalidOperationException("Damage request amount must be positive.");
            }

            if (!world.Has(request.Target) || !healths.Has(request.Target))
            {
                return;
            }

            ref var targetHealth = ref healths.Get(request.Target);
            var armorValues = GetArmorValues(request.Target);
            var healthValues = new HealthValues
            {
                Current = targetHealth.Current,
                Max = targetHealth.Max
            };

            var resolution = damageSystem.ApplyDamage(
                new DamageRequest
                {
                    Source = request.Source,
                    Target = request.Target,
                    HitPosition = request.HitPosition,
                    DamageType = request.DamageType,
                    Amount = request.Amount
                },
                armorValues,
                healthValues,
                configDatabase.CombatBalance.DamageFormula);

            targetHealth.Current = resolution.HealthAfterDamage.Current;
            targetHealth.Max = resolution.HealthAfterDamage.Max;

            if (resolution.Killed)
            {
                MarkDead(request.Target);
            }
        }

        private ArmorValues GetArmorValues(Entity target)
        {
            ref var armor = ref armors.Get(target, out var hasArmor);
            if (!hasArmor)
            {
                return default;
            }

            return new ArmorValues
            {
                BallisticProtection = armor.BallisticProtection,
                EnergyProtection = armor.EnergyProtection,
                ExplosionProtection = armor.ExplosionProtection
            };
        }

        private void MarkDead(Entity target)
        {
            deadComponents.Set(target, new DeadComponent());

            ref var botState = ref botStates.Get(target, out var hasBotState);
            if (hasBotState)
            {
                botState.Value = BotStateType.Dead;
            }
        }
    }
}
