using System;
using System.Collections.Generic;
using MercLord.Economy.Credits;
using MercLord.Game.Save;
using MercLord.Global.Cells;
using MercLord.Player.Inventory;

namespace MercLord.Battle.Generation
{
    public sealed class BattleResultApplier : IBattleResultApplier
    {
        private readonly IInfluenceService influenceService;
        private readonly CreditsService creditsService;
        private readonly IInventoryService inventoryService;

        public BattleResultApplier(
            IInfluenceService influenceService,
            CreditsService creditsService,
            IInventoryService inventoryService)
        {
            this.influenceService = influenceService ?? throw new ArgumentNullException(nameof(influenceService));
            this.creditsService = creditsService ?? throw new ArgumentNullException(nameof(creditsService));
            this.inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        }

        public void Apply(SaveModel saveModel, BattleResult result)
        {
            if (saveModel == null)
            {
                throw new ArgumentNullException(nameof(saveModel));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            ApplyPlayerRewards(saveModel, result);
            ApplyPlayerParty(saveModel.World, result);
            ApplyArmyUpdates(saveModel.World, result);
            ApplyInfluenceChanges(saveModel.World, result);
        }

        private void ApplyPlayerRewards(SaveModel saveModel, BattleResult result)
        {
            EnsureSaveGraph(saveModel);

            if (result.CreditsReward < 0)
            {
                throw new InvalidOperationException("Battle result credit reward cannot be negative.");
            }

            saveModel.World.Player.Credits = creditsService.AddCredits(
                saveModel.World.Player.Credits,
                result.CreditsReward);

            var lootEntries = result.Loot ?? Array.Empty<BattleLootEntry>();
            for (var lootIndex = 0; lootIndex < lootEntries.Length; lootIndex++)
            {
                var loot = lootEntries[lootIndex];
                inventoryService.AddItem(
                    saveModel.Inventory,
                    loot.ItemConfigId,
                    loot.Amount,
                    loot.Durability);
            }
        }

        private static void ApplyPlayerParty(WorldModel worldModel, BattleResult result)
        {
            EnsureWorldGraph(worldModel);

            if (!result.HasPlayerPartyUpdate)
            {
                return;
            }

            worldModel.Player.Party = result.PlayerParty ?? Array.Empty<SquadData>();
        }

        private static void ApplyArmyUpdates(WorldModel worldModel, BattleResult result)
        {
            EnsureWorldGraph(worldModel);

            var armyUpdates = result.ArmyUpdates ?? Array.Empty<BattleArmyUpdate>();
            if (armyUpdates.Length == 0)
            {
                return;
            }

            var armies = new List<ArmyData>(worldModel.Armies ?? Array.Empty<ArmyData>());
            for (var updateIndex = 0; updateIndex < armyUpdates.Length; updateIndex++)
            {
                var update = armyUpdates[updateIndex];
                var armyIndex = FindArmyIndex(armies, update.ArmyId);

                if (update.RemoveArmy)
                {
                    if (armyIndex >= 0)
                    {
                        armies.RemoveAt(armyIndex);
                    }

                    continue;
                }

                var army = new ArmyData
                {
                    Id = update.ArmyId,
                    FactionId = update.FactionId,
                    CellId = update.CellId,
                    TargetCellId = update.TargetCellId,
                    Squads = update.Squads ?? Array.Empty<SquadData>()
                };

                if (armyIndex >= 0)
                {
                    armies[armyIndex] = army;
                }
                else
                {
                    armies.Add(army);
                }
            }

            worldModel.Armies = armies.ToArray();
        }

        private void ApplyInfluenceChanges(WorldModel worldModel, BattleResult result)
        {
            EnsureWorldGraph(worldModel);

            var influenceChanges = result.InfluenceChanges ?? Array.Empty<BattleInfluenceChange>();
            for (var changeIndex = 0; changeIndex < influenceChanges.Length; changeIndex++)
            {
                var change = influenceChanges[changeIndex];
                if (Math.Abs(change.Amount) <= float.Epsilon)
                {
                    continue;
                }

                var cellIndex = FindCellIndex(worldModel, change.CellId);
                var factionSlot = FindFactionSlot(worldModel, change.FactionId);
                var cell = worldModel.Cells[cellIndex];

                cell.Influence = influenceService.AddInfluence(cell.Influence, factionSlot, change.Amount);
                var dominantFactionSlot = influenceService.GetDominantFactionSlot(cell.Influence);
                if (dominantFactionSlot >= worldModel.Factions.Length)
                {
                    throw new InvalidOperationException(
                        $"World influence slot {dominantFactionSlot} has no configured faction.");
                }

                var dominantFactionId = worldModel.Factions[dominantFactionSlot].Id;

                cell.DominantFactionId = dominantFactionId;
                cell.OwnerFactionId = dominantFactionId;
                worldModel.Cells[cellIndex] = cell;
            }
        }

        private static int FindArmyIndex(IReadOnlyList<ArmyData> armies, int armyId)
        {
            for (var armyIndex = 0; armyIndex < armies.Count; armyIndex++)
            {
                if (armies[armyIndex].Id == armyId)
                {
                    return armyIndex;
                }
            }

            return WorldIds.None;
        }

        private static void EnsureSaveGraph(SaveModel saveModel)
        {
            saveModel.World ??= new WorldModel();
            saveModel.Inventory ??= new PlayerInventory();
            EnsureWorldGraph(saveModel.World);
        }

        private static void EnsureWorldGraph(WorldModel worldModel)
        {
            worldModel.Cells ??= Array.Empty<WorldCell>();
            worldModel.Factions ??= Array.Empty<FactionData>();
            worldModel.Armies ??= Array.Empty<ArmyData>();
            worldModel.Player ??= new PlayerGlobalData();
            worldModel.Player.Party ??= Array.Empty<SquadData>();
        }

        private static int FindCellIndex(WorldModel worldModel, int cellId)
        {
            for (var cellIndex = 0; cellIndex < worldModel.Cells.Length; cellIndex++)
            {
                if (worldModel.Cells[cellIndex].Id == cellId)
                {
                    return cellIndex;
                }
            }

            throw new InvalidOperationException($"Battle result references unknown world cell id {cellId}.");
        }

        private static int FindFactionSlot(WorldModel worldModel, int factionId)
        {
            for (var factionSlot = 0; factionSlot < worldModel.Factions.Length; factionSlot++)
            {
                if (worldModel.Factions[factionSlot].Id == factionId)
                {
                    return factionSlot;
                }
            }

            throw new InvalidOperationException($"Battle result references unknown faction id {factionId}.");
        }
    }
}
