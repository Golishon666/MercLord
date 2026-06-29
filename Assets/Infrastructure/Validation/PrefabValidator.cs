using System.Collections.Generic;
using MercLord.Battle.Rendering;
using MercLord.Battle.Vehicles;
using MercLord.Global.Rendering;
using UnityEngine;

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
    }
}
