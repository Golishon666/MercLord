using MercLord.Battle.ECS.Components;
using MercLord.Player.Equipment;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MercLord.Battle.Input
{
    public readonly struct BattleInputRuntimeState
    {
        public BattleInputRuntimeState(
            float horizontal,
            float vertical,
            Vector2 mousePosition,
            Vector2 screenCenter,
            bool firePressed,
            bool interactPressed,
            int selectedWeaponSlot,
            bool followPressed,
            bool holdPressed,
            bool attackPressed,
            bool retreatPressed,
            bool pointerOverUi)
        {
            Horizontal = horizontal;
            Vertical = vertical;
            MousePosition = mousePosition;
            ScreenCenter = screenCenter;
            FirePressed = firePressed;
            InteractPressed = interactPressed;
            SelectedWeaponSlot = selectedWeaponSlot;
            FollowPressed = followPressed;
            HoldPressed = holdPressed;
            AttackPressed = attackPressed;
            RetreatPressed = retreatPressed;
            PointerOverUi = pointerOverUi;
        }

        public float Horizontal { get; }
        public float Vertical { get; }
        public Vector2 MousePosition { get; }
        public Vector2 ScreenCenter { get; }
        public bool FirePressed { get; }
        public bool InteractPressed { get; }
        public int SelectedWeaponSlot { get; }
        public bool FollowPressed { get; }
        public bool HoldPressed { get; }
        public bool AttackPressed { get; }
        public bool RetreatPressed { get; }
        public bool PointerOverUi { get; }
    }

    public static class BattleInputMapper
    {
        public static BattleInputSnapshot Map(BattleInputRuntimeState state)
        {
            var commandPressed = TryResolveSquadOrder(state, out var squadOrder);
            return new BattleInputSnapshot(
                new float2(state.Horizontal, state.Vertical),
                ResolveAimDirection(state),
                state.FirePressed && !state.PointerOverUi,
                state.InteractPressed,
                state.SelectedWeaponSlot,
                commandPressed,
                squadOrder);
        }

        private static float2 ResolveAimDirection(BattleInputRuntimeState state)
        {
            var delta = state.MousePosition - state.ScreenCenter;
            if (delta.sqrMagnitude <= 0.0001f)
            {
                return float2.zero;
            }

            delta.Normalize();
            return new float2(delta.x, delta.y);
        }

        private static bool TryResolveSquadOrder(BattleInputRuntimeState state, out SquadOrderType order)
        {
            if (state.FollowPressed)
            {
                order = SquadOrderType.FollowPlayer;
                return true;
            }

            if (state.HoldPressed)
            {
                order = SquadOrderType.HoldPosition;
                return true;
            }

            if (state.AttackPressed)
            {
                order = SquadOrderType.AttackNearest;
                return true;
            }

            if (state.RetreatPressed)
            {
                order = SquadOrderType.Retreat;
                return true;
            }

            order = SquadOrderType.AttackNearest;
            return false;
        }
    }

    public readonly struct BattleInputSnapshot
    {
        public BattleInputSnapshot(
            float2 moveDirection,
            float2 aimDirection,
            bool firePressed,
            bool interactPressed,
            int selectedWeaponSlot,
            bool squadOrderPressed = false,
            SquadOrderType squadOrder = SquadOrderType.AttackNearest)
        {
            MoveDirection = ClampDirection(moveDirection);
            AimDirection = ClampDirection(aimDirection);
            FirePressed = firePressed;
            InteractPressed = interactPressed;
            SelectedWeaponSlot = math.clamp(selectedWeaponSlot, 0, PlayerEquipment.WeaponSlotCount - 1);
            SquadOrderPressed = squadOrderPressed;
            SquadOrder = squadOrder;
        }

        public float2 MoveDirection { get; }
        public float2 AimDirection { get; }
        public bool FirePressed { get; }
        public bool InteractPressed { get; }
        public int SelectedWeaponSlot { get; }
        public bool SquadOrderPressed { get; }
        public SquadOrderType SquadOrder { get; }

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
        [Header("Runtime Polling")]
        [SerializeField] private bool pollUnityInput = true;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode followCommandKey = KeyCode.F1;
        [SerializeField] private KeyCode holdCommandKey = KeyCode.F2;
        [SerializeField] private KeyCode attackCommandKey = KeyCode.F3;
        [SerializeField] private KeyCode retreatCommandKey = KeyCode.F4;

        [SerializeField] private Vector2 moveDirection;
        [SerializeField] private Vector2 aimDirection;
        [SerializeField] private bool firePressed;
        [SerializeField] private bool interactPressed;
        [SerializeField] private int selectedWeaponSlot;
        [SerializeField] private bool squadOrderPressed;
        [SerializeField] private SquadOrderType squadOrder = SquadOrderType.AttackNearest;

        public BattleInputSnapshot Snapshot => new BattleInputSnapshot(
            new float2(moveDirection.x, moveDirection.y),
            new float2(aimDirection.x, aimDirection.y),
            firePressed,
            interactPressed,
            selectedWeaponSlot,
            squadOrderPressed,
            squadOrder);

        public bool PollUnityInput
        {
            get => pollUnityInput;
            set => pollUnityInput = value;
        }

        public void ApplySnapshot(BattleInputSnapshot snapshot)
        {
            moveDirection = new Vector2(snapshot.MoveDirection.x, snapshot.MoveDirection.y);
            aimDirection = new Vector2(snapshot.AimDirection.x, snapshot.AimDirection.y);
            firePressed = snapshot.FirePressed;
            interactPressed = snapshot.InteractPressed;
            selectedWeaponSlot = snapshot.SelectedWeaponSlot;
            squadOrderPressed = snapshot.SquadOrderPressed;
            squadOrder = snapshot.SquadOrder;
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

        public void SetSquadOrderPressed(bool value)
        {
            squadOrderPressed = value;
        }

        public void SetSquadOrder(SquadOrderType value)
        {
            squadOrder = value;
        }

        private void Update()
        {
            if (!pollUnityInput || !Application.isPlaying)
            {
                return;
            }

            ApplySnapshot(BattleInputMapper.Map(CreateRuntimeInputState()));
        }

        private BattleInputRuntimeState CreateRuntimeInputState()
        {
            var selectedSlot = selectedWeaponSlot;
            for (var slotIndex = 0; slotIndex < PlayerEquipment.WeaponSlotCount; slotIndex++)
            {
                var key = (KeyCode)((int)KeyCode.Alpha1 + slotIndex);
                if (UnityEngine.Input.GetKeyDown(key))
                {
                    selectedSlot = slotIndex;
                }
            }

            return new BattleInputRuntimeState(
                GetAxis(KeyCode.D, KeyCode.RightArrow, KeyCode.A, KeyCode.LeftArrow),
                GetAxis(KeyCode.W, KeyCode.UpArrow, KeyCode.S, KeyCode.DownArrow),
                UnityEngine.Input.mousePosition,
                new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
                UnityEngine.Input.GetMouseButton(0),
                UnityEngine.Input.GetKeyDown(interactKey),
                selectedSlot,
                UnityEngine.Input.GetKeyDown(followCommandKey),
                UnityEngine.Input.GetKeyDown(holdCommandKey),
                UnityEngine.Input.GetKeyDown(attackCommandKey),
                UnityEngine.Input.GetKeyDown(retreatCommandKey),
                EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
        }

        private static float GetAxis(KeyCode positivePrimary, KeyCode positiveSecondary, KeyCode negativePrimary, KeyCode negativeSecondary)
        {
            var value = 0f;
            if (UnityEngine.Input.GetKey(positivePrimary) || UnityEngine.Input.GetKey(positiveSecondary))
            {
                value += 1f;
            }

            if (UnityEngine.Input.GetKey(negativePrimary) || UnityEngine.Input.GetKey(negativeSecondary))
            {
                value -= 1f;
            }

            return value;
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
