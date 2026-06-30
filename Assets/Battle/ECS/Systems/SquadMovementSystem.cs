using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.Pathfinding;
using MercLord.Battle.Tiles;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class SquadMovementSystem : IBattleRuntimeSystem
    {
        private const float SlotDeadZoneSquared = 0.04f;
        private const float FlowWeight = 0.72f;
        private const float FormationWeight = 0.28f;

        private readonly BattleFlowFieldBuilder flowFieldBuilder = new BattleFlowFieldBuilder();
        private readonly Dictionary<int, SquadRuntimeState> squadStates = new Dictionary<int, SquadRuntimeState>();
        private readonly Dictionary<int, float2> squadPositionSums = new Dictionary<int, float2>();
        private readonly Dictionary<int, int> squadMemberCounts = new Dictionary<int, int>();
        private readonly List<Entity> memberBuffer = new List<Entity>();

        private World world;
        private Filter squadFilter;
        private Filter memberFilter;
        private Stash<SquadComponent> squads;
        private Stash<SquadAnchorComponent> anchors;
        private Stash<SquadOrderComponent> orders;
        private Stash<SquadMemberComponent> squadMembers;
        private Stash<FormationSlotComponent> formationSlots;
        private Stash<PositionComponent> positions;
        private Stash<VelocityComponent> velocities;
        private Stash<TargetComponent> targets;
        private BattleTileMap tileMap;

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("SquadMovementSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("SquadMovementSystem cannot initialize on a disposed Morpeh world.");
            }

            if (session.Model == null ||
                session.Model.Tiles == null ||
                session.Model.Tiles.Length != session.Model.Width * session.Model.Height)
            {
                throw new InvalidOperationException("SquadMovementSystem requires a valid BattleModel tile grid.");
            }

            tileMap = new BattleTileMap(session.Model.Width, session.Model.Height, session.Model.Tiles);
            squadFilter = world.Filter
                .With<SquadComponent>()
                .With<SquadAnchorComponent>()
                .With<SquadOrderComponent>()
                .Build();
            memberFilter = world.Filter
                .With<SquadMemberComponent>()
                .With<FormationSlotComponent>()
                .With<PositionComponent>()
                .With<VelocityComponent>()
                .Without<DeadComponent>()
                .Without<PlayerControlledComponent>()
                .Build();

            squads = world.GetStash<SquadComponent>();
            anchors = world.GetStash<SquadAnchorComponent>();
            orders = world.GetStash<SquadOrderComponent>();
            squadMembers = world.GetStash<SquadMemberComponent>();
            formationSlots = world.GetStash<FormationSlotComponent>();
            positions = world.GetStash<PositionComponent>();
            velocities = world.GetStash<VelocityComponent>();
            targets = world.GetStash<TargetComponent>();
            RebuildSquadStates();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || squadFilter == null || memberFilter == null)
            {
                return;
            }

            RebuildSquadStates();
            RefreshAnchorsFromMembers();
            memberBuffer.Clear();
            foreach (var entity in memberFilter)
            {
                memberBuffer.Add(entity);
            }

            for (var memberIndex = 0; memberIndex < memberBuffer.Count; memberIndex++)
            {
                TickMember(memberBuffer[memberIndex]);
            }

            memberBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed)
            {
                squadFilter?.Dispose();
                memberFilter?.Dispose();
            }

            squadStates.Clear();
            squadPositionSums.Clear();
            squadMemberCounts.Clear();
            memberBuffer.Clear();
            world = null;
            squadFilter = null;
            memberFilter = null;
            squads = null;
            anchors = null;
            orders = null;
            squadMembers = null;
            formationSlots = null;
            positions = null;
            velocities = null;
            targets = null;
            tileMap = null;
        }

        private void RebuildSquadStates()
        {
            foreach (var entity in squadFilter)
            {
                var squad = squads.Get(entity);
                var order = orders.Get(entity);
                var anchor = anchors.Get(entity);
                var targetCell = ResolveTargetCell(order.TargetPosition);
                if (!squadStates.TryGetValue(squad.SquadId, out var state))
                {
                    state = new SquadRuntimeState();
                    squadStates.Add(squad.SquadId, state);
                }

                state.Order = order;
                state.Anchor = anchor;
                if (state.FlowField == null ||
                    state.TargetCell.x != targetCell.x ||
                    state.TargetCell.y != targetCell.y)
                {
                    state.TargetCell = targetCell;
                    state.FlowField = BuildFlowField(targetCell);
                }
            }
        }

        private void RefreshAnchorsFromMembers()
        {
            squadPositionSums.Clear();
            squadMemberCounts.Clear();
            foreach (var entity in memberFilter)
            {
                var member = squadMembers.Get(entity);
                var position = positions.Get(entity).Value;
                squadPositionSums.TryGetValue(member.SquadId, out var sum);
                squadMemberCounts.TryGetValue(member.SquadId, out var count);
                squadPositionSums[member.SquadId] = sum + position;
                squadMemberCounts[member.SquadId] = count + 1;
            }

            foreach (var entity in squadFilter)
            {
                ref var anchor = ref anchors.Get(entity);
                var squad = squads.Get(entity);
                if (squadMemberCounts.TryGetValue(squad.SquadId, out var count) && count > 0)
                {
                    anchor.Position = squadPositionSums[squad.SquadId] / count;
                }

                if (squadStates.TryGetValue(squad.SquadId, out var state))
                {
                    state.Anchor = anchor;
                }
            }
        }

        private void TickMember(Entity entity)
        {
            var member = squadMembers.Get(entity);
            if (!squadStates.TryGetValue(member.SquadId, out var state))
            {
                return;
            }

            ref var velocity = ref velocities.Get(entity);
            var position = positions.Get(entity).Value;
            var order = state.Order;
            var hasCombatTarget = targets.Has(entity);
            var hasCombatVelocity = math.lengthsq(velocity.Value) > 0.0001f;
            var flowDirection = float2.zero;
            var useFlow = order.Value != SquadOrderType.HoldPosition &&
                          (!hasCombatTarget || hasCombatVelocity) &&
                          TryGetFlowDirection(state, position, out flowDirection);

            var slot = formationSlots.Get(entity);
            var slotDirection = float2.zero;
            var slotOffset = (state.Anchor.Position + slot.LocalOffset) - position;
            var useSlot = math.lengthsq(slotOffset) > SlotDeadZoneSquared;
            if (useSlot)
            {
                slotDirection = math.normalizesafe(slotOffset);
            }

            var desired = float2.zero;
            if (useFlow)
            {
                desired += flowDirection * FlowWeight;
            }
            else if (hasCombatVelocity)
            {
                desired += math.normalizesafe(velocity.Value) * FlowWeight;
            }

            if (useSlot)
            {
                desired += slotDirection * FormationWeight;
            }

            if (math.lengthsq(desired) > 0.0001f)
            {
                velocity.Value = math.normalizesafe(desired);
            }
            else if (!hasCombatTarget)
            {
                velocity.Value = float2.zero;
            }
        }

        private bool TryGetFlowDirection(
            SquadRuntimeState state,
            float2 position,
            out float2 direction)
        {
            direction = default;
            if (state.FlowField == null)
            {
                return false;
            }

            if (state.FlowField.TryGetDirection(position, out direction) &&
                math.lengthsq(direction) > 0.0001f)
            {
                return true;
            }

            var toTarget = new float2(state.TargetCell.x + 0.5f, state.TargetCell.y + 0.5f) - position;
            if (math.lengthsq(toTarget) <= 0.0001f)
            {
                return false;
            }

            direction = math.normalizesafe(toTarget);
            return true;
        }

        private BattleFlowField BuildFlowField(int2 targetCell)
        {
            if (!TryResolveEnterableTarget(targetCell, out var resolvedTarget))
            {
                return null;
            }

            return flowFieldBuilder.Build(tileMap, resolvedTarget.x, resolvedTarget.y, MoveLayer.Infantry);
        }

        private int2 ResolveTargetCell(float2 targetPosition)
        {
            return new int2(
                math.clamp((int)math.floor(targetPosition.x), 0, tileMap.Width - 1),
                math.clamp((int)math.floor(targetPosition.y), 0, tileMap.Height - 1));
        }

        private bool TryResolveEnterableTarget(int2 targetCell, out int2 resolvedTarget)
        {
            if (IsEnterable(targetCell.x, targetCell.y))
            {
                resolvedTarget = targetCell;
                return true;
            }

            var maxRadius = math.max(tileMap.Width, tileMap.Height);
            for (var radius = 1; radius <= maxRadius; radius++)
            {
                for (var y = targetCell.y - radius; y <= targetCell.y + radius; y++)
                {
                    for (var x = targetCell.x - radius; x <= targetCell.x + radius; x++)
                    {
                        if (math.abs(x - targetCell.x) != radius &&
                            math.abs(y - targetCell.y) != radius)
                        {
                            continue;
                        }

                        if (IsEnterable(x, y))
                        {
                            resolvedTarget = new int2(x, y);
                            return true;
                        }
                    }
                }
            }

            resolvedTarget = default;
            return false;
        }

        private bool IsEnterable(int x, int y)
        {
            if (!tileMap.IsInside(x, y))
            {
                return false;
            }

            var tile = tileMap.GetTile(x, y);
            return tile.Walkable &&
                   (tile.AllowedMoveLayers & MoveLayer.Infantry) != 0;
        }

        private sealed class SquadRuntimeState
        {
            public SquadAnchorComponent Anchor;
            public SquadOrderComponent Order;
            public int2 TargetCell;
            public BattleFlowField FlowField;
        }
    }
}
