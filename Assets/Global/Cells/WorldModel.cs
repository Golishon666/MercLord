using System;
using MercLord.Game.Configs;

namespace MercLord.Global.Cells
{
    public static class WorldIds
    {
        public const int None = -1;
    }

    [Serializable]
    public sealed class WorldModel
    {
        public int Seed;
        public int CurrentDay;
        public WorldCell[] Cells = Array.Empty<WorldCell>();
        public CellNeighbours[] Neighbours = Array.Empty<CellNeighbours>();
        public WorldRoadEdge[] RoadEdges = Array.Empty<WorldRoadEdge>();
        public WorldRiverData[] Rivers = Array.Empty<WorldRiverData>();
        public WorldRiverEdge[] RiverEdges = Array.Empty<WorldRiverEdge>();
        public FactionData[] Factions = Array.Empty<FactionData>();
        public SettlementData[] Settlements = Array.Empty<SettlementData>();
        public WorldActivityData[] Activities = Array.Empty<WorldActivityData>();
        public ArmyData[] Armies = Array.Empty<ArmyData>();
        public PlayerGlobalData Player = new PlayerGlobalData();
    }

    public enum RoadType
    {
        None,
        Small,
        Medium,
        Large
    }

    public enum WorldActivityType
    {
        Camp,
        Ruins,
        Mine,
        CaravanStop
    }

    [Serializable]
    public struct WorldSpherePoint
    {
        public float X;
        public float Y;
        public float Z;

        public WorldSpherePoint(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    [Serializable]
    public struct WorldCell
    {
        public int Id;
        public WorldSpherePoint SpherePosition;
        public BiomeType Biome;
        public int RegionId;
        public float Height;
        public float Moisture;
        public float Temperature;
        public int OwnerFactionId;
        public int DominantFactionId;
        public int ResourceAmount;
        public Influence4 Influence;
        public int SettlementId;
        public bool HasRoad;
        public RoadType RoadType;
        public bool HasRiver;
        public float RiverFlow;
        public int DownstreamCellId;
        public int DistanceToWater;
        public int MovementCost;
        public bool IsPassable;
    }

    [Serializable]
    public struct Influence4
    {
        public float[] Values;

        public Influence4(int factionCount)
        {
            if (factionCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(factionCount), "Faction influence count cannot be negative.");
            }

            Values = new float[factionCount];
        }

        public int Count => Values?.Length ?? 0;

        public int DominantFactionSlot
        {
            get
            {
                if (Values == null || Values.Length == 0)
                {
                    return WorldIds.None;
                }

                var bestId = 0;
                var bestValue = Values[0];
                for (var factionSlot = 1; factionSlot < Values.Length; factionSlot++)
                {
                    if (Values[factionSlot] > bestValue)
                    {
                        bestId = factionSlot;
                        bestValue = Values[factionSlot];
                    }
                }

                return bestId;
            }
        }

        public float Get(int factionSlot)
        {
            ValidateSlot(factionSlot);
            return Values[factionSlot];
        }

        public void Set(int factionSlot, float value)
        {
            ValidateSlot(factionSlot);
            Values[factionSlot] = value;
        }

        public void EnsureFactionCount(int factionCount)
        {
            if (factionCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(factionCount), "Faction influence count cannot be negative.");
            }

            if (Values == null)
            {
                Values = new float[factionCount];
                return;
            }

            if (Values.Length == factionCount)
            {
                return;
            }

            Array.Resize(ref Values, factionCount);
        }

        private void ValidateSlot(int factionSlot)
        {
            if (Values == null)
            {
                throw new InvalidOperationException("Influence values are not initialized.");
            }

            if (factionSlot < 0 || factionSlot >= Values.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(factionSlot),
                    $"Influence supports faction slots from 0 to {Values.Length - 1}.");
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
    public struct WorldRoadEdge
    {
        public int FromCellId;
        public int ToCellId;
        public RoadType RoadType;
    }

    [Serializable]
    public struct WorldRiverEdge
    {
        public int RiverId;
        public int FromCellId;
        public int ToCellId;
        public float Flow;
    }

    [Serializable]
    public struct WorldRiverData
    {
        public int Id;
        public int SourceCellId;
        public int MouthCellId;
        public int EdgeCount;
        public float TotalFlow;
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
    public struct WorldActivityData
    {
        public int Id;
        public int FactionId;
        public int CellId;
        public WorldActivityType Type;
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
