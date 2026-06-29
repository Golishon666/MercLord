using UnityEngine;

namespace MercLord.Battle.Rendering
{
    public sealed class InfantryView : MonoBehaviour
    {
        [SerializeField] private InfantryViewSettings settings;
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform headRoot;
        [SerializeField] private Transform weaponRoot;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer headRenderer;
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private DirectionalSpriteSet bodySprites;
        [SerializeField] private DirectionalSpriteSet headSprites;

        public InfantryViewSettings Settings => settings;
        public Transform BodyRoot => bodyRoot;
        public Transform HeadRoot => headRoot;
        public Transform WeaponRoot => weaponRoot;
        public Transform MuzzlePoint => muzzlePoint;
        public SpriteRenderer BodyRenderer => bodyRenderer;
        public SpriteRenderer HeadRenderer => headRenderer;
        public SpriteRenderer WeaponRenderer => weaponRenderer;
        public DirectionalSpriteSet BodySprites => bodySprites;
        public DirectionalSpriteSet HeadSprites => headSprites;
    }
}
