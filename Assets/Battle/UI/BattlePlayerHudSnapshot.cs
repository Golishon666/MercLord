using System;
using MercLord.Battle.ECS.Components;
using MercLord.Game.Configs;

namespace MercLord.Battle.UI
{
    public readonly struct BattlePlayerHudSnapshot
    {
        public BattlePlayerHudSnapshot(
            bool hasPlayer,
            bool isVehicle,
            int controlledConfigId,
            int currentHealth,
            int maxHealth,
            bool hasWeapon,
            int weaponConfigId,
            WeaponType weaponType,
            int selectedWeaponSlot,
            float cooldownRemaining,
            float cooldownDuration,
            int ballisticArmor,
            int energyArmor,
            int explosionArmor,
            bool firePressed,
            bool interactPressed,
            bool hasTarget,
            bool targetIsVehicle,
            int targetConfigId,
            int targetCurrentHealth,
            int targetMaxHealth)
        {
            HasPlayer = hasPlayer;
            IsVehicle = isVehicle;
            ControlledConfigId = controlledConfigId;
            CurrentHealth = Math.Max(0, currentHealth);
            MaxHealth = Math.Max(0, maxHealth);
            HasWeapon = hasWeapon;
            WeaponConfigId = weaponConfigId;
            WeaponType = weaponType;
            SelectedWeaponSlot = Math.Max(0, selectedWeaponSlot);
            CooldownRemaining = Math.Max(0f, cooldownRemaining);
            CooldownDuration = Math.Max(0f, cooldownDuration);
            BallisticArmor = Math.Max(0, ballisticArmor);
            EnergyArmor = Math.Max(0, energyArmor);
            ExplosionArmor = Math.Max(0, explosionArmor);
            FirePressed = firePressed;
            InteractPressed = interactPressed;
            HasTarget = hasTarget;
            TargetIsVehicle = targetIsVehicle;
            TargetConfigId = targetConfigId;
            TargetCurrentHealth = Math.Max(0, targetCurrentHealth);
            TargetMaxHealth = Math.Max(0, targetMaxHealth);
        }

        public bool HasPlayer { get; }
        public bool IsVehicle { get; }
        public int ControlledConfigId { get; }
        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        public bool HasWeapon { get; }
        public int WeaponConfigId { get; }
        public WeaponType WeaponType { get; }
        public int SelectedWeaponSlot { get; }
        public float CooldownRemaining { get; }
        public float CooldownDuration { get; }
        public int BallisticArmor { get; }
        public int EnergyArmor { get; }
        public int ExplosionArmor { get; }
        public bool FirePressed { get; }
        public bool InteractPressed { get; }
        public bool HasTarget { get; }
        public bool TargetIsVehicle { get; }
        public int TargetConfigId { get; }
        public int TargetCurrentHealth { get; }
        public int TargetMaxHealth { get; }

        public bool IsWeaponReady => !HasWeapon || CooldownRemaining <= 0.0001f;
    }
}
