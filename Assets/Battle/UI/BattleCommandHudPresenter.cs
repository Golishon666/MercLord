using System;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;

namespace MercLord.Battle.UI
{
    public sealed class BattleCommandHudPresenter : IDisposable
    {
        private World world;
        private Filter playerFilter;
        private Filter squadFilter;
        private Stash<TeamComponent> teams;
        private Stash<SquadComponent> squads;
        private Stash<SquadOrderComponent> orders;

        public void Bind(BattleSession session)
        {
            DisposeFilters();

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("BattleCommandHudPresenter requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattleCommandHudPresenter cannot bind to a disposed Morpeh world.");
            }

            playerFilter = world.Filter
                .With<PlayerControlledComponent>()
                .With<TeamComponent>()
                .Without<DeadComponent>()
                .Build();
            squadFilter = world.Filter
                .With<SquadComponent>()
                .With<SquadOrderComponent>()
                .Build();
            teams = world.GetStash<TeamComponent>();
            squads = world.GetStash<SquadComponent>();
            orders = world.GetStash<SquadOrderComponent>();
        }

        public BattleCommandHudSnapshot BuildSnapshot()
        {
            if (world == null ||
                world.IsDisposed ||
                playerFilter == null ||
                squadFilter == null ||
                !TryGetPlayerTeam(out var playerTeam))
            {
                return new BattleCommandHudSnapshot(
                    false,
                    false,
                    SquadOrderType.AttackNearest,
                    0,
                    0);
            }

            var friendlySquadCount = 0;
            var mixedOrderCount = 0;
            var hasPrimaryOrder = false;
            var primaryOrder = SquadOrderType.AttackNearest;

            foreach (var squadEntity in squadFilter)
            {
                var squad = squads.Get(squadEntity);
                if (squad.Team != playerTeam)
                {
                    continue;
                }

                var order = orders.Get(squadEntity).Value;
                friendlySquadCount++;
                if (!hasPrimaryOrder)
                {
                    hasPrimaryOrder = true;
                    primaryOrder = order;
                }
                else if (order != primaryOrder)
                {
                    mixedOrderCount++;
                }
            }

            return new BattleCommandHudSnapshot(
                true,
                friendlySquadCount > 0,
                primaryOrder,
                friendlySquadCount,
                mixedOrderCount);
        }

        public void Dispose()
        {
            DisposeFilters();
            world = null;
            teams = null;
            squads = null;
            orders = null;
        }

        private bool TryGetPlayerTeam(out BattleTeamType team)
        {
            foreach (var player in playerFilter)
            {
                team = teams.Get(player).Value;
                return true;
            }

            team = BattleTeamType.Attacker;
            return false;
        }

        private void DisposeFilters()
        {
            if (world != null && !world.IsDisposed)
            {
                playerFilter?.Dispose();
                squadFilter?.Dispose();
            }

            playerFilter = null;
            squadFilter = null;
        }
    }
}
