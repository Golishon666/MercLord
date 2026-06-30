using System;
using System.Reflection;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Rendering;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class BattleAudioCuePlayerTests
    {
        [Test]
        public void PlayCreatesPooledAudioSourceForConfiguredCue()
        {
            var catalog = CreateCatalog();
            var clip = AudioClip.Create("test-shot", 64, 1, 44100, false);
            var player = new BattleAudioCuePlayer(catalog);

            try
            {
                SetField(catalog, "audioCues", new[]
                {
                    new BattleAudioCueEntry(BattleAudioCueType.HitscanShot, clip)
                });

                player.Play(new BattleAudioCueComponent
                {
                    Type = BattleAudioCueType.HitscanShot,
                    Position = new float2(2f, 3f),
                    Volume = 0.75f,
                    Pitch = 1.2f
                });

                var root = GameObject.Find("Battle Audio Cue Sources");
                Assert.IsNotNull(root);
                var source = root.GetComponentInChildren<AudioSource>(true);
                Assert.IsNotNull(source);
                Assert.AreEqual(clip, source.clip);
                Assert.AreEqual(0.75f, source.volume, 0.001f);
                Assert.AreEqual(1.2f, source.pitch, 0.001f);
                Assert.AreEqual(new Vector3(5f, 7f, 0f), source.transform.position);

                player.StopAll();

                Assert.IsNull(GameObject.Find("Battle Audio Cue Sources"));
            }
            finally
            {
                player.StopAll();
                UnityEngine.Object.DestroyImmediate(clip);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PlaySkipsUnconfiguredCueWithoutCreatingSource()
        {
            var catalog = CreateCatalog();
            var player = new BattleAudioCuePlayer(catalog);

            try
            {
                player.Play(new BattleAudioCueComponent
                {
                    Type = BattleAudioCueType.ProjectileShot,
                    Position = float2.zero,
                    Volume = 1f,
                    Pitch = 1f
                });

                Assert.IsNull(GameObject.Find("Battle Audio Cue Sources"));
            }
            finally
            {
                player.StopAll();
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        private static BattleViewCatalog CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<BattleViewCatalog>();
            SetField(catalog, "cellSize", new Vector2(2f, 2f));
            SetField(catalog, "origin", new Vector3(1f, 1f, 0f));
            return catalog;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new InvalidOperationException($"Field '{fieldName}' was not found on {target.GetType().Name}.");
        }
    }
}
