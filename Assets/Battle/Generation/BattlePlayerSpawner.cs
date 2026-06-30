using System;
using MercLord.Battle.ECS.Components;
using MercLord.Game.Configs;
using MercLord.Game.Save;
using MercLord.Global.Cells;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.Generation
{
    public sealed class BattlePlayerSpawner : IBattlePlayerSpawner
    {
        private readonly ConfigDatabase configDatabase;
        private readonly ISaveService saveService;
        private readonly IBattleEntityFactory entityFactory;

        public BattlePlayerSpawner(
            ConfigDatabase configDatabase,
            ISaveService saveService,
            IBattleEntityFactory entityFactory)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
            this.saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            this.entityFactory = entityFactory ?? throw new ArgumentNullException(nameof(entityFactory));
        }

        public Entity SpawnPlayer(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var world = session.World ?? throw new InvalidOperationException("BattlePlayerSpawner requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattlePlayerSpawner cannot spawn into a disposed Morpeh world.");
            }

            if (TryFindExistingPlayer(world, out var existingPlayer))
            {
                return existingPlayer;
            }

            var simulationConfig = configDatabase.BattleSimulation
                ?? throw new InvalidOperationException("BattleSimulationConfig is required to spawn the player.");
            var unitConfig = simulationConfig.PlayerUnit
                ?? throw new InvalidOperationException("BattleSimulationConfig requires a player UnitConfig.");

            if (!configDatabase.TryGetUnit(unitConfig.Id, out _))
            {
                throw new InvalidOperationException($"Player UnitConfig id {unitConfig.Id} is not registered.");
            }

            var spawnPoints = GetSpawnPoints(session.Model, simulationConfig.PlayerSpawnSide);
            if (simulationConfig.PlayerSpawnPointIndex < 0 ||
                simulationConfig.PlayerSpawnPointIndex >= spawnPoints.Length)
            {
                throw new InvalidOperationException("BattleSimulationConfig player spawn point index is outside generated spawn points.");
            }

            var mapConfig = configDatabase.BattleMapGeneration
                ?? throw new InvalidOperationException("BattleMapGenerationConfig is required to spawn the player.");
            var spawnPoint = spawnPoints[simulationConfig.PlayerSpawnPointIndex];
            var factionId = ResolveFactionId(unitConfig);
            if (!configDatabase.TryGetFaction(factionId, out _))
            {
                throw new InvalidOperationException($"Player faction id {factionId} is not registered.");
            }

            return entityFactory.CreateUnit(
                world,
                new BattleEntitySpawnRequest(
                    unitConfig,
                    factionId,
                    ToTeam(simulationConfig.PlayerSpawnSide),
                    BattleSpawnPositionResolver.ResolveCenter(spawnPoint, mapConfig),
                    playerControlled: true,
                    simulationConfig.PlayerSpawnPointIndex,
                    spawnPoints.Length));
        }

        private int ResolveFactionId(UnitConfig unitConfig)
        {
            var saveModel = saveService.Current;
            if (saveModel?.World?.Player != null &&
                saveModel.World.Player.FactionId != WorldIds.None &&
                configDatabase.TryGetFaction(saveModel.World.Player.FactionId, out _))
            {
                return saveModel.World.Player.FactionId;
            }

            return unitConfig.FactionId;
        }

        private static BattleSpawnPoint[] GetSpawnPoints(BattleModel model, BattleSpawnSide side)
        {
            if (model == null)
            {
                throw new InvalidOperationException("BattlePlayerSpawner requires a BattleModel.");
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

        private static bool TryFindExistingPlayer(World world, out Entity player)
        {
            var filter = world.Filter
                .With<PlayerControlledComponent>()
                .Build();

            foreach (var entity in filter)
            {
                player = entity;
                filter.Dispose();
                return true;
            }

            filter.Dispose();
            player = default;
            return false;
        }
    }
}
