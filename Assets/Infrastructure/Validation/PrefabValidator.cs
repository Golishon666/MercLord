using System.Collections.Generic;
using MercLord.Bootstrap;
using MercLord.Game.Configs;
using MercLord.Game.UI;
using MercLord.Battle.Projectiles;
using MercLord.Battle.Rendering;
using MercLord.Battle.Vehicles;
using MercLord.Global.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace MercLord.Infrastructure.Validation
{
    public sealed class PrefabValidator
    {
        private const string LegacyUnityTextTypeName = "UnityEngine.UI." + "Text";

        public List<ValidationIssue> ValidateInfantryPrefab(GameObject prefab)
        {
            var issues = new List<ValidationIssue>();
            if (!ValidatePrefabRoot(prefab, issues))
            {
                return issues;
            }

            var view = prefab.GetComponentInChildren<InfantryView>(true);
            if (view == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Infantry prefab is missing InfantryView."));
                return issues;
            }

            if (view.Settings == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Infantry prefab is missing InfantryViewSettings reference."));
            }

            if (view.BodyRenderer == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Infantry prefab is missing BodyRenderer reference."));
            }

            if (view.HeadRenderer == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Infantry prefab is missing HeadRenderer reference."));
            }

            if (view.WeaponRenderer == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Infantry prefab is missing WeaponRenderer reference."));
            }

            if (view.BodyRoot == null || view.HeadRoot == null || view.WeaponRoot == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Infantry prefab is missing body/head/weapon roots."));
            }

            return issues;
        }

        public List<ValidationIssue> ValidateVehiclePrefab(GameObject prefab)
        {
            var issues = new List<ValidationIssue>();
            if (!ValidatePrefabRoot(prefab, issues))
            {
                return issues;
            }

            var view = prefab.GetComponentInChildren<VehicleView>(true);
            if (view == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Vehicle prefab is missing VehicleView."));
                return issues;
            }

            if (view.Settings == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Vehicle prefab is missing VehicleViewSettings reference."));
            }

            if (view.BodyRoot == null || view.MuzzlePoint == null || view.EnterPoint == null || view.ExitPoint == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Vehicle prefab is missing required transform references."));
            }

            return issues;
        }

        public List<ValidationIssue> ValidateGlobalMapPrefab(GameObject prefab)
        {
            var issues = new List<ValidationIssue>();
            if (!ValidatePrefabRoot(prefab, issues))
            {
                return issues;
            }

            var renderer = prefab.GetComponentInChildren<PlanetRenderer>(true);
            if (renderer == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Global map prefab is missing PlanetRenderer."));
                return issues;
            }

            var settings = renderer.Settings;
            if (settings == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "PlanetRenderer is missing GlobalMapViewSettings reference."));
                return issues;
            }

            if (settings.CellViewPrefab == null || settings.PlayerMarkerPrefab == null || settings.ArmyMarkerPrefab == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, settings, "Global map view settings must reference cell, player marker and army marker prefabs."));
            }
            else
            {
                if (settings.CellViewPrefab.BiomeRenderer == null || settings.CellViewPrefab.InfluenceOverlayRenderer == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, settings.CellViewPrefab, "Global cell prefab is missing biome or influence renderers."));
                }

                if (settings.PlayerMarkerPrefab.BodyRenderer == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, settings.PlayerMarkerPrefab, "Global player marker prefab is missing body renderer."));
                }

                if (settings.ArmyMarkerPrefab.BodyRenderer == null || settings.ArmyMarkerPrefab.Label == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, settings.ArmyMarkerPrefab, "Global army marker prefab is missing body renderer or TextMeshPro label."));
                }
            }

            if (settings.CellRoot == null || settings.MarkerRoot == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, settings, "Global map view settings must reference cell and marker roots."));
            }

            if (settings.LayoutColumnCount <= 0)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, settings, "Global map layout column count must be positive."));
            }

            if (settings.CellSpacing == Vector2.zero)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, settings, "Global map cell spacing must be configured."));
            }

            if (settings.CellVisualScale <= 0f)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, settings, "Global map cell visual scale must be positive."));
            }

            if (settings.InfluenceOverlayAlpha < 0f || settings.InfluenceOverlayAlpha > 1f)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, settings, "Global map influence overlay alpha must be between zero and one."));
            }

            return issues;
        }

        public List<ValidationIssue> ValidateProceduralGlobalMapLayersPrefab(GameObject prefab)
        {
            var issues = new List<ValidationIssue>();
            if (!ValidatePrefabRoot(prefab, issues))
            {
                return issues;
            }

            var settings = prefab.GetComponentInChildren<GlobalMapProceduralRenderSettings>(true);
            if (settings == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Procedural global map layers prefab is missing GlobalMapProceduralRenderSettings."));
            }
            else
            {
                ValidateRequiredSceneReference(settings.VertexColorMaterialTemplate, settings, "Procedural global map render settings are missing vertex color material template.", issues);
                ValidateRequiredSceneReference(settings.IconMaterialTemplate, settings, "Procedural global map render settings are missing icon material template.", issues);
                ValidateProceduralBiomeFallbackPalette(settings, issues);
            }

            ValidateProceduralMeshLayer(prefab, "Starfield Mesh", issues);
            ValidateProceduralMeshLayer(prefab, "Terrain Mesh", issues);
            ValidateProceduralMeshLayer(prefab, "Rivers Mesh", issues);
            ValidateProceduralMeshLayer(prefab, "Roads Mesh", issues);
            ValidateProceduralMeshLayer(prefab, "Marker Icons Mesh", issues);
            ValidateProceduralMeshLayer(prefab, "Settlement Feature Textures Mesh", issues);
            ValidateProceduralMeshLayer(prefab, "Activity Feature Textures Mesh", issues);
            ValidateProceduralMeshLayer(prefab, "Selected Cell Highlight", issues);

            return issues;
        }

        public List<ValidationIssue> ValidateGlobalMapSceneRoot(GameObject sceneRoot)
        {
            var issues = new List<ValidationIssue>();
            if (sceneRoot == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, null, "Global map scene root reference is missing."));
                return issues;
            }

            var scope = sceneRoot.GetComponent<GlobalLifetimeScope>();
            if (scope == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, sceneRoot, "Global map scene root is missing GlobalLifetimeScope."));
                return issues;
            }

            var root = sceneRoot.GetComponent<GlobalSceneRoot>();
            if (root == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, sceneRoot, "Global map scene root is missing GlobalSceneRoot."));
                return issues;
            }

            ValidateRequiredSceneReference(root.ConfigDatabase, root, "GlobalSceneRoot is missing ConfigDatabase reference.", issues);
            ValidateRequiredSceneReference(root.ProceduralMapRenderer, root, "GlobalSceneRoot is missing ProceduralGlobalMapRenderer reference.", issues);
            ValidateRequiredSceneReference(root.DebugController, root, "GlobalSceneRoot is missing GlobalMapDebugController reference.", issues);
            ValidateRequiredSceneReference(root.TooltipView, root, "GlobalSceneRoot is missing GlobalMapCellTooltipView reference.", issues);
            ValidateRequiredSceneReference(root.OrbitalFollow, root, "GlobalSceneRoot is missing CinemachineOrbitalFollow reference.", issues);
            ValidateRequiredSceneReference(root.InputCamera, root, "GlobalSceneRoot is missing input Camera reference.", issues);

            return issues;
        }

        public List<ValidationIssue> ValidateBattleScenePrefab(GameObject prefab)
        {
            var issues = new List<ValidationIssue>();
            if (!ValidatePrefabRoot(prefab, issues))
            {
                return issues;
            }

            var scope = prefab.GetComponentInChildren<BattleLifetimeScope>(true);
            if (scope == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Battle scene prefab is missing BattleLifetimeScope."));
                return issues;
            }

            if (scope.SceneRoot == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, scope, "BattleLifetimeScope is missing BattleSceneRoot reference."));
            }

            if (scope.ViewCatalog == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, scope, "BattleLifetimeScope is missing BattleViewCatalog reference."));
            }
            else
            {
                issues.AddRange(ValidateBattleViewCatalog(scope.ViewCatalog));
            }

            if (scope.InputSource == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, scope, "BattleLifetimeScope is missing BattleInputSource reference."));
            }

            if (scope.TilemapView == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, scope, "BattleLifetimeScope is missing BattleTilemapView reference."));
            }
            else
            {
                ValidateBattleTilemapView(scope.TilemapView, issues);
            }

            var sceneRoot = scope.SceneRoot != null
                ? scope.SceneRoot
                : prefab.GetComponentInChildren<BattleSceneRoot>(true);
            if (sceneRoot == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Battle scene prefab is missing BattleSceneRoot."));
                return issues;
            }

            if (sceneRoot.UnitViewRoot == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, sceneRoot, "BattleSceneRoot is missing unit view root."));
            }

            return issues;
        }

        private static void ValidateBattleTilemapView(
            BattleTilemapView tilemapView,
            ICollection<ValidationIssue> issues)
        {
            ValidateTilemapReference(tilemapView.GroundTilemap, tilemapView, "BattleTilemapView is missing GroundTilemap reference.", issues);
            ValidateTilemapReference(tilemapView.RoadTilemap, tilemapView, "BattleTilemapView is missing RoadTilemap reference.", issues);
            ValidateTilemapReference(tilemapView.ObstacleTilemap, tilemapView, "BattleTilemapView is missing ObstacleTilemap reference.", issues);
            ValidateTilemapReference(tilemapView.DecorationTilemap, tilemapView, "BattleTilemapView is missing DecorationTilemap reference.", issues);
            ValidateTilemapReference(tilemapView.OverlayTilemap, tilemapView, "BattleTilemapView is missing OverlayTilemap reference.", issues);
            ValidateTilemapReference(tilemapView.DebugTilemap, tilemapView, "BattleTilemapView is missing DebugTilemap reference.", issues);
        }

        private static void ValidateTilemapReference(
            Tilemap tilemap,
            Object context,
            string message,
            ICollection<ValidationIssue> issues)
        {
            if (tilemap != null)
            {
                return;
            }

            issues.Add(new ValidationIssue(ValidationSeverity.Error, context, message));
        }

        public List<ValidationIssue> ValidateBootstrapScenePrefab(GameObject prefab)
        {
            var issues = new List<ValidationIssue>();
            if (!ValidatePrefabRoot(prefab, issues))
            {
                return issues;
            }

            var scope = prefab.GetComponentInChildren<GameLifetimeScope>(true);
            if (scope == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Bootstrap scene prefab is missing GameLifetimeScope."));
                return issues;
            }

            if (scope.ConfigDatabase == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, scope, "GameLifetimeScope is missing ConfigDatabase reference."));
            }

            return issues;
        }

        public List<ValidationIssue> ValidateMainMenuScenePrefab(GameObject prefab)
        {
            var issues = new List<ValidationIssue>();
            if (!ValidatePrefabRoot(prefab, issues))
            {
                return issues;
            }

            var scope = prefab.GetComponentInChildren<MainMenuLifetimeScope>(true);
            if (scope == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Main menu scene prefab is missing MainMenuLifetimeScope."));
                return issues;
            }

            if (scope.SceneRoot == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, scope, "MainMenuLifetimeScope is missing MainMenuSceneRoot reference."));
            }

            var sceneRoot = scope.SceneRoot != null
                ? scope.SceneRoot
                : prefab.GetComponentInChildren<MainMenuSceneRoot>(true);
            if (sceneRoot == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Main menu scene prefab is missing MainMenuSceneRoot."));
                return issues;
            }

            if (sceneRoot.NewGameButton == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, sceneRoot, "MainMenuSceneRoot is missing New Game button reference."));
            }
            else
            {
                ValidateButtonTextMeshPro(sceneRoot.NewGameButton, issues);
            }

            ValidateNoLegacyText(prefab, issues);
            return issues;
        }

        public List<ValidationIssue> ValidateBattleViewCatalog(BattleViewCatalog catalog)
        {
            var issues = new List<ValidationIssue>();
            if (catalog == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, null, "BattleViewCatalog reference is missing."));
                return issues;
            }

            if (catalog.CellSize.x <= 0f || catalog.CellSize.y <= 0f)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, catalog, "BattleViewCatalog cell size must be positive."));
            }

            if (catalog.ProjectileViewPrefab == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, catalog, "BattleViewCatalog projectile view prefab is missing."));
            }
            else if (catalog.ProjectileViewPrefab.GetComponentInChildren<ProjectileView>(true) == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, catalog.ProjectileViewPrefab, $"BattleViewCatalog projectile prefab '{catalog.ProjectileViewPrefab.name}' has no ProjectileView component."));
            }

            var unitViews = catalog.UnitViews;
            if (unitViews.Count == 0)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, catalog, "BattleViewCatalog must contain at least one unit view prefab."));
            }

            var usedAddresses = new HashSet<string>();
            for (var entryIndex = 0; entryIndex < unitViews.Count; entryIndex++)
            {
                var entry = unitViews[entryIndex];
                if (string.IsNullOrWhiteSpace(entry.Address))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, catalog, $"BattleViewCatalog unit view entry {entryIndex} has no address."));
                }
                else if (!usedAddresses.Add(entry.Address))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, catalog, $"BattleViewCatalog duplicates unit view address '{entry.Address}'."));
                }

                if (entry.Prefab == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, catalog, $"BattleViewCatalog unit view entry {entryIndex} has no prefab."));
                    continue;
                }

                var hasUnitView =
                    entry.Prefab.GetComponentInChildren<InfantryView>(true) != null ||
                    entry.Prefab.GetComponentInChildren<VehicleView>(true) != null;
                if (!hasUnitView)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, entry.Prefab, $"BattleViewCatalog prefab '{entry.Prefab.name}' has no supported battle unit view component."));
                }
            }

            return issues;
        }

        private static void ValidateButtonTextMeshPro(Button button, ICollection<ValidationIssue> issues)
        {
            if (button.GetComponentInChildren<TextMeshProUGUI>(true) == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, button, "Button is missing TextMeshProUGUI label."));
            }
        }

        private static void ValidateNoLegacyText(GameObject prefab, ICollection<ValidationIssue> issues)
        {
            var components = prefab.GetComponentsInChildren<Component>(true);
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component != null && component.GetType().FullName == LegacyUnityTextTypeName)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Prefab uses a legacy Unity UI text component."));
                }
            }
        }

        public List<ValidationIssue> ValidateTextPrefab(GameObject prefab)
        {
            var issues = new List<ValidationIssue>();
            if (!ValidatePrefabRoot(prefab, issues))
            {
                return issues;
            }

            var components = prefab.GetComponentsInChildren<Component>(true);
            var hasTextMeshPro = false;
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null)
                {
                    continue;
                }

                var typeName = component.GetType().FullName;
                if (typeName == LegacyUnityTextTypeName)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Text prefab uses a legacy Unity UI text component."));
                }

                if (typeName == "TMPro.TextMeshPro" || typeName == "TMPro.TextMeshProUGUI")
                {
                    hasTextMeshPro = true;
                }
            }

            if (!hasTextMeshPro)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, "Text prefab is missing TextMeshPro/TextMeshProUGUI."));
            }

            return issues;
        }

        private static bool ValidatePrefabRoot(GameObject prefab, ICollection<ValidationIssue> issues)
        {
            if (prefab != null)
            {
                return true;
            }

            issues.Add(new ValidationIssue(ValidationSeverity.Error, null, "Prefab reference is missing."));
            return false;
        }

        private static void ValidateRequiredSceneReference(
            Object reference,
            Object context,
            string message,
            ICollection<ValidationIssue> issues)
        {
            if (reference != null)
            {
                return;
            }

            issues.Add(new ValidationIssue(ValidationSeverity.Error, context, message));
        }

        private static void ValidateProceduralMeshLayer(
            GameObject prefab,
            string layerName,
            ICollection<ValidationIssue> issues)
        {
            var transforms = prefab.GetComponentsInChildren<Transform>(true);
            Transform layerTransform = null;
            for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                if (transforms[transformIndex].name == layerName)
                {
                    layerTransform = transforms[transformIndex];
                    break;
                }
            }

            if (layerTransform == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, prefab, $"Procedural global map layers prefab is missing '{layerName}'."));
                return;
            }

            if (layerTransform.GetComponent<GlobalMapMeshLayer>() == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, layerTransform.gameObject, $"Procedural global map layer '{layerName}' is missing GlobalMapMeshLayer."));
            }

            if (layerTransform.GetComponent<MeshFilter>() == null || layerTransform.GetComponent<MeshRenderer>() == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, layerTransform.gameObject, $"Procedural global map layer '{layerName}' is missing MeshFilter or MeshRenderer."));
            }
        }

        private static void ValidateProceduralBiomeFallbackPalette(
            GlobalMapProceduralRenderSettings settings,
            ICollection<ValidationIssue> issues)
        {
            var coveredBiomes = new HashSet<BiomeType>();
            var fallbackColors = settings.FallbackBiomeColors;
            for (var entryIndex = 0; entryIndex < fallbackColors.Count; entryIndex++)
            {
                coveredBiomes.Add(fallbackColors[entryIndex].BiomeType);
            }

            foreach (BiomeType biomeType in System.Enum.GetValues(typeof(BiomeType)))
            {
                if (!coveredBiomes.Contains(biomeType))
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, settings, $"Procedural global map fallback palette is missing {biomeType}."));
                }
            }
        }
    }
}
