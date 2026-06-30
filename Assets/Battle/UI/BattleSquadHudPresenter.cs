using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;

namespace MercLord.Battle.UI
{
    public sealed class BattleSquadHudPresenter : IDisposable
    {
        private readonly List<BattleSquadHudEntry> entries = new List<BattleSquadHudEntry>(16);
        private readonly List<BattleVehicleHudEntry> vehicleEntries = new List<BattleVehicleHudEntry>(4);
        private readonly Dictionary<int, int> aliveBySquadId = new Dictionary<int, int>();

        private BattleSession session;
        private World world;
        private Filter playerFilter;
        private Filter vehicleFilter;
        private Filter squadFilter;
        private Filter memberFilter;
        private Stash<TeamComponent> teams;
        private Stash<SquadComponent> squads;
        private Stash<SquadOrderComponent> orders;
        private Stash<SquadMoraleComponent> morale;
        private Stash<SquadMemberComponent> members;
        private Stash<HealthComponent> health;
        private Stash<DeadComponent> dead;
        private Stash<VehicleComponent> vehicles;

        public void Bind(BattleSession session)
        {
            DisposeFilters();

            this.session = session ?? throw new ArgumentNullException(nameof(session));
            world = session.World ?? throw new InvalidOperationException("BattleSquadHudPresenter requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattleSquadHudPresenter cannot bind to a disposed Morpeh world.");
            }

            playerFilter = world.Filter
                .With<PlayerControlledComponent>()
                .With<TeamComponent>()
                .Without<DeadComponent>()
                .Build();
            vehicleFilter = world.Filter
                .With<PlayerControlledComponent>()
                .With<VehicleComponent>()
                .With<HealthComponent>()
                .With<TeamComponent>()
                .Without<DeadComponent>()
                .Build();
            squadFilter = world.Filter
                .With<SquadComponent>()
                .Build();
            memberFilter = world.Filter
                .With<SquadMemberComponent>()
                .With<HealthComponent>()
                .Build();
            teams = world.GetStash<TeamComponent>();
            squads = world.GetStash<SquadComponent>();
            orders = world.GetStash<SquadOrderComponent>();
            morale = world.GetStash<SquadMoraleComponent>();
            members = world.GetStash<SquadMemberComponent>();
            health = world.GetStash<HealthComponent>();
            dead = world.GetStash<DeadComponent>();
            vehicles = world.GetStash<VehicleComponent>();
        }

        public BattleSquadHudSnapshot BuildSnapshot()
        {
            entries.Clear();
            vehicleEntries.Clear();
            aliveBySquadId.Clear();

            if (session == null ||
                world == null ||
                world.IsDisposed ||
                playerFilter == null ||
                squadFilter == null ||
                memberFilter == null)
            {
                return new BattleSquadHudSnapshot(false, BattleTeamType.Attacker, Array.Empty<BattleSquadHudEntry>());
            }

            var hasPlayer = TryGetPlayerTeam(out var playerTeam);
            if (!hasPlayer)
            {
                return new BattleSquadHudSnapshot(false, BattleTeamType.Attacker, entries);
            }

            CountAliveMembers();
            BuildVehicleEntries(playerTeam);
            foreach (var entity in squadFilter)
            {
                var squad = squads.Get(entity);
                if (squad.Team != playerTeam)
                {
                    continue;
                }

                aliveBySquadId.TryGetValue(squad.SquadId, out var aliveCount);
                var totalCount = Math.Max(0, squad.MemberCount);
                if (totalCount > 0)
                {
                    aliveCount = Math.Min(aliveCount, totalCount);
                }

                ref var squadMorale = ref morale.Get(entity, out var hasMorale);
                var moralePercent = hasMorale && squadMorale.Max > 0f
                    ? squadMorale.Current / squadMorale.Max
                    : 0f;
                entries.Add(new BattleSquadHudEntry(
                    squad.SquadId,
                    squad.UnitConfigId,
                    aliveCount,
                    totalCount,
                    orders.Has(entity),
                    orders.Has(entity) ? orders.Get(entity).Value : SquadOrderType.AttackNearest,
                    hasMorale,
                    moralePercent,
                    hasMorale && squadMorale.IsRouted));
            }

            entries.Sort((left, right) => left.SquadId.CompareTo(right.SquadId));
            return new BattleSquadHudSnapshot(true, playerTeam, entries, vehicleEntries);
        }

        public void Dispose()
        {
            DisposeFilters();
            entries.Clear();
            vehicleEntries.Clear();
            aliveBySquadId.Clear();
            session = null;
            world = null;
            teams = null;
            squads = null;
            orders = null;
            morale = null;
            members = null;
            health = null;
            dead = null;
            vehicles = null;
        }

        private bool TryGetPlayerTeam(out BattleTeamType playerTeam)
        {
            playerTeam = BattleTeamType.Attacker;
            foreach (var entity in playerFilter)
            {
                playerTeam = teams.Get(entity).Value;
                return true;
            }

            return false;
        }

        private void CountAliveMembers()
        {
            foreach (var entity in memberFilter)
            {
                if (!IsAlive(entity))
                {
                    continue;
                }

                var squadId = members.Get(entity).SquadId;
                aliveBySquadId.TryGetValue(squadId, out var count);
                aliveBySquadId[squadId] = count + 1;
            }
        }

        private void BuildVehicleEntries(BattleTeamType playerTeam)
        {
            if (vehicleFilter == null)
            {
                return;
            }

            foreach (var entity in vehicleFilter)
            {
                if (teams.Get(entity).Value != playerTeam)
                {
                    continue;
                }

                var vehicle = vehicles.Get(entity);
                var vehicleHealth = health.Get(entity);
                vehicleEntries.Add(new BattleVehicleHudEntry(
                    vehicle.VehicleConfigId,
                    vehicleHealth.Current,
                    vehicleHealth.Max,
                    vehicle.State));
            }
        }

        private bool IsAlive(Entity entity)
        {
            if (dead.Has(entity))
            {
                return false;
            }

            return health.Get(entity).Current > 0;
        }

        private void DisposeFilters()
        {
            if (world != null && !world.IsDisposed)
            {
                playerFilter?.Dispose();
                vehicleFilter?.Dispose();
                squadFilter?.Dispose();
                memberFilter?.Dispose();
            }

            playerFilter = null;
            vehicleFilter = null;
            squadFilter = null;
            memberFilter = null;
        }
    }
}
