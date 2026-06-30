using System;
using MercLord.Battle.Tiles;
using MercLord.Game.Configs;
using MercLord.Global.Cells;
using Unity.Mathematics;
using UnityEngine;

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
        public BattleSpawnZone[] SpawnZones = Array.Empty<BattleSpawnZone>();
        public BattleObjectiveZone[] Objectives = Array.Empty<BattleObjectiveZone>();
        public BattleSpawnPoint[] AttackerSpawnPoints = Array.Empty<BattleSpawnPoint>();
        public BattleSpawnPoint[] DefenderSpawnPoints = Array.Empty<BattleSpawnPoint>();
        public BattleArmyData Attacker = new BattleArmyData();
        public BattleArmyData Defender = new BattleArmyData();
    }

    [Serializable]
    public struct BattleSpawnPoint
    {
        public int X;
        public int Y;
    }

    [Serializable]
    public struct BattleSpawnZone
    {
        public BattleSpawnSide Side;
        public RectInt Area;
        public Vector2Int ForwardDirection;

        public bool Contains(int x, int y)
        {
            return Area.Contains(new Vector2Int(x, y));
        }
    }

    public enum BattleObjectiveType : byte
    {
        EliminateEnemies = 0,
        ControlPoint = 1
    }

    [Serializable]
    public struct BattleObjectiveZone
    {
        public BattleObjectiveType Type;
        public RectInt Area;
        public int Priority;
        public float2 Center => new float2(
            Area.xMin + Area.width * 0.5f,
            Area.yMin + Area.height * 0.5f);

        public bool Contains(int x, int y)
        {
            return Area.Contains(new Vector2Int(x, y));
        }
    }

    [Serializable]
    public sealed class BattleArmyData
    {
        public int ArmyId = WorldIds.None;
        public int FactionId;
        public int CellId = WorldIds.None;
        public int TargetCellId = WorldIds.None;
        public bool IsPlayerParty;
        public SquadData[] Squads = Array.Empty<SquadData>();
    }
}
