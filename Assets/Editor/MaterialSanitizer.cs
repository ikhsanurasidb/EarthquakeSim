using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GazeVR.EditorTools
{
    /// <summary>
    /// Scans the project's prop and character prefabs and repairs any material that would render as
    /// magenta under the Universal Render Pipeline:
    ///   * null material slots are filled with a neutral URP fallback material,
    ///   * built-in (Standard / Legacy) materials that are standalone assets are upgraded in place
    ///     to URP/Lit (preserving their color and main texture),
    ///   * built-in materials embedded inside FBX models are replaced with a generated URP copy
    ///     (so the read-only model material is left untouched).
    ///
    /// Run it from <b>GazeVR ▸ Fix Materials (No Magenta)</b>. It also prints a report of every
    /// distinct shader it encountered so you can verify nothing magenta remains.
    /// </summary>
    public static class MaterialSanitizer
    {
        const string MatFolder = "Assets/_GazeVR/Materials";
        static readonly string[] SearchRoots = { "Assets/Environments", "Assets/Characters" };

        [MenuItem("GazeVR/Fix Materials (No Magenta)")]
        public static void FixMaterialsMenu()
        {
            int prefabs = SanitizeAllPrefabs(out int slots, out int upgraded, out string report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MaterialSanitizer] Done. Prefabs changed: {prefabs}, slots fixed: {slots}, " +
                      $"materials upgraded/created: {upgraded}.\nShaders seen across prefabs:\n{report}");
        }

        public static Shader UrpLit
        {
            get
            {
                var s = Shader.Find("Universal Render Pipeline/Lit");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
                return s;
            }
        }

        /// <summary>A shared neutral URP material used to fill null slots.</summary>
        public static Material GetFallbackMaterial()
        {
            EnsureFolder(MatFolder);
            const string path = MatFolder + "/URP_Fallback.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(UrpLit) { name = "URP_Fallback" };
                SetBaseColor(mat, new Color(0.76f, 0.76f, 0.78f));
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        public static int SanitizeAllPrefabs(out int slotsFixed, out int matsUpgraded, out string shaderReport)
        {
            slotsFixed = 0;
            matsUpgraded = 0;

            var copyCache = new Dictionary<Material, Material>();
            var upgradedSet = new HashSet<Material>();
            var shaderCounts = new SortedDictionary<string, int>();
            var fallback = GetFallbackMaterial();

            string[] guids = AssetDatabase.FindAssets("t:Prefab", SearchRoots);
            int prefabsChanged = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".prefab")) continue;

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool prefabChanged = false;

                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] mats = r.sharedMaterials;
                    bool rendererChanged = false;

                    for (int i = 0; i < mats.Length; i++)
                    {
                        RecordShader(shaderCounts, mats[i]);
                        if (!IsBroken(mats[i])) continue;

                        Material replacement = GetReplacement(
                            mats[i], copyCache, upgradedSet, fallback, ref matsUpgraded);

                        if (replacement != mats[i])
                        {
                            mats[i] = replacement;
                            rendererChanged = true;
                            slotsFixed++;
                        }
                    }

                    if (rendererChanged)
                    {
                        r.sharedMaterials = mats;
                        prefabChanged = true;
                    }
                }

                if (prefabChanged)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabsChanged++;
                }
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();

            var sb = new StringBuilder();
            foreach (var kv in shaderCounts) sb.AppendLine($"  {kv.Value,4}x  {kv.Key}");
            shaderReport = sb.ToString();
            return prefabsChanged;
        }

        static Material GetReplacement(Material src, Dictionary<Material, Material> cache,
                                       HashSet<Material> upgraded, Material fallback, ref int matsUpgraded)
        {
            if (src == null) return fallback;

            string assetPath = AssetDatabase.GetAssetPath(src);
            bool isStandaloneAsset = !string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".mat");

            if (isStandaloneAsset)
            {
                // Editable material asset → upgrade it in place so references keep working.
                if (upgraded.Add(src))
                {
                    UpgradeInPlace(src);
                    matsUpgraded++;
                }
                return src;
            }

            // Embedded (model) material → can't edit it, so make a URP copy and use that.
            if (cache.TryGetValue(src, out Material copy)) return copy;
            copy = CreateUrpCopy(src);
            cache[src] = copy;
            matsUpgraded++;
            return copy;
        }

        static bool IsBroken(Material m)
        {
            if (m == null) return true;
            Shader s = m.shader;
            if (s == null) return true;

            string n = s.name;
            if (n.StartsWith("Universal Render Pipeline/")) return false; // already URP – fine
            if (n.StartsWith("Standard")) return true;
            if (n.Contains("InternalErrorShader")) return true;
            if (n.StartsWith("Legacy Shaders/")) return true;
            if (n == "Diffuse" || n == "Bumped Diffuse" || n == "Specular" || n == "VertexLit" ||
                n == "Mobile/Diffuse" || n == "Mobile/Bumped Diffuse" || n == "Mobile/VertexLit")
                return true;

            // Unlit / Sprites / Skybox / TextMeshPro etc. render fine in URP – leave them alone.
            return false;
        }

        static void UpgradeInPlace(Material m)
        {
            Color c = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            Texture tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;

            m.shader = UrpLit;
            SetBaseColor(m, c);
            SetBaseMap(m, tex);
            EditorUtility.SetDirty(m);
        }

        static Material CreateUrpCopy(Material src)
        {
            EnsureFolder(MatFolder);

            var copy = new Material(UrpLit);
            Color c = src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white;
            Texture tex = src.HasProperty("_MainTex") ? src.GetTexture("_MainTex") : null;
            SetBaseColor(copy, c);
            SetBaseMap(copy, tex);

            string baseName = MakeSafe(string.IsNullOrEmpty(src.name) ? "mat" : src.name);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{MatFolder}/urp_{baseName}.mat");
            copy.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }

        static void RecordShader(SortedDictionary<string, int> counts, Material m)
        {
            string key = m == null ? "<null material>"
                       : (m.shader == null ? "<null shader>" : m.shader.name);
            counts.TryGetValue(key, out int c);
            counts[key] = c + 1;
        }

        static void SetBaseColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        static void SetBaseMap(Material m, Texture tex)
        {
            if (tex == null) return;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
        }

        static string MakeSafe(string s)
        {
            foreach (char ch in Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_');
            return s.Replace(' ', '_');
        }

        /// <summary>Creates a folder (and any missing parents) in the AssetDatabase.</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
