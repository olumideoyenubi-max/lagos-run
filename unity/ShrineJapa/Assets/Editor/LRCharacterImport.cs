using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Import rules for the MakeHuman people in Assets/Resources/Characters: no animation clips, small textures, and
// materials that use the game's own lit shader (with alpha cut-out for hair, brows and lashes).
public class LRCharacterImport : AssetPostprocessor {
    const string Root = "Assets/Resources/Characters/";
    const string Cars = "Assets/Resources/Vehicles/", Props = "Assets/Resources/Props/", Kit = "Assets/Resources/Kit/", Surf = "Assets/Resources/Surfaces/";
    bool Mine { get { return assetPath.StartsWith(Root) || assetPath.StartsWith(Cars) || assetPath.StartsWith(Props) || assetPath.StartsWith(Kit) || assetPath.StartsWith(Surf); } }
    bool Person { get { return assetPath.StartsWith(Root); } }

    void OnPreprocessModel() {
        if (!Mine) return;
        ModelImporter mi = (ModelImporter)assetImporter;
        mi.animationType = Person ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
        // street pieces are merged into the street meshes at run time, so their vertex data must stay readable
        if (assetPath.StartsWith(Props) || assetPath.StartsWith(Kit)) mi.isReadable = true;
        mi.importAnimation = false; mi.importCameras = false; mi.importLights = false; mi.importBlendShapes = false;
        mi.meshCompression = assetPath.StartsWith(Kit) ? ModelImporterMeshCompression.Off : ModelImporterMeshCompression.Medium;
    }
    void OnPreprocessAudio() {
        if (!assetPath.StartsWith("Assets/Resources/Audio/music")) return;
        // the music is long: keep it compressed in memory rather than decoding five minutes of audio at startup
        AudioImporter ai = (AudioImporter)assetImporter;
        ai.defaultSampleSettings = new AudioImporterSampleSettings { loadType = AudioClipLoadType.CompressedInMemory, compressionFormat = AudioCompressionFormat.Vorbis, quality = 0.45f };
        ai.loadInBackground = true;
    }
    void OnPreprocessTexture() {
        if (!Mine) return;
        TextureImporter ti = (TextureImporter)assetImporter;
        bool skin = assetPath.ToLowerInvariant().Contains("skin");
        ti.maxTextureSize = skin ? 2048 : 1024;
        ti.mipmapEnabled = true;
        ti.anisoLevel = 4;
        // phones and browsers get half-size textures
        foreach (string platform in new[] { "WebGL", "Android", "iPhone" })
            ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = platform, overridden = true, maxTextureSize = skin ? 1024 : 512, format = TextureImporterFormat.Automatic });
    }
    public override uint GetVersion() { return 10; }

    // the Naija man's file names its textures differently from its materials
    static readonly string[] NaijaTextures = { "Body.body", "young_darkskinned_male_diffuse", "low-poly", "brown_eye", "eyebrow001", "eyebrow001", "eyelashes01", "eyelashes01",
        "teeth_base", "teeth", "full_beard", "Full_beard_black_diff", "tankshirt", "toptex1", "cargo_pants", "cargo_pants_diff" };

    void OnPreprocessMaterialDescription(UnityEditor.AssetImporters.MaterialDescription description, Material material, AnimationClip[] clips) {
        if (!Mine) return;
        // load by path, not by name: on a fresh project the models can be imported before the shader has been
        material.shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/LRLit.shader");
        Texture tex = null;
        UnityEditor.AssetImporters.TexturePropertyDescription tp;
        if (description.TryGetProperty("DiffuseColor", out tp)) tex = tp.texture;
        if (tex == null) {
            string dir = Path.GetDirectoryName(assetPath) + "/" + Path.GetFileNameWithoutExtension(assetPath) + ".fbm/";
            for (int i = 0; i < NaijaTextures.Length; i += 2)
                if (description.materialName.Contains(NaijaTextures[i])) tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + NaijaTextures[i + 1] + ".png");
        }
        // props whose textures the converter did not link: look in the model's texture folder for one named after the material
        if (tex == null && assetPath.StartsWith(Props)) {
            string folder = Path.ChangeExtension(assetPath, ".fbm");
            if (Directory.Exists(folder))
                foreach (string f in Directory.GetFiles(folder)) {
                    string n = Path.GetFileNameWithoutExtension(f);
                    if (!f.EndsWith(".meta") && n.StartsWith(description.materialName.Replace("_baseColor", "")) ) { tex = AssetDatabase.LoadAssetAtPath<Texture2D>(f.Replace('\\', '/')); if (tex != null) break; }
                }
        }
        // vehicles carry one texture, saved beside the model under the same name
        if (tex == null && assetPath.StartsWith(Cars)) tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.ChangeExtension(assetPath, ".png"));
        bool car = assetPath.StartsWith(Cars);
        material.SetFloat("_Gloss", car ? 0.5f : 0.1f);
        material.SetFloat("_Rim", car ? 0.15f : 0.35f);
        material.SetTexture("_MainTex", tex);
        // untextured materials keep the colour they were given in the model
        Vector4 flat;
        material.SetColor("_Color", tex == null && !Person && description.TryGetProperty("DiffuseColor", out flat) ? new Color(flat.x, flat.y, flat.z, 1) : Color.white);
        bool alpha = false;
        if (tex != null) { TextureImporter ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter; alpha = ti != null && ti.DoesSourceTextureHaveAlpha(); }
        // no texture found: hide the piece rather than show untextured hair cards
        material.SetFloat("_Cutoff", !Person ? (alpha && !car ? 0.4f : 0) : tex == null ? 2 : alpha ? 0.4f : 0);
        material.SetFloat("_Cull", alpha || car ? 0 : 2);
    }

    // Unity -batchmode -quit -projectPath <path> -executeMethod LRCharacterImport.Dump
    public static void Dump() {
        StringBuilder sb = new StringBuilder();
        foreach (GameObject go in Resources.LoadAll<GameObject>("Characters")) {
            sb.AppendLine("== " + go.name + " scale " + go.transform.localScale + " rot " + go.transform.localEulerAngles);
            foreach (SkinnedMeshRenderer r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
                string mats = ""; foreach (Material m in r.sharedMaterials) mats += (m == null ? "null" : m.name + "[" + (m.mainTexture == null ? "-" : m.mainTexture.name) + " cut " + (m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : -1) + " " + m.shader.name + "]") + " ";
                sb.AppendLine("  skinned " + r.name + " verts " + r.sharedMesh.vertexCount + " tris " + r.sharedMesh.triangles.Length / 3 + " bones " + r.bones.Length + " bounds " + r.bounds.center + " " + r.bounds.size + " root " + (r.rootBone ? r.rootBone.name : "-") + " mats " + mats);
            }
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true)) sb.AppendLine("  static " + r.name);
            if (go.name == "Tunde" || go.name == "Naija") Walk(go.transform, 1, sb);
        }
        foreach (GameObject go in Resources.LoadAll<GameObject>("Vehicles"))
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true)) {
                Mesh m = r.GetComponent<MeshFilter>().sharedMesh;
                sb.AppendLine("== vehicle " + go.name + " bounds " + r.bounds.center.ToString("F2") + " " + r.bounds.size.ToString("F2") + " tris " + m.triangles.Length / 3 + " mat " + r.sharedMaterial.shader.name + " tex " + (r.sharedMaterial.mainTexture ? r.sharedMaterial.mainTexture.name : "-"));
            }
        File.WriteAllText("Logs/characters.txt", sb.ToString());
        Debug.Log("LRCharacterImport.Dump done");
    }
    static void Walk(Transform t, int depth, StringBuilder sb) {
        if (t.name.Contains("thumb") || t.name.Contains("index") || t.name.Contains("middle") || t.name.Contains("ring") || t.name.Contains("pinky")) return;
        sb.AppendLine(new string(' ', depth * 2) + t.name + " pos " + t.localPosition.ToString("F3") + " rot " + t.localEulerAngles.ToString("F0") + " scl " + t.localScale.ToString("F2"));
        foreach (Transform c in t) Walk(c, depth + 1, sb);
    }
}
