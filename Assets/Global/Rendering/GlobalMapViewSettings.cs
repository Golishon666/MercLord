using UnityEngine;

namespace MercLord.Global.Rendering
{
    public sealed class GlobalMapViewSettings : MonoBehaviour
    {
        [SerializeField] private GlobalCellView cellViewPrefab;
        [SerializeField] private GlobalPlayerMarkerView playerMarkerPrefab;
        [SerializeField] private GlobalArmyMarkerView armyMarkerPrefab;
        [SerializeField] private Transform cellRoot;
        [SerializeField] private Transform markerRoot;
        [SerializeField] private int layoutColumnCount;
        [SerializeField] private Vector2 cellSpacing;
        [SerializeField] private Vector2 oddRowOffset;
        [SerializeField] private Vector3 markerOffset;
        [SerializeField] private float cellVisualScale;
        [SerializeField] private float influenceOverlayAlpha;
        [SerializeField] private Color playerMarkerColor;

        public GlobalCellView CellViewPrefab => cellViewPrefab;
        public GlobalPlayerMarkerView PlayerMarkerPrefab => playerMarkerPrefab;
        public GlobalArmyMarkerView ArmyMarkerPrefab => armyMarkerPrefab;
        public Transform CellRoot => cellRoot;
        public Transform MarkerRoot => markerRoot;
        public int LayoutColumnCount => layoutColumnCount;
        public Vector2 CellSpacing => cellSpacing;
        public Vector2 OddRowOffset => oddRowOffset;
        public Vector3 MarkerOffset => markerOffset;
        public float CellVisualScale => cellVisualScale;
        public float InfluenceOverlayAlpha => influenceOverlayAlpha;
        public Color PlayerMarkerColor => playerMarkerColor;
    }
}
