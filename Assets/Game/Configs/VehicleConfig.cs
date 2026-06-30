using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Vehicle", fileName = "VehicleConfig")]
    public sealed class VehicleConfig : IdentifiedConfig
    {
        [SerializeField] private int maxHealth;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private float enterRadius;
        [SerializeField] private float exitDistance;
        [SerializeField] private ArmorConfig armor;
        [SerializeField] private WeaponConfig weapon;
        [SerializeField] private string viewPrefabAddress;

        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float RotationSpeed => rotationSpeed;
        public float EnterRadius => enterRadius;
        public float ExitDistance => exitDistance;
        public ArmorConfig Armor => armor;
        public WeaponConfig Weapon => weapon;
        public string ViewPrefabAddress => viewPrefabAddress;
    }
}
