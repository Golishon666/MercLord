using UnityEngine;

namespace MercLord.Battle.Projectiles
{
    public sealed class ProjectileViewSettings : MonoBehaviour
    {
        [SerializeField] private float visualLifetime;
        [SerializeField] private float trailLength;

        public float VisualLifetime => visualLifetime;
        public float TrailLength => trailLength;
    }
}
