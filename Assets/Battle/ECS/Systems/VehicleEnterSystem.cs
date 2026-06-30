using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class VehicleEnterSystem : IBattleRuntimeSystem
    {
        private readonly ConfigDatabase configDatabase;
        private readonly List<Entity> playerBuffer = new List<Entity>();
        private readonly List<Entity> vehicleBuffer = new List<Entity>();

        private World world;
        private Filter playerFilter;
        private Filter vehicleFilter;
        private Stash<PlayerInputComponent> playerInputs;
        private Stash<PlayerControlledComponent> playerControlled;
        private Stash<DriverComponent> drivers;
        private Stash<VehicleComponent> vehicles;
        private Stash<PositionComponent> positions;
        private Stash<VelocityComponent> velocities;

        public VehicleEnterSystem(ConfigDatabase configDatabase)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("VehicleEnterSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("VehicleEnterSystem cannot initialize on a disposed Morpeh world.");
            }

            playerFilter = world.Filter
                .With<PlayerControlledComponent>()
                .With<PlayerInputComponent>()
                .With<PositionComponent>()
                .With<VelocityComponent>()
                .Without<DriverComponent>()
                .Without<VehicleComponent>()
                .Without<DeadComponent>()
                .Build();

            vehicleFilter = world.Filter
                .With<VehicleComponent>()
                .With<PositionComponent>()
                .With<VelocityComponent>()
                .Without<DeadComponent>()
                .Build();

            playerInputs = world.GetStash<PlayerInputComponent>();
            playerControlled = world.GetStash<PlayerControlledComponent>();
            drivers = world.GetStash<DriverComponent>();
            vehicles = world.GetStash<VehicleComponent>();
            positions = world.GetStash<PositionComponent>();
            velocities = world.GetStash<VelocityComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || playerFilter == null || vehicleFilter == null)
            {
                return;
            }

            playerBuffer.Clear();
            foreach (var player in playerFilter)
            {
                playerBuffer.Add(player);
            }

            vehicleBuffer.Clear();
            foreach (var vehicle in vehicleFilter)
            {
                vehicleBuffer.Add(vehicle);
            }

            for (var playerIndex = 0; playerIndex < playerBuffer.Count; playerIndex++)
            {
                TryEnterVehicle(playerBuffer[playerIndex]);
            }

            playerBuffer.Clear();
            vehicleBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed)
            {
                playerFilter?.Dispose();
                vehicleFilter?.Dispose();
            }

            playerBuffer.Clear();
            vehicleBuffer.Clear();
            playerFilter = null;
            vehicleFilter = null;
            world = null;
            playerInputs = null;
            playerControlled = null;
            drivers = null;
            vehicles = null;
            positions = null;
            velocities = null;
        }

        private void TryEnterVehicle(Entity player)
        {
            var input = playerInputs.Get(player);
            if (!input.InteractPressed)
            {
                return;
            }

            if (!TryFindNearestEnterableVehicle(player, out var vehicleEntity))
            {
                return;
            }

            ref var vehicle = ref vehicles.Get(vehicleEntity);
            vehicle.State = VehicleStateType.PlayerControlled;
            vehicle.Driver = player;

            drivers.Set(player, new DriverComponent
            {
                ControlledVehicle = vehicleEntity
            });

            playerControlled.Remove(player);
            velocities.Get(player).Value = float2.zero;
            velocities.Get(vehicleEntity).Value = input.MoveDirection;

            playerControlled.Set(vehicleEntity, new PlayerControlledComponent());
            playerInputs.Set(vehicleEntity, input);
        }

        private bool TryFindNearestEnterableVehicle(Entity player, out Entity vehicleEntity)
        {
            vehicleEntity = default;
            var playerPosition = positions.Get(player);
            var bestDistanceSquared = float.MaxValue;

            for (var vehicleIndex = 0; vehicleIndex < vehicleBuffer.Count; vehicleIndex++)
            {
                var candidate = vehicleBuffer[vehicleIndex];
                ref var vehicle = ref vehicles.Get(candidate);
                if (vehicle.State != VehicleStateType.Empty ||
                    !configDatabase.TryGetVehicle(vehicle.VehicleConfigId, out var vehicleConfig) ||
                    vehicleConfig.EnterRadius <= 0f)
                {
                    continue;
                }

                var vehiclePosition = positions.Get(candidate);
                var distanceSquared = math.distancesq(playerPosition.Value, vehiclePosition.Value);
                var enterRadiusSquared = vehicleConfig.EnterRadius * vehicleConfig.EnterRadius;
                if (distanceSquared > enterRadiusSquared || distanceSquared >= bestDistanceSquared)
                {
                    continue;
                }

                bestDistanceSquared = distanceSquared;
                vehicleEntity = candidate;
            }

            return world.Has(vehicleEntity);
        }
    }
}
