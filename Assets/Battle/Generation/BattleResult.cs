using System;
using MercLord.Global.Cells;

namespace MercLord.Battle.Generation
{
    public enum BattleOutcome
    {
        None,
        AttackerVictory,
        DefenderVictory,
        Retreat
    }

    [Serializable]
    public sealed class BattleResult
    {
        public BattleOutcome Outcome;
        public int SourceCellId;
        public int WinnerFactionId = WorldIds.None;
        public bool PlayerSurvived;
        public bool HasPlayerPartyUpdate;
        public int CreditsReward;
        public SquadData[] PlayerParty = Array.Empty<SquadData>();
        public BattleArmyUpdate[] ArmyUpdates = Array.Empty<BattleArmyUpdate>();
        public BattleInfluenceChange[] InfluenceChanges = Array.Empty<BattleInfluenceChange>();
        public BattleLootEntry[] Loot = Array.Empty<BattleLootEntry>();
    }

    [Serializable]
    public struct BattleLootEntry
    {
        public int ItemConfigId;
        public int Amount;
        public int Durability;
    }

    [Serializable]
    public struct BattleArmyUpdate
    {
        public int ArmyId;
        public bool RemoveArmy;
        public int FactionId;
        public int CellId;
        public int TargetCellId;
        public SquadData[] Squads;
    }

    [Serializable]
    public struct BattleInfluenceChange
    {
        public int CellId;
        public int FactionId;
        public float Amount;
    }
}
