using UnityEngine;

namespace MercLord.Battle.Vehicles
{
    public sealed class VehicleView : MonoBehaviour
    {
        [SerializeField] private VehicleViewSettings settings;
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform turretRoot;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Transform enterPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer turretRenderer;

        public VehicleViewSettings Settings => settings;
        public Transform BodyRoot => bodyRoot;
        public Transform TurretRoot => turretRoot;
        public Transform MuzzlePoint => muzzlePoint;
        public Transform EnterPoint => enterPoint;
        public Transform ExitPoint => exitPoint;
        public SpriteRenderer BodyRenderer => bodyRenderer;
        public SpriteRenderer TurretRenderer => turretRenderer;
    }
}
