using System.Linq;
using MercLord.Bootstrap;
using MercLord.Game.Configs;
using MercLord.Game.UI;
using MercLord.Infrastructure.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MercLord.Editor.Tests
{
    public sealed class ProjectSceneValidationTests
    {
        private const string ConfigDatabasePath = "Assets/Game/Configs/ConfigDatabase.asset";
        private const string BootstrapScenePath = "Assets/Scenes/BootstrapScene.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";

        [Test]
        public void ProjectConfigDatabaseMatchesValidationContract()
        {
            var database = AssetDatabase.LoadAssetAtPath<ConfigDatabase>(ConfigDatabasePath);
            Assert.IsNotNull(database, $"Missing config database at {ConfigDatabasePath}.");

            var errors = new ConfigValidator().Validate(database)
                .Where(issue => issue.Severity == ValidationSeverity.Error)
                .ToArray();
            Assert.IsEmpty(errors, string.Join("\n", errors.Select(issue => issue.Message)));
        }

        [Test]
        public void BootstrapSceneMatchesValidationContract()
        {
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            try
            {
                var sceneRoot = FindRootWith<GameLifetimeScope>(scene);
                Assert.IsNotNull(sceneRoot, $"Missing GameLifetimeScope root in {BootstrapScenePath}.");

                var issues = new PrefabValidator().ValidateBootstrapScenePrefab(sceneRoot);
                AssertNoErrors(issues);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void MainMenuSceneMatchesValidationContract()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);
            try
            {
                var sceneRoot = FindRootWith<MainMenuLifetimeScope>(scene);
                Assert.IsNotNull(sceneRoot, $"Missing MainMenuLifetimeScope root in {MainMenuScenePath}.");

                var issues = new PrefabValidator().ValidateMainMenuScenePrefab(sceneRoot);
                AssertNoErrors(issues);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void CoreScenesAreInBuildSettingsInLoadOrder()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            Assert.Contains(BootstrapScenePath, scenes);
            Assert.Contains(MainMenuScenePath, scenes);
            Assert.Contains("Assets/Scenes/GlobalScene.unity", scenes);
            Assert.Contains("Assets/Scenes/BattleScene.unity", scenes);
            Assert.AreEqual(BootstrapScenePath, scenes.FirstOrDefault(), "BootstrapScene must be first enabled build scene.");
        }

        private static GameObject FindRootWith<T>(Scene scene)
            where T : Component
        {
            return scene.GetRootGameObjects()
                .FirstOrDefault(gameObject => gameObject.GetComponentInChildren<T>(true) != null);
        }

        private static void AssertNoErrors(System.Collections.Generic.IEnumerable<ValidationIssue> issues)
        {
            var errors = issues.Where(issue => issue.Severity == ValidationSeverity.Error).ToArray();
            Assert.IsEmpty(errors, string.Join("\n", errors.Select(issue => issue.Message)));
        }
    }
}
