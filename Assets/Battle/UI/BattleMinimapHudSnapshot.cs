using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Unity.Mathematics;

namespace MercLord.Battle.UI
{
    public readonly struct BattleMinimapHudPin
    {
        public BattleMinimapHudPin(float2 position, BattleTeamType team, bool isPlayer)
        {
            Position = position;
            Team = team;
            IsPlayer = isPlayer;
        }

        public float2 Position { get; }
        public BattleTeamType Team { get; }
        public bool IsPlayer { get; }
    }

    public readonly struct BattleMinimapHudDangerZone
    {
        public BattleMinimapHudDangerZone(float2 position, float radius, float remainingFraction)
        {
            Position = position;
            Radius = math.max(0f, radius);
            RemainingFraction = math.clamp(remainingFraction, 0f, 1f);
        }

        public float2 Position { get; }
        public float Radius { get; }
        public float RemainingFraction { get; }
    }

    public readonly struct BattleMinimapHudSnapshot
    {
        public BattleMinimapHudSnapshot(
            bool isValid,
            int mapWidth,
            int mapHeight,
            IReadOnlyList<BattleMinimapHudPin> pins,
            IReadOnlyList<BattleMinimapHudDangerZone> dangerZones,
            int objectiveCount,
            bool hasObjectiveCaptureTeam,
            BattleTeamType objectiveCaptureTeam,
            float objectiveCaptureProgress,
            bool isObjectiveContested,
            bool hasPlayer,
            BattleTeamType playerTeam,
            int attackerAlive,
            int defenderAlive,
            bool isCompleted,
            BattleOutcome outcome)
        {
            IsValid = isValid;
            MapWidth = Math.Max(0, mapWidth);
            MapHeight = Math.Max(0, mapHeight);
            Pins = pins ?? Array.Empty<BattleMinimapHudPin>();
            DangerZones = dangerZones ?? Array.Empty<BattleMinimapHudDangerZone>();
            ObjectiveCount = Math.Max(0, objectiveCount);
            HasObjectiveCaptureTeam = hasObjectiveCaptureTeam;
            ObjectiveCaptureTeam = objectiveCaptureTeam;
            ObjectiveCaptureProgress = math.clamp(objectiveCaptureProgress, 0f, 1f);
            IsObjectiveContested = isObjectiveContested;
            HasPlayer = hasPlayer;
            PlayerTeam = playerTeam;
            AttackerAlive = Math.Max(0, attackerAlive);
            DefenderAlive = Math.Max(0, defenderAlive);
            IsCompleted = isCompleted;
            Outcome = outcome;
        }

        public bool IsValid { get; }
        public int MapWidth { get; }
        public int MapHeight { get; }
        public IReadOnlyList<BattleMinimapHudPin> Pins { get; }
        public IReadOnlyList<BattleMinimapHudDangerZone> DangerZones { get; }
        public int ObjectiveCount { get; }
        public bool HasObjectiveCaptureTeam { get; }
        public BattleTeamType ObjectiveCaptureTeam { get; }
        public float ObjectiveCaptureProgress { get; }
        public bool IsObjectiveContested { get; }
        public bool HasPlayer { get; }
        public BattleTeamType PlayerTeam { get; }
        public int AttackerAlive { get; }
        public int DefenderAlive { get; }
        public bool IsCompleted { get; }
        public BattleOutcome Outcome { get; }
    }
}
