using System;
using MercLord.Game.Configs;

namespace MercLord.Global.Cells
{
    [Serializable]
    public sealed class WorldModel
    {
        public int Seed;
        public int CurrentDay;
        public WorldCell[] Cells = Array.Empty<WorldCell>();
        public CellNeighbours[] Neighbours = Array.Empty<CellNeighbours>();
        public FactionData[] Factions = Array.Empty<FactionData>();
        public SettlementData[] Settlements = Array.Empty<SettlementData>();
        public ArmyData[] Armies = Array.Empty<ArmyData>();
        public PlayerGlobalData Player = new PlayerGlobalData();
    }

    [Serializable]
    public struct WorldCell
    {
        public int Id;
        public BiomeType Biome;
        public int RegionId;
        public float Height;
        public float Moisture;
        public float Temperature;
        public int OwnerFactionId;
        public int DominantFactionId;
        public Influence4 Influence;
        public int SettlementId;
        public bool HasRoad;
        public bool IsPassable;
    }

    [Serializable]
    public struct Influence4
    {
        public float F0;
        public float F1;
        public float F2;
        public float F3;

        public int DominantFactionId
        {
            get
            {
                var bestId = 0;
                var bestValue = F0;

                if (F1 > bestValue)
                {
                    bestId = 1;
                    bestValue = F1;
                }

                if (F2 > bestValue)
                {
                    bestId = 2;
                    bestValue = F2;
                }

                if (F3 > bestValue)
                {
                    bestId = 3;
                }

                return bestId;
            }
        }
    }

    [Serializable]
    public struct CellNeighbours
    {
        public int N0;
        public int N1;
        public int N2;
        public int N3;
        public int N4;
        public int N5;
    }

    [Serializable]
    public struct FactionData
    {
        public int Id;
        public int Credits;
        public int CapitalCellId;
        public int Strength;
    }

    [Serializable]
    public struct SettlementData
    {
        public int Id;
        public int FactionId;
        public int CellId;
    }

    [Serializable]
    public struct ArmyData
    {
        public int Id;
        public int FactionId;
        public int CellId;
        public int TargetCellId;
        public SquadData[] Squads;
    }

    [Serializable]
    public struct SquadData
    {
        public int UnitConfigId;
        public int Count;
    }

    [Serializable]
    public sealed class PlayerGlobalData
    {
        public int CultureId;
        public int FactionId;
        public int CellId;
        public int Credits;
        public SquadData[] Party = Array.Empty<SquadData>();
    }
}
