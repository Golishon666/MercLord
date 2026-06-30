using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.Input;
using MercLord.Game.Configs;
using MercLord.Game.Save;
using MercLord.Player.Inventory;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class PlayerInputSystem : IBattleRuntimeSystem
    {
        private readonly IBattleInputSource inputSource;
        private readonly ConfigDatabase configDatabase;
        private readonly ISaveService saveService;
        private readonly IBattleEntityFactory entityFactory;
        private readonly SpatialHashSystem spatialHashSystem;
        private readonly List<Entity> playerBuffer = new List<Entity>();
        private readonly List<Entity> candidateBuffer = new List<Entity>();

        private World world;
        private Filter filter;
        private Stash<PlayerInputComponent> playerInputs;
        private Stash<PositionComponent> positions;
        private Stash<VelocityComponent> velocities;
        private Stash<TeamComponent> teams;
        private Stash<WeaponStatsComponent> weapons;
        private Stash<AttackCooldownComponent> cooldowns;
        private Stash<AttackRequestComponent> attackRequests;
        private Stash<TargetComponent> targets;
        private Stash<HealthComponent> healths;

        public PlayerInputSystem(
            IBattleInputSource inputSource,
            ConfigDatabase configDatabase,
            ISaveService saveService,
            IBattleEntityFactory entityFactory,
            SpatialHashSystem spatialHashSystem)
        {
            this.inputSource = inputSource ?? throw new ArgumentNullException(nameof(inputSource));
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
            this.saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            this.entityFactory = entityFactory ?? throw new ArgumentNullException(nameof(entityFactory));
            this.spatialHashSystem = spatialHashSystem ?? throw new ArgumentNullException(nameof(spatialHashSystem));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("PlayerInputSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("PlayerInputSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<PlayerControlledComponent>()
                .With<PlayerInputComponent>()
                .With<PositionComponent>()
                .With<VelocityComponent>()
                .With<TeamComponent>()
                .With<WeaponStatsComponent>()
                .With<AttackCooldownComponent>()
                .Without<DeadComponent>()
                .Without<VehicleComponent>()
                .Build();

            playerInputs = world.GetStash<PlayerInputComponent>();
            positions = world.GetStash<PositionComponent>();
            velocities = world.GetStash<VelocityComponent>();
            teams = world.GetStash<TeamComponent>();
            weapons = world.GetStash<WeaponStatsComponent>();
            cooldowns = world.GetStash<AttackCooldownComponent>();
            attackRequests = world.GetStash<AttackRequestComponent>();
            targets = world.GetStash<TargetComponent>();
            healths = world.GetStash<HealthComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            var snapshot = inputSource.Snapshot;
            playerBuffer.Clear();
            foreach (var entity in filter)
            {
                playerBuffer.Add(entity);
            }

            for (var playerIndex = 0; playerIndex < playerBuffer.Count; playerIndex++)
            {
                ApplyInput(playerBuffer[playerIndex], snapshot);
            }

            playerBuffer.Clear();
            candidateBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            playerBuffer.Clear();
            candidateBuffer.Clear();
            filter = null;
            world = null;
            playerInputs = null;
            positions = null;
            velocities = null;
            teams = null;
            weapons = null;
            cooldowns = null;
            attackRequests = null;
            targets = null;
            healths = null;
        }

        private void ApplyInput(Entity player, BattleInputSnapshot snapshot)
        {
            ref var playerInput = ref playerInputs.Get(player);
            playerInput = new PlayerInputComponent
            {
                MoveDirection = snapshot.MoveDirection,
                AimDirection = snapshot.AimDirection,
                FirePressed = snapshot.FirePressed,
                InteractPressed = snapshot.InteractPressed,
                SelectedWeaponSlot = snapshot.SelectedWeaponSlot
            };

            velocities.Get(player).Value = snapshot.MoveDirection;

            var hasEquippedWeapon = TryResolveEquippedWeapon(snapshot.SelectedWeaponSlot, out var weaponConfig);
            if (hasEquippedWeapon)
            {
                weapons.Get(player) = entityFactory.CreateWeaponStats(weaponConfig);
            }

            if (!snapshot.FirePressed ||
                !hasEquippedWeapon ||
                cooldowns.Get(player).Value > 0f ||
                !BattleAimTargeting.TryFindAimedTarget(
                    world,
                    spatialHashSystem,
                    configDatabase,
                    positions,
                    teams,
                    healths,
                    player,
                    snapshot.AimDirection,
                    weaponConfig.Range,
                    candidateBuffer,
                    out var target))
            {
                return;
            }

            targets.Set(player, new TargetComponent { Target = target });
            var requestEntity = world.CreateEntity();
            attackRequests.Set(requestEntity, new AttackRequestComponent
            {
                Source = player,
                Target = target,
                WeaponConfigId = weaponConfig.Id
            });
        }

        private bool TryResolveEquippedWeapon(int selectedWeaponSlot, out WeaponConfig weaponConfig)
        {
            weaponConfig = null;

            var equipment = saveService.Current?.Equipment;
            if (equipment == null ||
                !equipment.TryGetWeaponSlot(selectedWeaponSlot, out var item) ||
                !IsEquipped(item) ||
                !configDatabase.TryGetItem(item.ConfigId, out var itemConfig) ||
                itemConfig.Category != ItemCategory.Weapon ||
                itemConfig.Weapon == null)
            {
                return false;
            }

            weaponConfig = itemConfig.Weapon;
            return true;
        }

        private static bool IsEquipped(ItemInstance item)
        {
            return item.Amount > 0;
        }
    }
}
