using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;

namespace MercLord.Battle.UI
{
    public readonly struct BattleSquadHudEntry
    {
        public BattleSquadHudEntry(
            int squadId,
            int unitConfigId,
            int aliveCount,
            int totalCount,
            bool hasOrder,
            SquadOrderType order,
            bool hasMorale = false,
            float moralePercent = 0f,
            bool isRouted = false)
        {
            SquadId = squadId;
            UnitConfigId = unitConfigId;
            AliveCount = Math.Max(0, aliveCount);
            TotalCount = Math.Max(0, totalCount);
            HasOrder = hasOrder;
            Order = order;
            HasMorale = hasMorale;
            MoralePercent = Math.Max(0f, moralePercent);
            IsRouted = isRouted;
        }

        public int SquadId { get; }
        public int UnitConfigId { get; }
        public int AliveCount { get; }
        public int TotalCount { get; }
        public bool HasOrder { get; }
        public SquadOrderType Order { get; }
        public bool HasMorale { get; }
        public float MoralePercent { get; }
        public bool IsRouted { get; }
    }

    public readonly struct BattleVehicleHudEntry
    {
        public BattleVehicleHudEntry(
            int vehicleConfigId,
            int currentHealth,
            int maxHealth,
            VehicleStateType state)
        {
            VehicleConfigId = vehicleConfigId;
            CurrentHealth = Math.Max(0, currentHealth);
            MaxHealth = Math.Max(0, maxHealth);
            State = state;
        }

        public int VehicleConfigId { get; }
        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        public VehicleStateType State { get; }
    }

    public readonly struct BattleSquadHudSnapshot
    {
        public BattleSquadHudSnapshot(
            bool hasPlayer,
            BattleTeamType playerTeam,
            IReadOnlyList<BattleSquadHudEntry> squads,
            IReadOnlyList<BattleVehicleHudEntry> vehicles = null)
        {
            HasPlayer = hasPlayer;
            PlayerTeam = playerTeam;
            Squads = squads ?? Array.Empty<BattleSquadHudEntry>();
            Vehicles = vehicles ?? Array.Empty<BattleVehicleHudEntry>();
        }

        public bool HasPlayer { get; }
        public BattleTeamType PlayerTeam { get; }
        public IReadOnlyList<BattleSquadHudEntry> Squads { get; }
        public IReadOnlyList<BattleVehicleHudEntry> Vehicles { get; }
        public bool HasSquads => Squads.Count > 0;
        public bool HasVehicles => Vehicles.Count > 0;
    }
}
