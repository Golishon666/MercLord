using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;
using UnityEngine;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class BattleCameraShakeSystem : IBattleRuntimeSystem
    {
        private const float PrimaryFrequency = 37f;
        private const float SecondaryFrequency = 23f;

        private readonly List<Entity> shakeBuffer = new List<Entity>();

        private World world;
        private Filter filter;
        private Stash<BattleCameraShakeComponent> shakes;
        private Camera configuredCamera;
        private Camera targetCamera;
        private Vector3 lastOffset;
        private Vector3 lastBasePosition;
        private float elapsedTime;

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

            world = session.World ?? throw new InvalidOperationException("BattleCameraShakeSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattleCameraShakeSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<BattleCameraShakeComponent>()
                .Build();
            shakes = world.GetStash<BattleCameraShakeComponent>();
            targetCamera = ResolveCamera();
            lastOffset = Vector3.zero;
            lastBasePosition = targetCamera != null ? targetCamera.transform.position : Vector3.zero;
            elapsedTime = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            elapsedTime += deltaTime;
            targetCamera = targetCamera != null ? targetCamera : ResolveCamera();
            shakeBuffer.Clear();

            foreach (var entity in filter)
            {
                shakeBuffer.Add(entity);
            }

            var totalIntensity = 0f;
            for (var index = 0; index < shakeBuffer.Count; index++)
            {
                var entity = shakeBuffer[index];
                ref var shake = ref shakes.Get(entity);
                shake.RemainingTime -= deltaTime;
                if (shake.RemainingTime <= 0f)
                {
                    world.RemoveEntity(entity);
                    continue;
                }

                var normalizedTime = shake.Duration > 0f
                    ? Mathf.Clamp01(shake.RemainingTime / shake.Duration)
                    : 0f;
                totalIntensity += shake.Intensity * normalizedTime;
            }

            ApplyOffset(totalIntensity);
            shakeBuffer.Clear();
        }

        public void Dispose()
        {
            if (targetCamera != null)
            {
                targetCamera.transform.position -= lastOffset;
            }

            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            shakeBuffer.Clear();
            filter = null;
            world = null;
            shakes = null;
            configuredCamera = null;
            targetCamera = null;
            lastOffset = Vector3.zero;
            lastBasePosition = Vector3.zero;
            elapsedTime = 0f;
        }

        private void ApplyOffset(float totalIntensity)
        {
            if (targetCamera == null)
            {
                return;
            }

            var cameraTransform = targetCamera.transform;
            var currentPosition = cameraTransform.position;
            var expectedPosition = lastBasePosition + lastOffset;
            var basePosition = lastOffset.sqrMagnitude > 0f &&
                               (currentPosition - expectedPosition).sqrMagnitude <= 0.0001f
                ? currentPosition - lastOffset
                : currentPosition;
            if (totalIntensity <= 0f)
            {
                cameraTransform.position = basePosition;
                lastOffset = Vector3.zero;
                lastBasePosition = basePosition;
                return;
            }

            lastBasePosition = basePosition;
            lastOffset = new Vector3(
                Mathf.Sin(elapsedTime * PrimaryFrequency) * totalIntensity,
                Mathf.Cos(elapsedTime * SecondaryFrequency) * totalIntensity,
                0f);
            cameraTransform.position = basePosition + lastOffset;
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
