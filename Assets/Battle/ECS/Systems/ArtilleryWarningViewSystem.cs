using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.Rendering;
using Scellecs.Morpeh;
using UnityEngine;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class ArtilleryWarningViewSystem : IBattleRuntimeSystem
    {
        private const int SegmentCount = 72;
        private static readonly Color WarningColor = new Color(1f, 0.12f, 0.05f, 0.72f);

        private readonly BattleViewCatalog viewCatalog;
        private readonly Dictionary<Entity, LineRenderer> views = new Dictionary<Entity, LineRenderer>();
        private readonly HashSet<Entity> visibleThisFrame = new HashSet<Entity>();
        private readonly List<Entity> staleBuffer = new List<Entity>();

        private World world;
        private Filter filter;
        private Stash<PositionComponent> positions;
        private Stash<ArtilleryWarningComponent> warnings;
        private Transform root;
        private Material lineMaterial;

        public ArtilleryWarningViewSystem(BattleViewCatalog viewCatalog)
        {
            this.viewCatalog = viewCatalog ?? throw new ArgumentNullException(nameof(viewCatalog));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("ArtilleryWarningViewSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("ArtilleryWarningViewSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<PositionComponent>()
                .With<ArtilleryWarningComponent>()
                .Build();
            positions = world.GetStash<PositionComponent>();
            warnings = world.GetStash<ArtilleryWarningComponent>();
            root = new GameObject("Battle Artillery Warning Views").transform;
            lineMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            visibleThisFrame.Clear();
            foreach (var entity in filter)
            {
                visibleThisFrame.Add(entity);
                var renderer = GetOrCreateView(entity);
                UpdateView(renderer, positions.Get(entity), warnings.Get(entity));
            }

            RemoveStaleViews();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            foreach (var renderer in views.Values)
            {
                if (renderer != null)
                {
                    UnityEngine.Object.Destroy(renderer.gameObject);
                }
            }

            views.Clear();
            visibleThisFrame.Clear();
            staleBuffer.Clear();
            if (root != null)
            {
                UnityEngine.Object.Destroy(root.gameObject);
            }

            if (lineMaterial != null)
            {
                UnityEngine.Object.Destroy(lineMaterial);
            }

            filter = null;
            world = null;
            positions = null;
            warnings = null;
            root = null;
            lineMaterial = null;
        }

        private LineRenderer GetOrCreateView(Entity entity)
        {
            if (views.TryGetValue(entity, out var renderer) && renderer != null)
            {
                return renderer;
            }

            var gameObject = new GameObject("Artillery Warning Circle");
            gameObject.transform.SetParent(root, false);
            renderer = gameObject.AddComponent<LineRenderer>();
            renderer.useWorldSpace = false;
            renderer.loop = true;
            renderer.positionCount = SegmentCount;
            renderer.startWidth = 0.06f;
            renderer.endWidth = 0.06f;
            renderer.material = lineMaterial;
            renderer.startColor = WarningColor;
            renderer.endColor = WarningColor;
            views[entity] = renderer;
            return renderer;
        }

        private void UpdateView(LineRenderer renderer, PositionComponent position, ArtilleryWarningComponent warning)
        {
            renderer.transform.position = viewCatalog.GridToWorld(position.Value);
            var radius = Mathf.Max(0.01f, warning.Radius);
            for (var segment = 0; segment < SegmentCount; segment++)
            {
                var angle = segment / (float)SegmentCount * Mathf.PI * 2f;
                renderer.SetPosition(segment, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void RemoveStaleViews()
        {
            staleBuffer.Clear();
            foreach (var pair in views)
            {
                if (!visibleThisFrame.Contains(pair.Key))
                {
                    staleBuffer.Add(pair.Key);
                }
            }

            for (var index = 0; index < staleBuffer.Count; index++)
            {
                var entity = staleBuffer[index];
                if (views.TryGetValue(entity, out var renderer) && renderer != null)
                {
                    UnityEngine.Object.Destroy(renderer.gameObject);
                }

                views.Remove(entity);
            }

            staleBuffer.Clear();
        }
    }
}
