using MercLord.Player.Equipment;
using Unity.Mathematics;
using UnityEngine;

namespace MercLord.Battle.Input
{
    public readonly struct BattleInputSnapshot
    {
        public BattleInputSnapshot(
            float2 moveDirection,
            float2 aimDirection,
            bool firePressed,
            bool interactPressed,
            int selectedWeaponSlot)
        {
            MoveDirection = ClampDirection(moveDirection);
            AimDirection = ClampDirection(aimDirection);
            FirePressed = firePressed;
            InteractPressed = interactPressed;
            SelectedWeaponSlot = math.clamp(selectedWeaponSlot, 0, PlayerEquipment.WeaponSlotCount - 1);
        }

        public float2 MoveDirection { get; }
        public float2 AimDirection { get; }
        public bool FirePressed { get; }
        public bool InteractPressed { get; }
        public int SelectedWeaponSlot { get; }

        private static float2 ClampDirection(float2 direction)
        {
            return math.lengthsq(direction) > 1f
                ? math.normalizesafe(direction)
                : direction;
        }
    }

    public interface IBattleInputSource
    {
        BattleInputSnapshot Snapshot { get; }
    }

    public sealed class BattleInputSource : MonoBehaviour, IBattleInputSource
    {
        [SerializeField] private Vector2 moveDirection;
        [SerializeField] private Vector2 aimDirection;
        [SerializeField] private bool firePressed;
        [SerializeField] private bool interactPressed;
        [SerializeField] private int selectedWeaponSlot;

        public BattleInputSnapshot Snapshot => new BattleInputSnapshot(
            new float2(moveDirection.x, moveDirection.y),
            new float2(aimDirection.x, aimDirection.y),
            firePressed,
            interactPressed,
            selectedWeaponSlot);

        public void ApplySnapshot(BattleInputSnapshot snapshot)
        {
            moveDirection = new Vector2(snapshot.MoveDirection.x, snapshot.MoveDirection.y);
            aimDirection = new Vector2(snapshot.AimDirection.x, snapshot.AimDirection.y);
            firePressed = snapshot.FirePressed;
            interactPressed = snapshot.InteractPressed;
            selectedWeaponSlot = snapshot.SelectedWeaponSlot;
        }

        public void SetMoveDirection(Vector2 value)
        {
            moveDirection = value;
        }

        public void SetAimDirection(Vector2 value)
        {
            aimDirection = value;
        }

        public void SetFirePressed(bool value)
        {
            firePressed = value;
        }

        public void SetInteractPressed(bool value)
        {
            interactPressed = value;
        }

        public void SetSelectedWeaponSlot(int value)
        {
            selectedWeaponSlot = Mathf.Clamp(value, 0, PlayerEquipment.WeaponSlotCount - 1);
        }

        private void OnValidate()
        {
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            if (aimDirection.sqrMagnitude > 1f)
            {
                aimDirection.Normalize();
            }

            selectedWeaponSlot = Mathf.Clamp(selectedWeaponSlot, 0, PlayerEquipment.WeaponSlotCount - 1);
        }
    }
}
