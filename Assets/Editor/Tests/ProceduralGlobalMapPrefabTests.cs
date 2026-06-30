using System.Linq;
using MercLord.Global.Rendering;
using MercLord.Infrastructure.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MercLord.Editor.Tests
{
    public sealed class ProceduralGlobalMapPrefabTests
    {
        private const string ProceduralLayersPrefabPath = "Assets/Global/Prefabs/GlobalMapProceduralLayers.prefab";
        private const string GlobalScenePath = "Assets/Scenes/GlobalScene.unity";

        [Test]
        public void ProceduralLayersPrefabMatchesValidationContract()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProceduralLayersPrefabPath);
            Assert.IsNotNull(prefab, $"Missing prefab at {ProceduralLayersPrefabPath}.");

            var issues = new PrefabValidator().ValidateProceduralGlobalMapLayersPrefab(prefab);
            var errors = issues.Where(issue => issue.Severity == ValidationSeverity.Error).ToArray();

            Assert.IsEmpty(errors, string.Join("\n", errors.Select(issue => issue.Message)));
        }

        [Test]
        public void GlobalSceneReferencesAreExplicitlyConfigured()
        {
            var scene = EditorSceneManager.OpenScene(GlobalScenePath, OpenSceneMode.Additive);
            try
            {
                var sceneRoot = scene.GetRootGameObjects()
                    .FirstOrDefault(gameObject => gameObject.GetComponent<GlobalMapSceneBootstrap>() != null);
                Assert.IsNotNull(sceneRoot, $"Missing GlobalMapSceneBootstrap root in {GlobalScenePath}.");

                var issues = new PrefabValidator().ValidateGlobalMapSceneRoot(sceneRoot);
                var errors = issues.Where(issue => issue.Severity == ValidationSeverity.Error).ToArray();

                Assert.IsEmpty(errors, string.Join("\n", errors.Select(issue => issue.Message)));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
