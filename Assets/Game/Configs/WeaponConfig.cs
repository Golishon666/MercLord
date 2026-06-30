using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Weapon", fileName = "WeaponConfig")]
    public sealed class WeaponConfig : IdentifiedConfig
    {
        [SerializeField] private WeaponType type;
        [SerializeField] private DamageType damageType;
        [SerializeField] private int damage;
        [SerializeField] private float range;
        [SerializeField] private float cooldown;
        [SerializeField] private float projectileSpeed;
        [SerializeField] private bool isProjectile;
        [SerializeField] private bool usesParabolicTrajectory;
        [SerializeField] private float parabolicArcHeight;
        [SerializeField] private float explosionRadius;

        public WeaponType Type => type;
        public DamageType DamageType => damageType;
        public int Damage => damage;
        public float Range => range;
        public float Cooldown => cooldown;
        public float ProjectileSpeed => projectileSpeed;
        public bool IsProjectile => isProjectile;
        public bool UsesParabolicTrajectory => usesParabolicTrajectory;
        public float ParabolicArcHeight => parabolicArcHeight;
        public float ExplosionRadius => explosionRadius;
    }
}
