using MercLord.Battle.ECS.Components;
using MercLord.Battle.Input;
using MercLord.Player.Equipment;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class BattleInputMapperTests
    {
        [Test]
        public void MapNormalizesMovementAndAim()
        {
            var snapshot = BattleInputMapper.Map(new BattleInputRuntimeState(
                horizontal: 1f,
                vertical: 1f,
                mousePosition: new Vector2(200f, 100f),
                screenCenter: new Vector2(100f, 100f),
                firePressed: false,
                interactPressed: false,
                selectedWeaponSlot: 0,
                followPressed: false,
                holdPressed: false,
                attackPressed: false,
                retreatPressed: false,
                pointerOverUi: false));

            Assert.AreEqual(1f, math.length(snapshot.MoveDirection), 0.0001f);
            Assert.AreEqual(1f, snapshot.AimDirection.x, 0.0001f);
            Assert.AreEqual(0f, snapshot.AimDirection.y, 0.0001f);
        }

        [Test]
        public void MapSuppressesFireWhenPointerIsOverUi()
        {
            var snapshot = BattleInputMapper.Map(new BattleInputRuntimeState(
                horizontal: 0f,
                vertical: 0f,
                mousePosition: Vector2.zero,
                screenCenter: Vector2.zero,
                firePressed: true,
                interactPressed: false,
                selectedWeaponSlot: 0,
                followPressed: false,
                holdPressed: false,
                attackPressed: false,
                retreatPressed: false,
                pointerOverUi: true));

            Assert.IsFalse(snapshot.FirePressed);
        }

        [Test]
        public void MapResolvesSquadCommandsByPriority()
        {
            var follow = MapCommand(follow: true);
            var hold = MapCommand(hold: true);
            var attack = MapCommand(attack: true);
            var retreat = MapCommand(retreat: true);
            var priority = MapCommand(follow: true, retreat: true);

            Assert.IsTrue(follow.SquadOrderPressed);
            Assert.AreEqual(SquadOrderType.FollowPlayer, follow.SquadOrder);
            Assert.AreEqual(SquadOrderType.HoldPosition, hold.SquadOrder);
            Assert.AreEqual(SquadOrderType.AttackNearest, attack.SquadOrder);
            Assert.AreEqual(SquadOrderType.Retreat, retreat.SquadOrder);
            Assert.AreEqual(SquadOrderType.FollowPlayer, priority.SquadOrder);
        }

        [Test]
        public void MapClampsSelectedWeaponSlot()
        {
            var snapshot = BattleInputMapper.Map(new BattleInputRuntimeState(
                horizontal: 0f,
                vertical: 0f,
                mousePosition: Vector2.zero,
                screenCenter: Vector2.zero,
                firePressed: false,
                interactPressed: false,
                selectedWeaponSlot: PlayerEquipment.WeaponSlotCount + 3,
                followPressed: false,
                holdPressed: false,
                attackPressed: false,
                retreatPressed: false,
                pointerOverUi: false));

            Assert.AreEqual(PlayerEquipment.WeaponSlotCount - 1, snapshot.SelectedWeaponSlot);
        }

        private static BattleInputSnapshot MapCommand(
            bool follow = false,
            bool hold = false,
            bool attack = false,
            bool retreat = false)
        {
            return BattleInputMapper.Map(new BattleInputRuntimeState(
                horizontal: 0f,
                vertical: 0f,
                mousePosition: Vector2.zero,
                screenCenter: Vector2.zero,
                firePressed: false,
                interactPressed: false,
                selectedWeaponSlot: 0,
                followPressed: follow,
                holdPressed: hold,
                attackPressed: attack,
                retreatPressed: retreat,
                pointerOverUi: false));
        }
    }
}
