using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.Rendering;
using Scellecs.Morpeh;
using UnityEngine;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class BattleCameraFollowSystem : IBattleRuntimeSystem
    {
        private const float FollowSharpness = 18f;

        private readonly BattleViewCatalog viewCatalog;
        private readonly List<Entity> playerBuffer = new List<Entity>(2);

        private World world;
        private Filter filter;
        private Stash<PositionComponent> positions;
        private Camera configuredCamera;
        private Camera targetCamera;
        private bool hasAppliedInitialPosition;

        public BattleCameraFollowSystem(BattleViewCatalog viewCatalog)
        {
            this.viewCatalog = viewCatalog ?? throw new ArgumentNullException(nameof(viewCatalog));
        }

        public void SetTargetCamera(Camera camera)
        {
            configuredCamera = camera;
            targetCamera = camera;
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("BattleCameraFollowSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattleCameraFollowSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<PlayerControlledComponent>()
                .With<PositionComponent>()
                .Without<DeadComponent>()
                .Build();
            positions = world.GetStash<PositionComponent>();
            targetCamera = ResolveCamera();
            hasAppliedInitialPosition = false;
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            targetCamera = targetCamera != null ? targetCamera : ResolveCamera();
            if (targetCamera == null || !TryGetFollowTarget(out var targetPosition))
            {
                return;
            }

            var desiredPosition = viewCatalog.GridToWorld(targetPosition);
            desiredPosition.z = targetCamera.transform.position.z;

            if (!hasAppliedInitialPosition || deltaTime <= 0f)
            {
                targetCamera.transform.position = desiredPosition;
                hasAppliedInitialPosition = true;
                return;
            }

            var t = 1f - Mathf.Exp(-FollowSharpness * deltaTime);
            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position,
                desiredPosition,
                t);
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            playerBuffer.Clear();
            filter = null;
            world = null;
            positions = null;
            configuredCamera = null;
            targetCamera = null;
            hasAppliedInitialPosition = false;
        }

        private bool TryGetFollowTarget(out Unity.Mathematics.float2 targetPosition)
        {
            playerBuffer.Clear();
            foreach (var entity in filter)
            {
                playerBuffer.Add(entity);
            }

            if (playerBuffer.Count == 0)
            {
                targetPosition = default;
                return false;
            }

            targetPosition = positions.Get(playerBuffer[0]).Value;
            playerBuffer.Clear();
            return true;
        }

        private Camera ResolveCamera()
        {
            if (configuredCamera != null)
            {
                return configuredCamera;
            }

            var camera = Camera.main;
            return camera != null
                ? camera
                : UnityEngine.Object.FindAnyObjectByType<Camera>();
        }
    }
}
