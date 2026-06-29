using System;
using MercLord.Game.Configs;
using MercLord.Global.Cells;
using MercLord.Infrastructure.Pooling;
using UnityEngine;

namespace MercLord.Global.Rendering
{
    public interface IGlobalMapPresenter
    {
        void Render(PlanetRenderer renderer, WorldModel worldModel);
    }

    public interface IWorldCellLayout
    {
        Vector3 GetCellPosition(WorldCell cell, int cellIndex, GlobalMapViewSettings settings);
    }

    public sealed class ConfiguredGridWorldCellLayout : IWorldCellLayout
    {
        public Vector3 GetCellPosition(WorldCell cell, int cellIndex, GlobalMapViewSettings settings)
        {
            if (settings.LayoutColumnCount <= 0)
            {
                throw new InvalidOperationException("Global map layout column count must be configured.");
            }

            var row = cellIndex / settings.LayoutColumnCount;
            var column = cellIndex % settings.LayoutColumnCount;
            var offset = row % 2 == 0 ? Vector2.zero : settings.OddRowOffset;
            return new Vector3(
                column * settings.CellSpacing.x + offset.x,
                row * settings.CellSpacing.y + offset.y,
                0f);
        }
    }

    public sealed class GlobalMapPresenter : IGlobalMapPresenter
    {
        private readonly ConfigDatabase configDatabase;
        private readonly IPrefabFactory prefabFactory;
        private readonly IWorldCellLayout cellLayout;

        public GlobalMapPresenter(
            ConfigDatabase configDatabase,
            IPrefabFactory prefabFactory,
            IWorldCellLayout cellLayout)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
            this.prefabFactory = prefabFactory ?? throw new ArgumentNullException(nameof(prefabFactory));
            this.cellLayout = cellLayout ?? throw new ArgumentNullException(nameof(cellLayout));
        }

        public void Render(PlanetRenderer renderer, WorldModel worldModel)
        {
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            if (worldModel == null)
            {
                throw new ArgumentNullException(nameof(worldModel));
            }

            var settings = renderer.Settings
                ?? throw new InvalidOperationException("PlanetRenderer requires GlobalMapViewSettings.");

            ValidateSettings(settings);
            renderer.ClearSpawnedViews();
            renderer.Bind(worldModel);

            var cellViews = RenderCells(renderer, settings, worldModel);
            RenderPlayerMarker(renderer, settings, worldModel, cellViews);
            RenderArmyMarkers(renderer, settings, worldModel, cellViews);
        }

        private GlobalCellView[] RenderCells(
            PlanetRenderer renderer,
            GlobalMapViewSettings settings,
            WorldModel worldModel)
        {
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            var cellViews = new GlobalCellView[cells.Length];
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                var cell = cells[cellIndex];
                var cellView = prefabFactory.Instantiate(settings.CellViewPrefab, settings.CellRoot);
                var cellPosition = cellLayout.GetCellPosition(cell, cellIndex, settings);

                cellView.transform.localPosition = cellPosition;
                cellView.transform.localScale = Vector3.one * settings.CellVisualScale;
                cellView.Bind(
                    cell.Id,
                    GetBiomeColor(cell.Biome),
                    GetInfluenceOverlayColor(worldModel, cell, settings.InfluenceOverlayAlpha));

                renderer.TrackSpawnedView(cellView);
                cellViews[cellIndex] = cellView;
            }

            return cellViews;
        }

        private void RenderPlayerMarker(
            PlanetRenderer renderer,
            GlobalMapViewSettings settings,
            WorldModel worldModel,
            GlobalCellView[] cellViews)
        {
            var player = worldModel.Player;
            if (player == null || player.CellId == WorldIds.None)
            {
                return;
            }

            var cellView = FindCellView(worldModel, cellViews, player.CellId);
            var playerMarker = prefabFactory.Instantiate(settings.PlayerMarkerPrefab, settings.MarkerRoot);
            playerMarker.transform.localPosition = cellView.transform.localPosition + settings.MarkerOffset;
            playerMarker.Bind(settings.PlayerMarkerColor);
            renderer.TrackSpawnedView(playerMarker);
        }

        private void RenderArmyMarkers(
            PlanetRenderer renderer,
            GlobalMapViewSettings settings,
            WorldModel worldModel,
            GlobalCellView[] cellViews)
        {
            var armies = worldModel.Armies ?? Array.Empty<ArmyData>();
            for (var armyIndex = 0; armyIndex < armies.Length; armyIndex++)
            {
                var army = armies[armyIndex];
                if (army.CellId == WorldIds.None)
                {
                    continue;
                }

                var cellView = FindCellView(worldModel, cellViews, army.CellId);
                var armyMarker = prefabFactory.Instantiate(settings.ArmyMarkerPrefab, settings.MarkerRoot);
                armyMarker.transform.localPosition = cellView.transform.localPosition + settings.MarkerOffset;
                armyMarker.Bind(army, GetFactionColor(army.FactionId));
                renderer.TrackSpawnedView(armyMarker);
            }
        }

        private static void ValidateSettings(GlobalMapViewSettings settings)
        {
            if (settings.CellViewPrefab == null)
            {
                throw new InvalidOperationException("Global map cell view prefab must be configured.");
            }

            if (settings.PlayerMarkerPrefab == null)
            {
                throw new InvalidOperationException("Global map player marker prefab must be configured.");
            }

            if (settings.ArmyMarkerPrefab == null)
            {
                throw new InvalidOperationException("Global map army marker prefab must be configured.");
            }

            if (settings.CellRoot == null || settings.MarkerRoot == null)
            {
                throw new InvalidOperationException("Global map cell and marker roots must be configured.");
            }

            if (settings.CellVisualScale <= 0f)
            {
                throw new InvalidOperationException("Global map cell visual scale must be positive.");
            }

            if (settings.CellSpacing == Vector2.zero)
            {
                throw new InvalidOperationException("Global map cell spacing must be configured.");
            }

            if (settings.InfluenceOverlayAlpha < 0f || settings.InfluenceOverlayAlpha > 1f)
            {
                throw new InvalidOperationException("Global map influence overlay alpha must be between zero and one.");
            }
        }

        private Color GetBiomeColor(BiomeType biomeType)
        {
            for (var biomeIndex = 0; biomeIndex < configDatabase.Biomes.Count; biomeIndex++)
            {
                var biomeConfig = configDatabase.Biomes[biomeIndex];
                if (biomeConfig != null && biomeConfig.BiomeType == biomeType)
                {
                    return biomeConfig.MapColor;
                }
            }

            throw new InvalidOperationException($"Biome color is not configured for {biomeType}.");
        }

        private Color GetFactionColor(int factionId)
        {
            if (configDatabase.TryGetFaction(factionId, out var factionConfig))
            {
                return factionConfig.Color;
            }

            throw new InvalidOperationException($"Faction color is not configured for faction id {factionId}.");
        }

        private Color GetInfluenceOverlayColor(WorldModel worldModel, WorldCell cell, float overlayAlpha)
        {
            var factionSlot = FindFactionSlot(worldModel, cell.DominantFactionId);
            var factionColor = GetFactionColor(cell.DominantFactionId);
            var influenceRatio = GetInfluenceRatio(worldModel, cell.Influence, factionSlot);
            factionColor.a = overlayAlpha * influenceRatio;
            return factionColor;
        }

        private static float GetInfluenceRatio(WorldModel worldModel, Influence4 influence, int dominantFactionSlot)
        {
            var totalInfluence = 0f;
            for (var factionSlot = 0; factionSlot < worldModel.Factions.Length; factionSlot++)
            {
                totalInfluence += influence.Get(factionSlot);
            }

            if (totalInfluence <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(influence.Get(dominantFactionSlot) / totalInfluence);
        }

        private static int FindFactionSlot(WorldModel worldModel, int factionId)
        {
            for (var factionSlot = 0; factionSlot < worldModel.Factions.Length; factionSlot++)
            {
                if (worldModel.Factions[factionSlot].Id == factionId)
                {
                    return factionSlot;
                }
            }

            throw new InvalidOperationException($"World references unknown faction id {factionId}.");
        }

        private static GlobalCellView FindCellView(
            WorldModel worldModel,
            GlobalCellView[] cellViews,
            int cellId)
        {
            var cells = worldModel.Cells ?? Array.Empty<WorldCell>();
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (cells[cellIndex].Id == cellId)
                {
                    return cellViews[cellIndex];
                }
            }

            throw new InvalidOperationException($"Global marker references unknown cell id {cellId}.");
        }
    }
}
