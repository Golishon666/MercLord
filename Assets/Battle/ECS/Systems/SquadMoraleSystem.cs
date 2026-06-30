using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class SquadMoraleSystem : IBattleRuntimeSystem
    {
        private const float TickInterval = 1f;
        private const float RetreatEdgeOffset = 0.5f;

        private readonly Dictionary<int, int> aliveBySquadId = new Dictionary<int, int>();

        private World world;
        private BattleModel model;
        private Filter squadFilter;
        private Filter memberFilter;
        private Stash<SquadComponent> squads;
        private Stash<SquadAnchorComponent> anchors;
        private Stash<SquadOrderComponent> orders;
        private Stash<SquadMoraleComponent> morale;
        private Stash<SquadMemberComponent> members;
        private Stash<HealthComponent> health;
        private Stash<DeadComponent> dead;
        private BattleCadenceTimer tickTimer;

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("SquadMoraleSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("SquadMoraleSystem cannot initialize on a disposed Morpeh world.");
            }

            model = session.Model ?? throw new InvalidOperationException("SquadMoraleSystem requires a BattleModel.");
            squadFilter = world.Filter
                .With<SquadComponent>()
                .With<SquadAnchorComponent>()
                .With<SquadOrderComponent>()
                .With<SquadMoraleComponent>()
                .Build();
            memberFilter = world.Filter
                .With<SquadMemberComponent>()
                .With<HealthComponent>()
                .Build();

            squads = world.GetStash<SquadComponent>();
            anchors = world.GetStash<SquadAnchorComponent>();
            orders = world.GetStash<SquadOrderComponent>();
            morale = world.GetStash<SquadMoraleComponent>();
            members = world.GetStash<SquadMemberComponent>();
            health = world.GetStash<HealthComponent>();
            dead = world.GetStash<DeadComponent>();
            tickTimer = new BattleCadenceTimer(TickInterval);
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || squadFilter == null || memberFilter == null)
            {
                return;
            }

            if (!tickTimer.Consume(deltaTime))
            {
                return;
            }

            CountAliveMembers();
            foreach (var entity in squadFilter)
            {
                RefreshSquadMorale(entity);
            }
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed)
            {
                squadFilter?.Dispose();
                memberFilter?.Dispose();
            }

            aliveBySquadId.Clear();
            world = null;
            model = null;
            squadFilter = null;
            memberFilter = null;
            squads = null;
            anchors = null;
            orders = null;
            morale = null;
            members = null;
            health = null;
            dead = null;
            tickTimer = default;
        }

        private void CountAliveMembers()
        {
            aliveBySquadId.Clear();
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

        private void RefreshSquadMorale(Entity entity)
        {
            var squad = squads.Get(entity);
            ref var squadMorale = ref morale.Get(entity);
            var maxMorale = math.max(1f, squadMorale.Max);
            var threshold = math.clamp(squadMorale.RoutThreshold, 0f, maxMorale);
            var totalMembers = math.max(1, squad.MemberCount);
            aliveBySquadId.TryGetValue(squad.SquadId, out var aliveMembers);

            var survivalRatio = math.clamp((float)aliveMembers / totalMembers, 0f, 1f);
            squadMorale.Max = maxMorale;
            squadMorale.RoutThreshold = threshold;
            squadMorale.Current = math.clamp(maxMorale * survivalRatio, 0f, maxMorale);
            squadMorale.IsRouted = aliveMembers > 0 && squadMorale.Current <= threshold;

            if (!squadMorale.IsRouted)
            {
                return;
            }

            ref var order = ref orders.Get(entity);
            order.Value = SquadOrderType.Retreat;
            order.TargetPosition = ResolveRetreatTarget(squad.Team, anchors.Get(entity).Position);
        }

        private float2 ResolveRetreatTarget(BattleTeamType team, float2 anchorPosition)
        {
            var width = math.max(1, model.Width);
            var height = math.max(1, model.Height);
            var x = team == BattleTeamType.Attacker
                ? RetreatEdgeOffset
                : width - RetreatEdgeOffset;
            var y = math.clamp(anchorPosition.y, RetreatEdgeOffset, height - RetreatEdgeOffset);
            return new float2(x, y);
        }

        private bool IsAlive(Entity entity)
        {
            if (dead.Has(entity))
            {
                return false;
            }

            return health.Get(entity).Current > 0;
        }
    }
}
