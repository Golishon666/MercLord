using System.Linq;
using MercLord.Battle.Rendering;
using MercLord.Infrastructure.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MercLord.Editor.Tests
{
    public sealed class BattlePrefabValidationTests
    {
        private const string BattleViewCatalogPath = "Assets/Battle/Prefabs/BattleViewCatalog.asset";
        private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
        private const string InfantryPrefabPath = "Assets/Battle/Prefabs/BattleInfantryView.prefab";
        private const string VehiclePrefabPath = "Assets/Battle/Prefabs/BattleVehicleView.prefab";

        [Test]
        public void BattleViewCatalogMatchesValidationContract()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BattleViewCatalog>(BattleViewCatalogPath);
            Assert.IsNotNull(catalog, $"Missing battle view catalog at {BattleViewCatalogPath}.");

            var issues = new PrefabValidator().ValidateBattleViewCatalog(catalog);
            AssertNoErrors(issues);
        }

        [Test]
        public void BattleInfantryPrefabMatchesValidationContract()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InfantryPrefabPath);
            Assert.IsNotNull(prefab, $"Missing infantry prefab at {InfantryPrefabPath}.");

            var issues = new PrefabValidator().ValidateInfantryPrefab(prefab);
            AssertNoErrors(issues);
        }

        [Test]
        public void BattleVehiclePrefabMatchesValidationContract()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VehiclePrefabPath);
            Assert.IsNotNull(prefab, $"Missing vehicle prefab at {VehiclePrefabPath}.");

            var issues = new PrefabValidator().ValidateVehiclePrefab(prefab);
            AssertNoErrors(issues);
        }

        [Test]
        public void BattleSceneMatchesValidationContract()
        {
            var scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Additive);
            try
            {
                var sceneRoot = scene.GetRootGameObjects()
                    .FirstOrDefault(gameObject => gameObject.GetComponentInChildren<BattleLifetimeScope>(true) != null);
                Assert.IsNotNull(sceneRoot, $"Missing BattleLifetimeScope root in {BattleScenePath}.");

                var issues = new PrefabValidator().ValidateBattleScenePrefab(sceneRoot);
                AssertNoErrors(issues);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertNoErrors(System.Collections.Generic.IEnumerable<ValidationIssue> issues)
        {
            var errors = issues.Where(issue => issue.Severity == ValidationSeverity.Error).ToArray();
            Assert.IsEmpty(errors, string.Join("\n", errors.Select(issue => issue.Message)));
        }
    }
}
