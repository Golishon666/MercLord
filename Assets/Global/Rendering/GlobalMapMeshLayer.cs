using UnityEngine;
using UnityEngine.Rendering;

namespace MercLord.Global.Rendering
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class GlobalMapMeshLayer : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private ShadowCastingMode shadowCastingMode = ShadowCastingMode.Off;
        [SerializeField] private bool receiveShadows;
        [SerializeField] private bool disableWhenEmpty = true;

        public bool HasMesh => meshFilter != null && meshFilter.sharedMesh != null;

        public void SetMesh(Mesh mesh, Material material)
        {
            ValidateReferences();

            var previousMesh = meshFilter.sharedMesh;
            if (previousMesh != mesh)
            {
                DestroyGeneratedMesh(previousMesh);
            }

            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = shadowCastingMode;
            meshRenderer.receiveShadows = receiveShadows;
            SetVisible(mesh != null || !disableWhenEmpty);
            if (!HasDontSaveFlag(mesh) && !HasDontSaveFlag(material))
            {
                SetEditorDirty(this);
            }
        }

        public void Clear()
        {
            ValidateReferences();

            DestroyGeneratedMesh(meshFilter.sharedMesh);
            meshFilter.sharedMesh = null;
            meshRenderer.sharedMaterial = null;
            SetVisible(!disableWhenEmpty);
            SetEditorDirty(this);
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
                SetEditorDirty(gameObject);
            }
        }

        private void ValidateReferences()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            ValidateReferences();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            ValidateReferences();
        }
#endif

        private static void DestroyGeneratedMesh(Mesh mesh)
        {
            if (mesh == null || IsPersistentAsset(mesh))
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(mesh);
            }
            else
            {
                DestroyImmediate(mesh);
            }
        }

        private static bool IsPersistentAsset(UnityEngine.Object target)
        {
#if UNITY_EDITOR
            return !Application.isPlaying && UnityEditor.EditorUtility.IsPersistent(target);
#else
            return false;
#endif
        }

        private static void SetEditorDirty(UnityEngine.Object target)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && target != null && !HasDontSaveFlag(target))
            {
                UnityEditor.EditorUtility.SetDirty(target);
            }
#endif
        }

        private static bool HasDontSaveFlag(UnityEngine.Object target)
        {
            const HideFlags dontSaveFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            return target != null && (target.hideFlags & dontSaveFlags) != 0;
        }
    }
}
