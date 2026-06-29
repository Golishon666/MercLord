using UnityEngine;

namespace MercLord.Battle.Projectiles
{
    public sealed class ProjectileView : MonoBehaviour
    {
        [SerializeField] private ProjectileViewSettings settings;
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private SpriteRenderer bodyRenderer;

        public ProjectileViewSettings Settings => settings;
        public Transform BodyRoot => bodyRoot;
        public SpriteRenderer BodyRenderer => bodyRenderer;
    }
}
