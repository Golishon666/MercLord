using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Combat Balance", fileName = "CombatBalanceConfig")]
    public sealed class CombatBalanceConfig : IdentifiedConfig
    {
        [SerializeField] private DamageFormula damageFormula;
        [SerializeField] private HitChanceFormula hitChanceFormula = HitChanceFormula.Default;

        public DamageFormula DamageFormula => damageFormula;
        public HitChanceFormula HitChanceFormula => hitChanceFormula;
    }
}
