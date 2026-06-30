using System;
using System.Collections.Generic;
using MercLord.Battle.Generation;
using MercLord.Battle.Rendering;
using Scellecs.Morpeh;
using UnityEngine;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class SpatialHashDebugViewSystem : IBattleRuntimeSystem
    {
        private static readonly Color BucketColor = new Color(0.1f, 0.85f, 1f, 0.42f);

        private readonly SpatialHashSystem spatialHashSystem;
        private readonly BattleViewCatalog viewCatalog;
        private readonly BattleTilemapView tilemapView;
        private readonly List<SpatialHashBucketDebugInfo> bucketBuffer = new List<SpatialHashBucketDebugInfo>(128);
        private readonly List<LineRenderer> renderers = new List<LineRenderer>(128);

        private World world;
        private Transform root;
        private Material lineMaterial;

        public SpatialHashDebugViewSystem(
            SpatialHashSystem spatialHashSystem,
            BattleViewCatalog viewCatalog,
            BattleTilemapView tilemapView)
        {
            this.spatialHashSystem = spatialHashSystem ?? throw new ArgumentNullException(nameof(spatialHashSystem));
            this.viewCatalog = viewCatalog ?? throw new ArgumentNullException(nameof(viewCatalog));
            this.tilemapView = tilemapView ?? throw new ArgumentNullException(nameof(tilemapView));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("SpatialHashDebugViewSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("SpatialHashDebugViewSystem cannot initialize on a disposed Morpeh world.");
            }

            root = new GameObject("Battle Spatial Hash Debug Views").transform;
            lineMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || root == null)
            {
                return;
            }

            if (tilemapView.DebugOverlayMode != BattleDebugOverlayMode.SpatialBuckets)
            {
                EnsureRendererCount(0);
                return;
            }

            spatialHashSystem.GetDebugBuckets(bucketBuffer);
            EnsureRendererCount(bucketBuffer.Count);
            for (var bucketIndex = 0; bucketIndex < bucketBuffer.Count; bucketIndex++)
            {
                UpdateRenderer(renderers[bucketIndex], bucketBuffer[bucketIndex]);
            }

            bucketBuffer.Clear();
        }

        public void Dispose()
        {
            for (var rendererIndex = renderers.Count - 1; rendererIndex >= 0; rendererIndex--)
            {
                var renderer = renderers[rendererIndex];
                if (renderer != null)
                {
                    DestroyObject(renderer.gameObject);
                }
            }

            renderers.Clear();
            bucketBuffer.Clear();

            if (root != null)
            {
                DestroyObject(root.gameObject);
            }

            if (lineMaterial != null)
            {
                DestroyObject(lineMaterial);
            }

            world = null;
            root = null;
            lineMaterial = null;
        }

        private void EnsureRendererCount(int count)
        {
            while (renderers.Count < count)
            {
                renderers.Add(CreateRenderer(renderers.Count));
            }

            for (var rendererIndex = renderers.Count - 1; rendererIndex >= count; rendererIndex--)
            {
                var renderer = renderers[rendererIndex];
                renderers.RemoveAt(rendererIndex);
                if (renderer != null)
                {
                    DestroyObject(renderer.gameObject);
                }
            }
        }

        private LineRenderer CreateRenderer(int index)
        {
            var gameObject = new GameObject($"Spatial Bucket {index}");
            gameObject.transform.SetParent(root, false);
            var renderer = gameObject.AddComponent<LineRenderer>();
            renderer.useWorldSpace = true;
            renderer.loop = true;
            renderer.positionCount = 4;
            renderer.startWidth = 0.04f;
            renderer.endWidth = 0.04f;
            renderer.material = lineMaterial;
            renderer.startColor = BucketColor;
            renderer.endColor = BucketColor;
            renderer.numCornerVertices = 1;
            return renderer;
        }

        private void UpdateRenderer(LineRenderer renderer, SpatialHashBucketDebugInfo bucket)
        {
            var min = bucket.Min;
            var max = bucket.Max;
            renderer.gameObject.name = $"Spatial Bucket {bucket.CellX},{bucket.CellY} ({bucket.EntityCount})";
            renderer.SetPosition(0, viewCatalog.GridToWorld(min));
            renderer.SetPosition(1, viewCatalog.GridToWorld(new Unity.Mathematics.float2(max.x, min.y)));
            renderer.SetPosition(2, viewCatalog.GridToWorld(max));
            renderer.SetPosition(3, viewCatalog.GridToWorld(new Unity.Mathematics.float2(min.x, max.y)));
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
