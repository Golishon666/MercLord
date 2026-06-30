using System;
using MercLord.Battle.ECS.Components;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.Generation
{
    public sealed class BattleVehicleSpawner : IBattleVehicleSpawner
    {
        private readonly ConfigDatabase configDatabase;
        private readonly IBattleEntityFactory entityFactory;

        public BattleVehicleSpawner(
            ConfigDatabase configDatabase,
            IBattleEntityFactory entityFactory)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
            this.entityFactory = entityFactory ?? throw new ArgumentNullException(nameof(entityFactory));
        }

        public void SpawnVehicles(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var world = session.World ?? throw new InvalidOperationException("BattleVehicleSpawner requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattleVehicleSpawner cannot spawn into a disposed Morpeh world.");
            }

            if (HasSpawnedVehicles(world))
            {
                return;
            }

            var simulationConfig = configDatabase.BattleSimulation
                ?? throw new InvalidOperationException("BattleSimulationConfig is required to spawn vehicles.");
            var spawns = simulationConfig.VehicleSpawns;
            for (var spawnIndex = 0; spawnIndex < spawns.Length; spawnIndex++)
            {
                SpawnVehicle(session.Model, world, spawns[spawnIndex], spawnIndex);
            }
        }

        private void SpawnVehicle(
            BattleModel model,
            World world,
            BattleVehicleSpawnConfig spawn,
            int spawnIndex)
        {
            var vehicleConfig = spawn.Vehicle
                ?? throw new InvalidOperationException($"Battle vehicle spawn {spawnIndex} has no VehicleConfig.");
            if (!configDatabase.TryGetVehicle(vehicleConfig.Id, out _))
            {
                throw new InvalidOperationException($"VehicleConfig id {vehicleConfig.Id} is not registered.");
            }

            if (!configDatabase.TryGetFaction(spawn.FactionId, out _))
            {
                throw new InvalidOperationException($"Battle vehicle spawn {spawnIndex} references missing FactionConfig id {spawn.FactionId}.");
            }

            var spawnPoints = GetSpawnPoints(model, spawn.SpawnSide);
            if (spawn.SpawnPointIndex < 0 || spawn.SpawnPointIndex >= spawnPoints.Length)
            {
                throw new InvalidOperationException($"Battle vehicle spawn {spawnIndex} point index is outside generated spawn points.");
            }

            var spawnPoint = spawnPoints[spawn.SpawnPointIndex];
            entityFactory.CreateVehicle(
                world,
                new BattleVehicleEntitySpawnRequest(
                    vehicleConfig,
                    spawn.FactionId,
                    ToTeam(spawn.SpawnSide),
                    new float2(spawnPoint.X, spawnPoint.Y),
                    ToVehicleState(spawn.ControlMode)));
        }

        private static BattleSpawnPoint[] GetSpawnPoints(BattleModel model, BattleSpawnSide side)
        {
            if (model == null)
            {
                throw new InvalidOperationException("BattleVehicleSpawner requires a BattleModel.");
            }

            return side == BattleSpawnSide.Attacker
                ? model.AttackerSpawnPoints
                : model.DefenderSpawnPoints;
        }

        private static BattleTeamType ToTeam(BattleSpawnSide side)
        {
            return side == BattleSpawnSide.Attacker
                ? BattleTeamType.Attacker
                : BattleTeamType.Defender;
        }

        private static VehicleStateType ToVehicleState(VehicleSpawnControlMode controlMode)
        {
            return controlMode == VehicleSpawnControlMode.AIControlled
                ? VehicleStateType.AIControlled
                : VehicleStateType.Empty;
        }

        private static bool HasSpawnedVehicles(World world)
        {
            var filter = world.Filter
                .With<VehicleComponent>()
                .Build();

            foreach (var _ in filter)
            {
                filter.Dispose();
                return true;
            }

            filter.Dispose();
            return false;
        }
    }
}
