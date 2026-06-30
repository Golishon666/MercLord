using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Armor", fileName = "ArmorConfig")]
    public sealed class ArmorConfig : IdentifiedConfig
    {
        [SerializeField] private int ballisticProtection;
        [SerializeField] private int energyProtection;
        [SerializeField] private int explosionProtection;

        public int BallisticProtection => ballisticProtection;
        public int EnergyProtection => energyProtection;
        public int ExplosionProtection => explosionProtection;
    }
}
