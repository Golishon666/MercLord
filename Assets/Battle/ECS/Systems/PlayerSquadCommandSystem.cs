using System;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.Input;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class PlayerSquadCommandSystem : IBattleRuntimeSystem
    {
        private const float RetreatDistance = 12f;

        private readonly IBattleInputSource inputSource;

        private World world;
        private Filter playerFilter;
        private Filter squadFilter;
        private Stash<TeamComponent> teams;
        private Stash<PositionComponent> positions;
        private Stash<SquadComponent> squads;
        private Stash<SquadAnchorComponent> anchors;
        private Stash<SquadOrderComponent> orders;
        private BattleModel model;

        public PlayerSquadCommandSystem(IBattleInputSource inputSource)
        {
            this.inputSource = inputSource ?? throw new ArgumentNullException(nameof(inputSource));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("PlayerSquadCommandSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("PlayerSquadCommandSystem cannot initialize on a disposed Morpeh world.");
            }

            model = session.Model;
            playerFilter = world.Filter
                .With<PlayerControlledComponent>()
                .With<TeamComponent>()
                .With<PositionComponent>()
                .Without<DeadComponent>()
                .Build();
            squadFilter = world.Filter
                .With<SquadComponent>()
                .With<SquadAnchorComponent>()
                .With<SquadOrderComponent>()
                .Build();

            teams = world.GetStash<TeamComponent>();
            positions = world.GetStash<PositionComponent>();
            squads = world.GetStash<SquadComponent>();
            anchors = world.GetStash<SquadAnchorComponent>();
            orders = world.GetStash<SquadOrderComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || playerFilter == null || squadFilter == null)
            {
                return;
            }

            var snapshot = inputSource.Snapshot;
            if (!snapshot.SquadOrderPressed || !TryGetPlayerContext(out var playerTeam, out var playerPosition))
            {
                return;
            }

            foreach (var squadEntity in squadFilter)
            {
                var squad = squads.Get(squadEntity);
                if (squad.Team != playerTeam)
                {
                    continue;
                }

                var anchor = anchors.Get(squadEntity);
                ref var order = ref orders.Get(squadEntity);
                order = new SquadOrderComponent
                {
                    Value = snapshot.SquadOrder,
                    TargetPosition = ResolveTargetPosition(snapshot.SquadOrder, playerTeam, playerPosition, anchor, order)
                };
            }
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed)
            {
                playerFilter?.Dispose();
                squadFilter?.Dispose();
            }

            world = null;
            playerFilter = null;
            squadFilter = null;
            teams = null;
            positions = null;
            squads = null;
            anchors = null;
            orders = null;
            model = null;
        }

        private bool TryGetPlayerContext(out BattleTeamType team, out float2 position)
        {
            foreach (var player in playerFilter)
            {
                team = teams.Get(player).Value;
                position = positions.Get(player).Value;
                return true;
            }

            team = BattleTeamType.Attacker;
            position = float2.zero;
            return false;
        }

        private float2 ResolveTargetPosition(
            SquadOrderType order,
            BattleTeamType playerTeam,
            float2 playerPosition,
            SquadAnchorComponent anchor,
            SquadOrderComponent currentOrder)
        {
            switch (order)
            {
                case SquadOrderType.FollowPlayer:
                    return playerPosition;
                case SquadOrderType.HoldPosition:
                    return anchor.Position;
                case SquadOrderType.Retreat:
                    return anchor.Position + ResolveRetreatDirection(playerTeam, playerPosition, anchor.Position) * RetreatDistance;
                case SquadOrderType.AttackNearest:
                    return BattleObjectiveResolver.TryResolvePrimaryControlPointTarget(model, out var targetPosition)
                        ? targetPosition
                        : currentOrder.TargetPosition;
                default:
                    return currentOrder.TargetPosition;
            }
        }

        private static float2 ResolveRetreatDirection(
            BattleTeamType playerTeam,
            float2 playerPosition,
            float2 anchorPosition)
        {
            var awayFromPlayer = anchorPosition - playerPosition;
            if (math.lengthsq(awayFromPlayer) > 0.0001f)
            {
                return math.normalizesafe(awayFromPlayer);
            }

            return playerTeam == BattleTeamType.Attacker
                ? new float2(-1f, 0f)
                : new float2(1f, 0f);
        }
    }
}
