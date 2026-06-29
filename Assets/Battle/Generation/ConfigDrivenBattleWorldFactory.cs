using System;
using MercLord.Battle.ECS.Components;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.Generation
{
    public sealed class ConfigDrivenBattleWorldFactory : IBattleWorldFactory
    {
        private readonly ConfigDatabase configDatabase;
        private readonly IBattleEntityFactory entityFactory;

        public ConfigDrivenBattleWorldFactory(
            ConfigDatabase configDatabase,
            IBattleEntityFactory entityFactory)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
            this.entityFactory = entityFactory ?? throw new ArgumentNullException(nameof(entityFactory));
        }

        public World CreateWorld(BattleModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            ValidateModel(model);

            var world = World.Create();
            if (world == null)
            {
                throw new InvalidOperationException("Morpeh world could not be created.");
            }

            try
            {
                CreateArmy(world, model.Attacker, model.AttackerSpawnPoints, BattleTeamType.Attacker);
                CreateArmy(world, model.Defender, model.DefenderSpawnPoints, BattleTeamType.Defender);
                return world;
            }
            catch
            {
                DisposeWorld(world);
                throw;
            }
        }

        public void DisposeWorld(World world)
        {
            if (world == null || world.IsDisposed)
            {
                return;
            }

            world.Dispose();
        }

        private void CreateArmy(
            World world,
            BattleArmyData army,
            BattleSpawnPoint[] spawnPoints,
            BattleTeamType team)
        {
            if (army == null)
            {
                return;
            }

            ValidateArmy(army);

            var unitCount = CountUnits(army);
            if (unitCount == 0)
            {
                return;
            }

            if (spawnPoints == null || spawnPoints.Length < unitCount)
            {
                throw new InvalidOperationException(
                    $"{team} army requires {unitCount} spawn points, but battle map provides {spawnPoints?.Length ?? 0}.");
            }

            var spawnIndex = 0;
            for (var squadIndex = 0; squadIndex < army.Squads.Length; squadIndex++)
            {
                var squad = army.Squads[squadIndex];
                if (squad.Count == 0)
                {
                    continue;
                }

                var unitConfig = GetUnitConfig(squad.UnitConfigId);
                for (var unitIndex = 0; unitIndex < squad.Count; unitIndex++)
                {
                    var spawnPoint = spawnPoints[spawnIndex];
                    entityFactory.CreateUnit(
                        world,
                        new BattleEntitySpawnRequest(
                            unitConfig,
                            army.FactionId,
                            team,
                            new float2(spawnPoint.X, spawnPoint.Y),
                            playerControlled: false,
                            spawnIndex,
                            unitCount));
                    spawnIndex++;
                }
            }
        }

        private UnitConfig GetUnitConfig(int unitConfigId)
        {
            if (!configDatabase.TryGetUnit(unitConfigId, out var unitConfig))
            {
                throw new InvalidOperationException($"UnitConfig id {unitConfigId} is not registered.");
            }

            return unitConfig;
        }

        private static void ValidateModel(BattleModel model)
        {
            if (model.Width <= 0 || model.Height <= 0)
            {
                throw new InvalidOperationException("BattleModel dimensions must be positive.");
            }

            if (model.Tiles == null || model.Tiles.Length != model.Width * model.Height)
            {
                throw new InvalidOperationException("BattleModel tiles must match map dimensions.");
            }
        }

        private void ValidateArmy(BattleArmyData army)
        {
            if (!configDatabase.TryGetFaction(army.FactionId, out _))
            {
                throw new InvalidOperationException($"FactionConfig id {army.FactionId} is not registered.");
            }

            if (army.Squads == null)
            {
                throw new InvalidOperationException("Battle army squads must not be null.");
            }

            for (var squadIndex = 0; squadIndex < army.Squads.Length; squadIndex++)
            {
                var squad = army.Squads[squadIndex];
                if (squad.Count < 0)
                {
                    throw new InvalidOperationException($"Squad {squadIndex} has negative unit count.");
                }

                if (squad.Count > 0)
                {
                    _ = GetUnitConfig(squad.UnitConfigId);
                }
            }
        }

        private static int CountUnits(BattleArmyData army)
        {
            var count = 0;
            for (var squadIndex = 0; squadIndex < army.Squads.Length; squadIndex++)
            {
                count += army.Squads[squadIndex].Count;
            }

            return count;
        }
    }
}
