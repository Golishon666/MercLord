using MercLord.Game.Configs;
using MercLord.Global.Cells;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MercLord.Global.Rendering
{
    [ExecuteAlways]
    public sealed class GlobalMapCellInfoController : MonoBehaviour
    {
        [SerializeField] private ProceduralGlobalMapRenderer mapRenderer;
        [SerializeField] private GlobalMapDebugController debugController;
        [SerializeField] private ConfigDatabase configDatabase;
        [SerializeField] private GlobalMapCellTooltipView tooltipView;
        [SerializeField] private Camera inputCamera;

        private int selectedCellId = WorldIds.None;

        public ProceduralGlobalMapRenderer MapRenderer => mapRenderer;
        public GlobalMapDebugController DebugController => debugController;
        public GlobalMapCellTooltipView TooltipView => tooltipView;
        public Camera InputCamera => inputCamera;

        public void Configure(
            ProceduralGlobalMapRenderer renderer,
            GlobalMapDebugController debug,
            ConfigDatabase database,
            GlobalMapCellTooltipView view,
            Camera camera)
        {
            mapRenderer = renderer;
            debugController = debug;
            configDatabase = database;
            tooltipView = view;
            inputCamera = camera;
            RefreshDate();
        }

        private void Update()
        {
            RefreshDate();

            if (!Application.isPlaying || mapRenderer == null || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (debugController != null && debugController.IsPointerOverDebugButton(Input.mousePosition))
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (inputCamera == null)
            {
                return;
            }

            var ray = inputCamera.ScreenPointToRay(Input.mousePosition);
            if (!mapRenderer.TryPickCell(ray, out var cellId) ||
                !mapRenderer.TryGetCell(cellId, out var cell))
            {
                ClearSelection();
                return;
            }

            selectedCellId = cellId;
            mapRenderer.SelectCell(cellId);
            tooltipView?.Show(cell, GetFactionName(cell.OwnerFactionId), Input.mousePosition);
        }

        public void ClearSelection()
        {
            selectedCellId = WorldIds.None;
            mapRenderer?.ClearSelection();
            tooltipView?.Hide();
        }

        private void RefreshDate()
        {
            var world = mapRenderer?.CurrentWorld;
            if (world == null)
            {
                return;
            }

            tooltipView?.SetDate(world.CurrentDay);
        }

        private string GetFactionName(int factionId)
        {
            if (factionId == WorldIds.None)
            {
                return "\u043d\u0435\u0442";
            }

            return configDatabase != null && configDatabase.TryGetFaction(factionId, out var faction)
                ? faction.DisplayName
                : $"Faction {factionId}";
        }
    }
}
