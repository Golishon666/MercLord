using System;

namespace MercLord.Battle.Tiles
{
    [Flags]
    public enum MoveLayer : byte
    {
        None = 0,
        Infantry = 1 << 0,
        Vehicle = 1 << 1,
        Flying = 1 << 2
    }

    public enum CoverType : byte
    {
        None = 0,
        Light = 1,
        Medium = 2,
        Heavy = 3
    }

    public enum BattleTileSurface : byte
    {
        Ground = 0,
        Road = 1,
        Obstacle = 2
    }

    [Serializable]
    public struct BattleTile
    {
        public bool Walkable;
        public BattleTileSurface Surface;
        public byte MoveCost;
        public CoverType Cover;
        public sbyte Height;
        public MoveLayer AllowedMoveLayers;
        public bool BlocksLineOfSight;
        public bool BlocksProjectiles;
        public ushort RegionId;
    }
}
