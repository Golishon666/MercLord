using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Combat Balance", fileName = "CombatBalanceConfig")]
    public sealed class CombatBalanceConfig : IdentifiedConfig
    {
        [SerializeField] private DamageFormula damageFormula;

        public DamageFormula DamageFormula => damageFormula;
    }
}
