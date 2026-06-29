using System;

namespace MercLord.Battle.Tiles
{
    [Serializable]
    public struct BattleTile
    {
        public bool Walkable;
        public byte MoveCost;
        public byte Cover;
        public byte Height;
    }
}
