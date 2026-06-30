using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/AI", fileName = "AIConfig")]
    public sealed class AIConfig : IdentifiedConfig
    {
        [SerializeField] private AIType type;
        [SerializeField] private float thinkInterval;
        [SerializeField] private float targetSearchRadius;
        [SerializeField] private float preferredAttackDistance;
        [SerializeField] private float retreatHealthPercent;

        public AIType Type => type;
        public float ThinkInterval => thinkInterval;
        public float TargetSearchRadius => targetSearchRadius;
        public float PreferredAttackDistance => preferredAttackDistance;
        public float RetreatHealthPercent => retreatHealthPercent;
    }
}
