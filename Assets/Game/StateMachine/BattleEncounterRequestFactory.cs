using System;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using MercLord.Game.Save;
using MercLord.Global.Cells;

namespace MercLord.Game.StateMachine
{
    public interface IBattleEncounterRequestFactory
    {
        EnterBattleRequest CreatePlayerVsArmy(
            SaveModel saveModel,
            int opponentArmyId,
            bool playerIsAttacker = true,
            int? seed = null,
            bool nearSettlement = false,
            bool loadScene = true);

        EnterBattleRequest CreateArmyVsArmy(
            SaveModel saveModel,
            int attackerArmyId,
            int defenderArmyId,
            int? seed = null,
            bool nearSettlement = false,
            bool loadScene = true);
    }

    public sealed class BattleEncounterRequestFactory : IBattleEncounterRequestFactory
    {
        private readonly ConfigDatabase configDatabase;

        public BattleEncounterRequestFactory(ConfigDatabase configDatabase)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
        }

        public EnterBattleRequest CreatePlayerVsArmy(
            SaveModel saveModel,
            int opponentArmyId,
            bool playerIsAttacker = true,
            int? seed = null,
            bool nearSettlement = false,
            bool loadScene = true)
        {
            var worldModel = GetWorld(saveModel);
            var player = worldModel.Player
                ?? throw new InvalidOperationException("Cannot create player battle request without player data.");
            if (player.CellId == WorldIds.None)
            {
                throw new InvalidOperationException("Cannot create player battle request without a player world cell.");
            }

            var opponentArmy = FindArmy(worldModel, opponentArmyId);
            EnsureSameCell(player.CellId, opponentArmy.CellId, "Player and opponent army must be in the same cell to start a battle.");
            EnsureCellExists(worldModel, player.CellId);

            var playerArmy = CreatePlayerArmy(worldModel);
            var opponent = CreateArmy(opponentArmy);
            return new EnterBattleRequest(
                player.CellId,
                playerIsAttacker ? playerArmy : opponent,
                playerIsAttacker ? opponent : playerArmy,
                seed,
                nearSettlement,
                loadScene);
        }

        public EnterBattleRequest CreateArmyVsArmy(
            SaveModel saveModel,
            int attackerArmyId,
            int defenderArmyId,
            int? seed = null,
            bool nearSettlement = false,
            bool loadScene = true)
        {
            var worldModel = GetWorld(saveModel);
            var attacker = FindArmy(worldModel, attackerArmyId);
            var defender = FindArmy(worldModel, defenderArmyId);
            EnsureSameCell(attacker.CellId, defender.CellId, "Armies must be in the same cell to start a battle.");
            EnsureCellExists(worldModel, attacker.CellId);

            return new EnterBattleRequest(
                attacker.CellId,
                CreateArmy(attacker),
                CreateArmy(defender),
                seed,
                nearSettlement,
                loadScene);
        }

        private BattleArmyData CreatePlayerArmy(WorldModel worldModel)
        {
            var player = worldModel.Player
                ?? throw new InvalidOperationException("Cannot create player battle army without player data.");

            return new BattleArmyData
            {
                FactionId = ResolvePlayerFactionId(player),
                CellId = player.CellId,
                TargetCellId = player.CellId,
                IsPlayerParty = true,
                Squads = CloneSquads(player.Party)
            };
        }

        private static BattleArmyData CreateArmy(ArmyData army)
        {
            return new BattleArmyData
            {
                ArmyId = army.Id,
                FactionId = army.FactionId,
                CellId = army.CellId,
                TargetCellId = army.TargetCellId,
                Squads = CloneSquads(army.Squads)
            };
        }

        private int ResolvePlayerFactionId(PlayerGlobalData player)
        {
            if (player.FactionId != WorldIds.None &&
                configDatabase.TryGetFaction(player.FactionId, out _))
            {
                return player.FactionId;
            }

            var fallbackFactionId = configDatabase.BattleSimulation?.PlayerUnit?.FactionId ?? WorldIds.None;
            if (fallbackFactionId == WorldIds.None ||
                !configDatabase.TryGetFaction(fallbackFactionId, out _))
            {
                throw new InvalidOperationException("Cannot resolve a registered player faction for battle.");
            }

            return fallbackFactionId;
        }

        private static SaveModel GetSave(SaveModel saveModel)
        {
            return saveModel ?? throw new ArgumentNullException(nameof(saveModel));
        }

        private static WorldModel GetWorld(SaveModel saveModel)
        {
            return GetSave(saveModel).World
                ?? throw new InvalidOperationException("Cannot create battle request without a world model.");
        }

        private static ArmyData FindArmy(WorldModel worldModel, int armyId)
        {
            var armies = worldModel.Armies ?? Array.Empty<ArmyData>();
            for (var armyIndex = 0; armyIndex < armies.Length; armyIndex++)
            {
                if (armies[armyIndex].Id == armyId)
                {
                    return armies[armyIndex];
                }
            }

            throw new InvalidOperationException($"Cannot create battle request for unknown army id {armyId}.");
        }

        private static SquadData[] CloneSquads(SquadData[] squads)
        {
            if (squads == null || squads.Length == 0)
            {
                return Array.Empty<SquadData>();
            }

            var clone = new SquadData[squads.Length];
            Array.Copy(squads, clone, squads.Length);
            return clone;
        }

        private static void EnsureSameCell(int firstCellId, int secondCellId, string message)
        {
            if (firstCellId == WorldIds.None || firstCellId != secondCellId)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void EnsureCellExists(WorldModel worldModel, int cellId)
        {
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (cells[cellIndex].Id == cellId)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Cannot create battle request for unknown world cell id {cellId}.");
        }
    }
}
