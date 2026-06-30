using UnityEngine;

namespace MercLord.Battle.Rendering
{
    public sealed class InfantryView : MonoBehaviour
    {
        private static readonly Color DeadBodyTint = new Color(0.35f, 0.35f, 0.35f, 0.75f);

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

        private bool cachedVisualState;
        private bool bodyRendererEnabled;
        private bool headRendererEnabled;
        private bool weaponRendererEnabled;
        private Color bodyRendererColor;
        private Color headRendererColor;
        private Color weaponRendererColor;

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
        public bool IsDeadVisual { get; private set; }

        private void OnEnable()
        {
            SetDeadVisual(false);
        }

        public void SetDeadVisual(bool isDead)
        {
            EnsureVisualStateCached();
            IsDeadVisual = isDead;

            if (bodyRenderer != null)
            {
                bodyRenderer.enabled = bodyRendererEnabled;
                bodyRenderer.color = isDead ? DeadBodyTint : bodyRendererColor;
            }

            if (headRenderer != null)
            {
                headRenderer.enabled = !isDead && headRendererEnabled;
                headRenderer.color = headRendererColor;
            }

            if (weaponRenderer != null)
            {
                weaponRenderer.enabled = !isDead && weaponRendererEnabled;
                weaponRenderer.color = weaponRendererColor;
            }
        }

        private void EnsureVisualStateCached()
        {
            if (cachedVisualState)
            {
                return;
            }

            bodyRendererEnabled = bodyRenderer == null || bodyRenderer.enabled;
            headRendererEnabled = headRenderer == null || headRenderer.enabled;
            weaponRendererEnabled = weaponRenderer == null || weaponRenderer.enabled;
            bodyRendererColor = bodyRenderer != null ? bodyRenderer.color : Color.white;
            headRendererColor = headRenderer != null ? headRenderer.color : Color.white;
            weaponRendererColor = weaponRenderer != null ? weaponRenderer.color : Color.white;
            cachedVisualState = true;
        }
    }
}
