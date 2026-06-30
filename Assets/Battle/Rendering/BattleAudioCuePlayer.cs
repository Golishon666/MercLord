using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using UnityEngine;

namespace MercLord.Battle.Rendering
{
    public sealed class BattleAudioCuePlayer : IBattleAudioCuePlayer
    {
        private const float MinimumPitch = 0.05f;
        private const float MaximumPitch = 3f;

        private readonly BattleViewCatalog viewCatalog;
        private readonly Stack<AudioSource> availableSources = new Stack<AudioSource>();
        private readonly List<AudioSource> activeSources = new List<AudioSource>();

        private Transform root;

        public BattleAudioCuePlayer(BattleViewCatalog viewCatalog)
        {
            this.viewCatalog = viewCatalog ?? throw new ArgumentNullException(nameof(viewCatalog));
        }

        public void Tick(float deltaTime)
        {
            ReleaseFinishedSources();
        }

        public void Play(BattleAudioCueComponent cue)
        {
            if (!viewCatalog.TryGetAudioCueClip(cue.Type, out var clip))
            {
                return;
            }

            var source = RentSource();
            source.transform.position = viewCatalog.GridToWorld(cue.Position);
            source.clip = clip;
            source.volume = Mathf.Clamp01(cue.Volume);
            source.pitch = Mathf.Clamp(cue.Pitch, MinimumPitch, MaximumPitch);
            source.Play();
            activeSources.Add(source);
        }

        public void StopAll()
        {
            activeSources.Clear();
            availableSources.Clear();

            if (root != null)
            {
                DestroyObject(root.gameObject);
                root = null;
            }
        }

        private AudioSource RentSource()
        {
            ReleaseFinishedSources();
            while (availableSources.Count > 0)
            {
                var source = availableSources.Pop();
                if (source != null)
                {
                    source.gameObject.SetActive(true);
                    return source;
                }
            }

            return CreateSource();
        }

        private AudioSource CreateSource()
        {
            EnsureRoot();
            var gameObject = new GameObject("Battle Audio Source");
            gameObject.transform.SetParent(root, false);
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void EnsureRoot()
        {
            if (root != null)
            {
                return;
            }

            root = new GameObject("Battle Audio Cue Sources").transform;
        }

        private void ReleaseFinishedSources()
        {
            for (var sourceIndex = activeSources.Count - 1; sourceIndex >= 0; sourceIndex--)
            {
                var source = activeSources[sourceIndex];
                if (source != null && source.isPlaying)
                {
                    continue;
                }

                activeSources.RemoveAt(sourceIndex);
                ReturnSource(source);
            }
        }

        private void ReturnSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
            source.transform.SetParent(root, false);
            availableSources.Push(source);
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
