using System;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;

namespace MercLord.Battle.UI
{
    public sealed class BattlePlayerHudPresenter : IDisposable
    {
        private World world;
        private Filter playerFilter;
        private Stash<HealthComponent> healths;
        private Stash<WeaponStatsComponent> weapons;
        private Stash<AttackCooldownComponent> cooldowns;
        private Stash<ArmorStatsComponent> armors;
        private Stash<PlayerInputComponent> playerInputs;
        private Stash<BotComponent> bots;
        private Stash<VehicleComponent> vehicles;
        private Stash<TargetComponent> targets;

        public void Bind(BattleSession session)
        {
            DisposeFilter();

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("BattlePlayerHudPresenter requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattlePlayerHudPresenter cannot bind to a disposed Morpeh world.");
            }

            playerFilter = world.Filter
                .With<PlayerControlledComponent>()
                .With<HealthComponent>()
                .Build();
            healths = world.GetStash<HealthComponent>();
            weapons = world.GetStash<WeaponStatsComponent>();
            cooldowns = world.GetStash<AttackCooldownComponent>();
            armors = world.GetStash<ArmorStatsComponent>();
            playerInputs = world.GetStash<PlayerInputComponent>();
            bots = world.GetStash<BotComponent>();
            vehicles = world.GetStash<VehicleComponent>();
            targets = world.GetStash<TargetComponent>();
        }

        public BattlePlayerHudSnapshot BuildSnapshot()
        {
            if (world == null ||
                world.IsDisposed ||
                playerFilter == null)
            {
                return default;
            }

            foreach (var entity in playerFilter)
            {
                return BuildSnapshot(entity);
            }

            return default;
        }

        public void Dispose()
        {
            DisposeFilter();
            world = null;
            healths = null;
            weapons = null;
            cooldowns = null;
            armors = null;
            playerInputs = null;
            bots = null;
            vehicles = null;
            targets = null;
        }

        private BattlePlayerHudSnapshot BuildSnapshot(Entity entity)
        {
            var health = healths.Get(entity);
            var hasWeapon = weapons.Has(entity);
            var weapon = hasWeapon ? weapons.Get(entity) : default;
            var cooldown = cooldowns.Has(entity) ? cooldowns.Get(entity).Value : 0f;
            var armor = armors.Has(entity) ? armors.Get(entity) : default;
            var input = playerInputs.Has(entity) ? playerInputs.Get(entity) : default;
            var isVehicle = vehicles.Has(entity);
            var controlledConfigId = ResolveControlledConfigId(entity, isVehicle);
            TryResolveTarget(
                entity,
                out var hasTarget,
                out var targetIsVehicle,
                out var targetConfigId,
                out var targetCurrentHealth,
                out var targetMaxHealth);

            return new BattlePlayerHudSnapshot(
                hasPlayer: true,
                isVehicle: isVehicle,
                controlledConfigId: controlledConfigId,
                currentHealth: health.Current,
                maxHealth: health.Max,
                hasWeapon: hasWeapon,
                weaponConfigId: weapon.WeaponConfigId,
                weaponType: weapon.Type,
                selectedWeaponSlot: input.SelectedWeaponSlot,
                cooldownRemaining: cooldown,
                cooldownDuration: hasWeapon ? weapon.Cooldown : 0f,
                ballisticArmor: armor.BallisticProtection,
                energyArmor: armor.EnergyProtection,
                explosionArmor: armor.ExplosionProtection,
                firePressed: input.FirePressed,
                interactPressed: input.InteractPressed,
                hasTarget: hasTarget,
                targetIsVehicle: targetIsVehicle,
                targetConfigId: targetConfigId,
                targetCurrentHealth: targetCurrentHealth,
                targetMaxHealth: targetMaxHealth);
        }

        private int ResolveControlledConfigId(Entity entity, bool isVehicle)
        {
            if (isVehicle)
            {
                return vehicles.Get(entity).VehicleConfigId;
            }

            return bots.Has(entity)
                ? bots.Get(entity).UnitConfigId
                : 0;
        }

        private void TryResolveTarget(
            Entity entity,
            out bool hasTarget,
            out bool targetIsVehicle,
            out int targetConfigId,
            out int targetCurrentHealth,
            out int targetMaxHealth)
        {
            hasTarget = false;
            targetIsVehicle = false;
            targetConfigId = 0;
            targetCurrentHealth = 0;
            targetMaxHealth = 0;

            if (!targets.Has(entity))
            {
                return;
            }

            var target = targets.Get(entity).Target;
            if (!world.Has(target) || !healths.Has(target))
            {
                return;
            }

            hasTarget = true;
            targetIsVehicle = vehicles.Has(target);
            targetConfigId = ResolveControlledConfigId(target, targetIsVehicle);
            var targetHealth = healths.Get(target);
            targetCurrentHealth = targetHealth.Current;
            targetMaxHealth = targetHealth.Max;
        }

        private void DisposeFilter()
        {
            if (world != null && !world.IsDisposed)
            {
                playerFilter?.Dispose();
            }

            playerFilter = null;
        }
    }
}
