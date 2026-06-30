using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.Input;
using MercLord.Game.Configs;
using Scellecs.Morpeh;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class VehicleInputSystem : IBattleRuntimeSystem
    {
        private readonly IBattleInputSource inputSource;
        private readonly ConfigDatabase configDatabase;
        private readonly SpatialHashSystem spatialHashSystem;
        private readonly List<Entity> vehicleBuffer = new List<Entity>();
        private readonly List<Entity> candidateBuffer = new List<Entity>();

        private World world;
        private Filter filter;
        private Stash<PlayerInputComponent> playerInputs;
        private Stash<VehicleComponent> vehicles;
        private Stash<PositionComponent> positions;
        private Stash<VelocityComponent> velocities;
        private Stash<TeamComponent> teams;
        private Stash<WeaponStatsComponent> weapons;
        private Stash<AttackCooldownComponent> cooldowns;
        private Stash<AttackRequestComponent> attackRequests;
        private Stash<TargetComponent> targets;
        private Stash<HealthComponent> healths;

        public VehicleInputSystem(
            IBattleInputSource inputSource,
            ConfigDatabase configDatabase,
            SpatialHashSystem spatialHashSystem)
        {
            this.inputSource = inputSource ?? throw new ArgumentNullException(nameof(inputSource));
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
            this.spatialHashSystem = spatialHashSystem ?? throw new ArgumentNullException(nameof(spatialHashSystem));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("VehicleInputSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("VehicleInputSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<PlayerControlledComponent>()
                .With<PlayerInputComponent>()
                .With<VehicleComponent>()
                .With<PositionComponent>()
                .With<VelocityComponent>()
                .With<TeamComponent>()
                .With<WeaponStatsComponent>()
                .With<AttackCooldownComponent>()
                .Without<DeadComponent>()
                .Build();

            playerInputs = world.GetStash<PlayerInputComponent>();
            vehicles = world.GetStash<VehicleComponent>();
            positions = world.GetStash<PositionComponent>();
            velocities = world.GetStash<VelocityComponent>();
            teams = world.GetStash<TeamComponent>();
            weapons = world.GetStash<WeaponStatsComponent>();
            cooldowns = world.GetStash<AttackCooldownComponent>();
            attackRequests = world.GetStash<AttackRequestComponent>();
            targets = world.GetStash<TargetComponent>();
            healths = world.GetStash<HealthComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            var snapshot = inputSource.Snapshot;
            vehicleBuffer.Clear();
            foreach (var entity in filter)
            {
                vehicleBuffer.Add(entity);
            }

            for (var vehicleIndex = 0; vehicleIndex < vehicleBuffer.Count; vehicleIndex++)
            {
                ApplyInput(vehicleBuffer[vehicleIndex], snapshot);
            }

            vehicleBuffer.Clear();
            candidateBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            vehicleBuffer.Clear();
            candidateBuffer.Clear();
            filter = null;
            world = null;
            playerInputs = null;
            vehicles = null;
            positions = null;
            velocities = null;
            teams = null;
            weapons = null;
            cooldowns = null;
            attackRequests = null;
            targets = null;
            healths = null;
        }

        private void ApplyInput(Entity vehicleEntity, BattleInputSnapshot snapshot)
        {
            ref var vehicle = ref vehicles.Get(vehicleEntity);
            if (vehicle.State != VehicleStateType.PlayerControlled)
            {
                velocities.Get(vehicleEntity).Value = Unity.Mathematics.float2.zero;
                return;
            }

            playerInputs.Get(vehicleEntity) = new PlayerInputComponent
            {
                MoveDirection = snapshot.MoveDirection,
                AimDirection = snapshot.AimDirection,
                FirePressed = snapshot.FirePressed,
                InteractPressed = snapshot.InteractPressed,
                SelectedWeaponSlot = snapshot.SelectedWeaponSlot
            };

            velocities.Get(vehicleEntity).Value = snapshot.MoveDirection;

            var weapon = weapons.Get(vehicleEntity);
            if (!snapshot.FirePressed ||
                cooldowns.Get(vehicleEntity).Value > 0f ||
                !BattleAimTargeting.TryFindAimedTarget(
                    world,
                    spatialHashSystem,
                    configDatabase,
                    positions,
                    teams,
                    healths,
                    vehicleEntity,
                    snapshot.AimDirection,
                    weapon.Range,
                    candidateBuffer,
                    out var target))
            {
                return;
            }

            targets.Set(vehicleEntity, new TargetComponent { Target = target });
            var requestEntity = world.CreateEntity();
            attackRequests.Set(requestEntity, new AttackRequestComponent
            {
                Source = vehicleEntity,
                Target = target,
                WeaponConfigId = weapon.WeaponConfigId
            });
        }
    }
}
