using System;

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
        public int WinnerFactionId;
        public int CreditsReward;
        public BattleLootEntry[] Loot = Array.Empty<BattleLootEntry>();
    }

    [Serializable]
    public struct BattleLootEntry
    {
        public int ItemConfigId;
        public int Count;
    }
}
