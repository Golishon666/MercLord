using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.Rendering;
using Scellecs.Morpeh;
using UnityEngine;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class HitscanTraceViewSystem : IBattleRuntimeSystem
    {
        private static readonly Color HitColor = new Color(1f, 0.82f, 0.18f, 0.88f);
        private static readonly Color MissColor = new Color(0.8f, 0.8f, 0.8f, 0.55f);
        private static readonly Color MuzzleColor = new Color(1f, 0.96f, 0.65f, 0.95f);
        private const float MuzzleLength = 0.18f;

        private readonly BattleViewCatalog viewCatalog;
        private readonly Dictionary<Entity, TraceView> views = new Dictionary<Entity, TraceView>();
        private readonly HashSet<Entity> visibleThisFrame = new HashSet<Entity>();
        private readonly List<Entity> staleBuffer = new List<Entity>();

        private World world;
        private Filter filter;
        private Stash<HitscanTraceComponent> traces;
        private Transform root;
        private Material lineMaterial;

        public HitscanTraceViewSystem(BattleViewCatalog viewCatalog)
        {
            this.viewCatalog = viewCatalog ?? throw new ArgumentNullException(nameof(viewCatalog));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("HitscanTraceViewSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("HitscanTraceViewSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<HitscanTraceComponent>()
                .Build();
            traces = world.GetStash<HitscanTraceComponent>();
            root = new GameObject("Battle Hitscan Trace Views").transform;
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
                UpdateView(renderer, traces.Get(entity));
            }

            RemoveStaleViews();
            visibleThisFrame.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            foreach (var view in views.Values)
            {
                if (view?.Root != null)
                {
                    DestroyObject(view.Root);
                }
            }

            views.Clear();
            visibleThisFrame.Clear();
            staleBuffer.Clear();

            if (root != null)
            {
                DestroyObject(root.gameObject);
            }

            if (lineMaterial != null)
            {
                DestroyObject(lineMaterial);
            }

            filter = null;
            world = null;
            traces = null;
            root = null;
            lineMaterial = null;
        }

        private TraceView GetOrCreateView(Entity entity)
        {
            if (views.TryGetValue(entity, out var view) && view?.Root != null)
            {
                return view;
            }

            var traceRoot = new GameObject("Hitscan Trace");
            traceRoot.transform.SetParent(root, false);
            view = new TraceView(
                traceRoot,
                CreateLineRenderer(traceRoot.transform, "Trail", 0.045f, 0.012f),
                CreateLineRenderer(traceRoot.transform, "Muzzle", 0.12f, 0.02f));
            views[entity] = view;
            return view;
        }

        private LineRenderer CreateLineRenderer(Transform parent, string name, float startWidth, float endWidth)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            var renderer = gameObject.AddComponent<LineRenderer>();
            renderer.useWorldSpace = true;
            renderer.positionCount = 2;
            renderer.startWidth = startWidth;
            renderer.endWidth = endWidth;
            renderer.material = lineMaterial;
            renderer.numCapVertices = 2;
            return renderer;
        }

        private void UpdateView(TraceView view, HitscanTraceComponent trace)
        {
            var color = trace.Hit ? HitColor : MissColor;
            var alpha = trace.Duration > 0f
                ? Mathf.Clamp01(trace.RemainingTime / trace.Duration)
                : 0f;
            color.a *= alpha;
            var start = viewCatalog.GridToWorld(trace.Start);
            var end = viewCatalog.GridToWorld(trace.End);

            view.Trail.startColor = color;
            view.Trail.endColor = new Color(color.r, color.g, color.b, 0f);
            view.Trail.SetPosition(0, start);
            view.Trail.SetPosition(1, end);

            var muzzleColor = MuzzleColor;
            muzzleColor.a *= alpha;
            view.Muzzle.startColor = muzzleColor;
            view.Muzzle.endColor = new Color(muzzleColor.r, muzzleColor.g, muzzleColor.b, 0f);
            view.Muzzle.SetPosition(0, start);
            view.Muzzle.SetPosition(1, ResolveMuzzleEnd(start, end));
        }

        private static Vector3 ResolveMuzzleEnd(Vector3 start, Vector3 end)
        {
            var direction = end - start;
            if (direction.sqrMagnitude <= float.Epsilon)
            {
                return start;
            }

            return start + direction.normalized * MuzzleLength;
        }

        private void RemoveStaleViews()
        {
            staleBuffer.Clear();
            foreach (var pair in views)
            {
                if (!visibleThisFrame.Contains(pair.Key) || !world.Has(pair.Key))
                {
                    staleBuffer.Add(pair.Key);
                }
            }

            for (var index = 0; index < staleBuffer.Count; index++)
            {
                var entity = staleBuffer[index];
                if (views.TryGetValue(entity, out var view) && view?.Root != null)
                {
                    DestroyObject(view.Root);
                }

                views.Remove(entity);
            }

            staleBuffer.Clear();
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

        private sealed class TraceView
        {
            public TraceView(GameObject root, LineRenderer trail, LineRenderer muzzle)
            {
                Root = root;
                Trail = trail;
                Muzzle = muzzle;
            }

            public GameObject Root { get; }
            public LineRenderer Trail { get; }
            public LineRenderer Muzzle { get; }
        }
    }
}
