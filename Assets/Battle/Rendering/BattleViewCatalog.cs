using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using Unity.Mathematics;
using UnityEngine;

namespace MercLord.Battle.Rendering
{
    [CreateAssetMenu(menuName = "MercLord/Battle/Battle View Catalog", fileName = "BattleViewCatalog")]
    public sealed class BattleViewCatalog : ScriptableObject
    {
        [SerializeField] private Vector2 cellSize;
        [SerializeField] private Vector3 origin;
        [SerializeField] private GameObject projectileViewPrefab;
        [SerializeField] private UnitViewPrefabEntry[] unitViews = new UnitViewPrefabEntry[0];
        [SerializeField] private BattleAudioCueEntry[] audioCues = new BattleAudioCueEntry[0];

        public Vector2 CellSize => cellSize;
        public Vector3 Origin => origin;
        public GameObject ProjectileViewPrefab => projectileViewPrefab;
        public IReadOnlyList<UnitViewPrefabEntry> UnitViews => unitViews ?? Array.Empty<UnitViewPrefabEntry>();
        public IReadOnlyList<BattleAudioCueEntry> AudioCues => audioCues ?? Array.Empty<BattleAudioCueEntry>();

        public Vector3 GridToWorld(float2 gridPosition)
        {
            if (cellSize.x <= 0f || cellSize.y <= 0f)
            {
                throw new InvalidOperationException("BattleViewCatalog requires positive cell size.");
            }

            return origin + new Vector3(gridPosition.x * cellSize.x, gridPosition.y * cellSize.y, 0f);
        }

        public bool TryGetUnitViewPrefab(string address, out GameObject prefab)
        {
            return TryGetViewPrefab(address, out prefab);
        }

        public bool TryGetVehicleViewPrefab(string address, out GameObject prefab)
        {
            return TryGetViewPrefab(address, out prefab);
        }

        public bool TryGetProjectileViewPrefab(out GameObject prefab)
        {
            prefab = projectileViewPrefab;
            return prefab != null;
        }

        public bool TryGetAudioCueClip(BattleAudioCueType cueType, out AudioClip clip)
        {
            var entries = audioCues ?? Array.Empty<BattleAudioCueEntry>();
            for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                var entry = entries[entryIndex];
                if (entry.Type == cueType && entry.Clip != null)
                {
                    clip = entry.Clip;
                    return true;
                }
            }

            clip = null;
            return false;
        }

        private bool TryGetViewPrefab(string address, out GameObject prefab)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                prefab = null;
                return false;
            }

            var entries = unitViews ?? Array.Empty<UnitViewPrefabEntry>();
            for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                var entry = entries[entryIndex];
                if (entry.Address == address && entry.Prefab != null)
                {
                    prefab = entry.Prefab;
                    return true;
                }
            }

            prefab = null;
            return false;
        }
    }

    [Serializable]
    public struct UnitViewPrefabEntry
    {
        [SerializeField] private string address;
        [SerializeField] private GameObject prefab;

        public string Address => address;
        public GameObject Prefab => prefab;
    }

    [Serializable]
    public struct BattleAudioCueEntry
    {
        [SerializeField] private BattleAudioCueType type;
        [SerializeField] private AudioClip clip;

        public BattleAudioCueEntry(BattleAudioCueType type, AudioClip clip)
        {
            this.type = type;
            this.clip = clip;
        }

        public BattleAudioCueType Type => type;
        public AudioClip Clip => clip;
    }
}
