using System;
using MercLord.Battle.Tiles;
using MercLord.Global.Cells;

namespace MercLord.Battle.Generation
{
    [Serializable]
    public sealed class BattleModel
    {
        public int Seed;
        public int SourceCellId;
        public int Width;
        public int Height;
        public BattleTile[] Tiles = Array.Empty<BattleTile>();
        public BattleArmyData Attacker = new BattleArmyData();
        public BattleArmyData Defender = new BattleArmyData();
    }

    [Serializable]
    public sealed class BattleArmyData
    {
        public int FactionId;
        public SquadData[] Squads = Array.Empty<SquadData>();
    }
}
