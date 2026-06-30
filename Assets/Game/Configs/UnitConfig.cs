using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Unit", fileName = "UnitConfig")]
    public sealed class UnitConfig : IdentifiedConfig
    {
        [SerializeField] private int factionId;
        [SerializeField] private UnitCategory category;
        [SerializeField] private int maxHealth;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private WeaponConfig weapon;
        [SerializeField] private ArmorConfig armor;
        [SerializeField] private AIConfig ai;
        [SerializeField] private string viewPrefabAddress;

        public int FactionId => factionId;
        public UnitCategory Category => category;
        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float RotationSpeed => rotationSpeed;
        public WeaponConfig Weapon => weapon;
        public ArmorConfig Armor => armor;
        public AIConfig AI => ai;
        public string ViewPrefabAddress => viewPrefabAddress;
    }
}
