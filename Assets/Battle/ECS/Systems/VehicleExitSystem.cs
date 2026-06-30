using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class VehicleExitSystem : IBattleRuntimeSystem
    {
        private readonly ConfigDatabase configDatabase;
        private readonly List<Entity> vehicleBuffer = new List<Entity>();

        private World world;
        private Filter filter;
        private Stash<PlayerInputComponent> playerInputs;
        private Stash<PlayerControlledComponent> playerControlled;
        private Stash<DriverComponent> drivers;
        private Stash<VehicleComponent> vehicles;
        private Stash<PositionComponent> positions;
        private Stash<VelocityComponent> velocities;

        public VehicleExitSystem(ConfigDatabase configDatabase)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("VehicleExitSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("VehicleExitSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<PlayerControlledComponent>()
                .With<PlayerInputComponent>()
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
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            vehicleBuffer.Clear();
            foreach (var vehicle in filter)
            {
                vehicleBuffer.Add(vehicle);
            }

            for (var vehicleIndex = 0; vehicleIndex < vehicleBuffer.Count; vehicleIndex++)
            {
                TryExitVehicle(vehicleBuffer[vehicleIndex]);
            }

            vehicleBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            vehicleBuffer.Clear();
            filter = null;
            world = null;
            playerInputs = null;
            playerControlled = null;
            drivers = null;
            vehicles = null;
            positions = null;
            velocities = null;
        }

        private void TryExitVehicle(Entity vehicleEntity)
        {
            var input = playerInputs.Get(vehicleEntity);
            if (!input.InteractPressed)
            {
                return;
            }

            ref var vehicle = ref vehicles.Get(vehicleEntity);
            if (vehicle.State != VehicleStateType.PlayerControlled ||
                !world.Has(vehicle.Driver))
            {
                return;
            }

            var driver = vehicle.Driver;
            if (!positions.Has(driver) || !velocities.Has(driver))
            {
                return;
            }

            if (!configDatabase.TryGetVehicle(vehicle.VehicleConfigId, out var vehicleConfig))
            {
                throw new InvalidOperationException($"VehicleConfig id {vehicle.VehicleConfigId} is not registered.");
            }

            var exitDirection = ResolveExitDirection(input);
            var vehiclePosition = positions.Get(vehicleEntity);
            positions.Get(driver).Value = vehiclePosition.Value + exitDirection * vehicleConfig.ExitDistance;
            velocities.Get(driver).Value = float2.zero;
            velocities.Get(vehicleEntity).Value = float2.zero;

            vehicle.State = VehicleStateType.Empty;
            vehicle.Driver = default;

            drivers.Remove(driver);
            playerControlled.Remove(vehicleEntity);
            playerInputs.Remove(vehicleEntity);

            playerControlled.Set(driver, new PlayerControlledComponent());
            playerInputs.Set(driver, input);
        }

        private static float2 ResolveExitDirection(PlayerInputComponent input)
        {
            if (math.lengthsq(input.AimDirection) > float.Epsilon)
            {
                return math.normalizesafe(input.AimDirection);
            }

            if (math.lengthsq(input.MoveDirection) > float.Epsilon)
            {
                return math.normalizesafe(input.MoveDirection);
            }

            return new float2(1f, 0f);
        }
    }
}
