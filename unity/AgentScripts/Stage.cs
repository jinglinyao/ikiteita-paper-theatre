using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

/// The life-size paper stage for the first pipeline pass (shots S16-S17), built from nothing by code:
/// lacquered floor, two cut-paper facades with real window holes and one lit pane behind each, a far row, a sky cloth,
/// a follow spot, a cool back light, the singer. Stage axes: x across, z upstage from the proscenium, y up.
public static class Stage
{
    const string GEN = "Assets/Gen", PAPER = "Assets/Paper/stage/";
    static StringBuilder log;
    static readonly Vector3 MARK = new Vector3(-0.5f, 0, 1.3f);                 // where she stands in the follow spot
    public static float SPOT = 13f, RIM = 7f, WASH = 9f;
    static readonly float[] PANE_ON = { 55.48f, 56.55f, 57.42f, 58.50f, 59.56f, 60.63f };
    static readonly string[] PANE_TEX = { "pane_bike", "pane_clock", "pane_book", "pane_umbrella", "pane_lamp", "pane_bag" };

    // ---------------------------------------------------------------- helpers
    static void EnsureDir(string p) { if (!AssetDatabase.IsValidFolder(p)) { var parent = Path.GetDirectoryName(p).Replace('\\', '/'); EnsureDir(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(p)); } }

    static Texture2D Tex(string path, bool normal = false, bool alpha = false, bool repeat = true, bool linear = false, int max = 4096)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { log.AppendLine("MISSING texture " + path); return null; }
        bool dirty = false;
        var want = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (ti.textureType != want) { ti.textureType = want; dirty = true; }
        if (ti.alphaIsTransparency != alpha) { ti.alphaIsTransparency = alpha; dirty = true; }
        var wm = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        if (ti.wrapMode != wm) { ti.wrapMode = wm; dirty = true; }
        if (ti.maxTextureSize != max) { ti.maxTextureSize = max; dirty = true; }
        if (!normal && ti.sRGBTexture == linear) { ti.sRGBTexture = !linear; dirty = true; }
        if (ti.anisoLevel != 8) { ti.anisoLevel = 8; dirty = true; }
        if (dirty) ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material Save(Material m, string name)
    {
        EnsureDir(GEN); string p = GEN + "/" + name + ".mat";
        var old = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (old != null) { old.shader = m.shader; old.CopyPropertiesFromMaterial(m); old.shaderKeywords = m.shaderKeywords; old.renderQueue = m.renderQueue; old.globalIlluminationFlags = m.globalIlluminationFlags; EditorUtility.SetDirty(old); return old; }
        AssetDatabase.CreateAsset(m, p); return m;
    }

    static Material Lit(string name, Texture2D albedo, Texture2D normal, Vector2 tiling, Color tint, float smooth)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", tint); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0);
        if (albedo != null) { m.SetTexture("_BaseMap", albedo); m.SetTextureScale("_BaseMap", tiling); }
        if (normal != null) { m.SetTexture("_BumpMap", normal); m.SetTextureScale("_BumpMap", tiling); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 0.7f); }
        return Save(m, name);
    }

    static Material Paper(string name, Texture2D albedo, Texture2D emit, Color emission, bool clip)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetTexture("_BaseMap", albedo); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.05f); m.SetFloat("_Metallic", 0); m.SetFloat("_Cull", 0);
        if (clip) { m.SetFloat("_AlphaClip", 1); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cutoff", 0.5f); m.renderQueue = 2450; m.SetOverrideTag("RenderType", "TransparentCutout"); }
        if (emit != null) { m.SetTexture("_EmissionMap", emit); m.SetColor("_EmissionColor", emission); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive; }
        return Save(m, name);
    }

    static GameObject Quad(string name, Transform parent, Vector3 pos, Vector2 size, Material mat, Vector3 euler = default, bool shadows = true)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Quad); g.name = name; Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localEulerAngles = euler; g.transform.localScale = new Vector3(size.x, size.y, 1);
        var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = shadows ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off; r.receiveShadows = true;
        return g;
    }

    static Light AddLight(string name, Transform parent, LightType type, Vector3 pos, Vector3 lookAt, Color col, float intensity, float range, float spot = 60, bool shadows = true)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = pos;
        if (type != LightType.Point) g.transform.rotation = Quaternion.LookRotation(parent.TransformPoint(lookAt) - g.transform.position);
        var l = g.AddComponent<Light>(); l.type = type; l.color = col; l.intensity = intensity; l.range = range; l.spotAngle = spot; l.innerSpotAngle = spot * 0.6f;
        l.shadows = shadows ? LightShadows.Soft : LightShadows.None; l.shadowStrength = 0.95f; l.shadowBias = 0.02f; l.shadowNormalBias = 0.2f;
        var ad = g.AddComponent<UniversalAdditionalLightData>(); ad.softShadowQuality = SoftShadowQuality.High;
        return l;
    }

    static UniversalRenderPipelineAsset Urp()
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        return urp != null ? urp : QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
    }

    static void Pipeline()
    {
        var urp = Urp(); if (urp == null) { log.AppendLine("no URP asset active"); return; }
        var so = new SerializedObject(urp);
        void Set(string n, float v) { var p = so.FindProperty(n); if (p == null) { log.AppendLine("  (no prop " + n + ")"); return; } if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = v > 0.5f; else if (p.propertyType == SerializedPropertyType.Float) p.floatValue = v; else p.intValue = (int)v; }
        Set("m_SupportsHDR", 1); Set("m_MSAA", 4); Set("m_MainLightShadowsSupported", 1); Set("m_MainLightShadowmapResolution", 4096);
        Set("m_AdditionalLightsRenderingMode", 1); Set("m_AdditionalLightsPerObjectLimit", 8); Set("m_AdditionalLightShadowsSupported", 1); Set("m_AdditionalLightsShadowmapResolution", 4096);
        Set("m_ShadowDistance", 30); Set("m_ShadowCascadeCount", 2); Set("m_SoftShadowsSupported", 1); Set("m_SoftShadowQuality", 3);
        so.ApplyModifiedProperties(); EditorUtility.SetDirty(urp);
        log.AppendLine("URP asset: " + urp.name);
    }

    static VolumeProfile Post(string name)
    {
        EnsureDir(GEN); string p = GEN + "/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(p) != null) AssetDatabase.DeleteAsset(p);
        var prof = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(prof, p);
        var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.Neutral);                                   // softer shoulder than ACES: highlights roll off, colours stay
        var bl = prof.Add<Bloom>(true); bl.intensity.Override(0.55f); bl.threshold.Override(0.95f); bl.scatter.Override(0.78f); bl.tint.Override(new Color(1f, 0.82f, 0.62f)); bl.highQualityFiltering.Override(true);   // warm halation
        var ca = prof.Add<ColorAdjustments>(true); ca.postExposure.Override(0.0f); ca.contrast.Override(6f); ca.saturation.Override(4f);
        var vg = prof.Add<Vignette>(true); vg.intensity.Override(0.26f); vg.smoothness.Override(0.5f);
        var fg = prof.Add<FilmGrain>(true); fg.type.Override(FilmGrainLookup.Thin1); fg.intensity.Override(0.20f); fg.response.Override(0.7f);
        var dof = prof.Add<DepthOfField>(true); dof.mode.Override(DepthOfFieldMode.Bokeh); dof.focusDistance.Override(2.3f); dof.aperture.Override(2.8f); dof.focalLength.Override(70f); dof.bladeCount.Override(9); dof.bladeCurvature.Override(0.9f);
        foreach (var c in prof.components) AssetDatabase.AddObjectToAsset(c, prof);
        EditorUtility.SetDirty(prof); return prof;
    }

    // ---------------------------------------------------------------- build
    const string ART = "Assets/Paper/stage2/";
    const float TILE = 0.6f;                       // one repeat of the 2K paper scan every 0.6 m: about 3400 texels a metre, sharp wherever the camera goes
    static readonly Dictionary<string, Material> kitM = new Dictionary<string, Material>();
    static System.Random rnd;

    static Material CardM(string name, Color tint, float smooth = 0.10f, int kind = 0)
    {
        if (kitM.TryGetValue(name, out var m0) && m0 != null) return m0;
        string b = kind == 1 ? "Assets/Textures/paper006/Paper006_2K-JPG_" : kind == 2 ? "Assets/Textures/cardboard004/Cardboard004_2K-JPG_" : "Assets/Textures/paper001/Paper001_2K-JPG_";
        var m = Lit("kit_" + name, Tex(b + "Color.jpg"), Tex(b + "NormalGL.jpg", true), Vector2.one, tint, smooth);
        m.SetFloat("_BumpScale", kind == 0 ? 1.8f : 1.0f); kitM[name] = m; return m;
    }

    /// a solid block of card; its texture coordinates come from its place in the parent, so neighbouring blocks share one sheet of paper
    static GameObject Box(string name, Transform parent, Vector3 c, Vector3 s, Material mat, Vector3 euler = default)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = c; g.transform.localEulerAngles = euler;
        float hx = s.x * 0.5f, hy = s.y * 0.5f, hz = s.z * 0.5f;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        void Face(Vector3 o, Vector3 ea, Vector3 eb, Vector3 U, Vector3 V)
        {
            int i = v.Count; Vector3 nrm = Vector3.Cross(ea, eb).normalized;
            foreach (var q in new[] { o, o + ea, o + ea + eb, o + eb }) { v.Add(q); n.Add(nrm); uv.Add(new Vector2(Vector3.Dot(q + c, U), Vector3.Dot(q + c, V)) / TILE); }
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }
        Face(new Vector3(-hx, -hy, -hz), new Vector3(0, s.y, 0), new Vector3(s.x, 0, 0), Vector3.right, Vector3.up);
        Face(new Vector3(hx, -hy, hz), new Vector3(0, s.y, 0), new Vector3(-s.x, 0, 0), Vector3.right, Vector3.up);
        Face(new Vector3(-hx, -hy, hz), new Vector3(0, s.y, 0), new Vector3(0, 0, -s.z), Vector3.forward, Vector3.up);
        Face(new Vector3(hx, -hy, -hz), new Vector3(0, s.y, 0), new Vector3(0, 0, s.z), Vector3.forward, Vector3.up);
        Face(new Vector3(-hx, hy, -hz), new Vector3(0, 0, s.z), new Vector3(s.x, 0, 0), Vector3.right, Vector3.forward);
        Face(new Vector3(-hx, -hy, hz), new Vector3(0, 0, -s.z), new Vector3(s.x, 0, 0), Vector3.right, Vector3.forward);
        var mesh = new Mesh { name = name }; mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
        return g;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 c, Vector3 scale, Material mat, Vector3 euler = default)
    {
        var g = GameObject.CreatePrimitive(type); g.name = name; Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false); g.transform.localPosition = c; g.transform.localScale = scale; g.transform.localEulerAngles = euler; g.GetComponent<MeshRenderer>().sharedMaterial = mat; return g;
    }
    static GameObject Cyl(string name, Transform parent, Vector3 c, float radius, float height, Material mat, Vector3 euler = default) { return Prim(PrimitiveType.Cylinder, name, parent, c, new Vector3(radius * 2, height * 0.5f, radius * 2), mat, euler); }
    static GameObject Ball(string name, Transform parent, Vector3 c, float radius, Material mat) { return Prim(PrimitiveType.Sphere, name, parent, c, Vector3.one * radius * 2, mat); }
    static GameObject Rod(string name, Transform parent, Vector3 a, Vector3 b, float radius, Material mat)
    {
        var g = Prim(PrimitiveType.Cylinder, name, parent, (a + b) * 0.5f, new Vector3(radius * 2, (b - a).magnitude * 0.5f, radius * 2), mat);
        g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized); return g;
    }

    /// a wall of card with real openings cut through it (lower-left corner at x0, y0; its face at z)
    static void Wall(Transform parent, float x0, float y0, float w, float h, float z, float thick, List<Rect> holes, Material mat)
    {
        var xs = new SortedSet<float> { x0, x0 + w }; var ys = new SortedSet<float> { y0, y0 + h };
        foreach (var r in holes) { xs.Add(r.xMin); xs.Add(r.xMax); ys.Add(r.yMin); ys.Add(r.yMax); }
        var X = xs.ToList(); var Y = ys.ToList();
        for (int j = 0; j + 1 < Y.Count; j++)
        {
            int run = -1;
            for (int i = 0; i <= X.Count - 1; i++)
            {
                bool solid = i + 1 < X.Count && !holes.Any(r => r.Contains(new Vector2((X[i] + X[i + 1]) * 0.5f, (Y[j] + Y[j + 1]) * 0.5f)));
                if (solid && run < 0) run = i;
                if (!solid && run >= 0) { Box("wall", parent, new Vector3((X[run] + X[i]) * 0.5f, (Y[j] + Y[j + 1]) * 0.5f, z + thick * 0.5f), new Vector3(X[i] - X[run], Y[j + 1] - Y[j], thick), mat); run = -1; }
            }
        }
    }

    /// frame, sill, glazing bars and the bare cut edge of one window opening
    static void WindowTrim(Transform parent, Rect r, float z, float thick, Material trim, Material core, int vbars, bool hbar, bool sill = true, bool lintel = false)
    {
        const float fw = 0.035f, fd = 0.022f;
        Box("frame_l", parent, new Vector3(r.xMin - fw * 0.5f, r.center.y, z - fd * 0.5f), new Vector3(fw, r.height + fw * 2, fd), trim);
        Box("frame_r", parent, new Vector3(r.xMax + fw * 0.5f, r.center.y, z - fd * 0.5f), new Vector3(fw, r.height + fw * 2, fd), trim);
        Box("frame_t", parent, new Vector3(r.center.x, r.yMax + fw * 0.5f, z - fd * 0.5f), new Vector3(r.width, fw, fd), trim);
        Box("frame_b", parent, new Vector3(r.center.x, r.yMin - fw * 0.5f, z - fd * 0.5f), new Vector3(r.width, fw, fd), trim);
        if (sill) Box("sill", parent, new Vector3(r.center.x, r.yMin - fw - 0.016f, z - 0.034f), new Vector3(r.width + 0.14f, 0.032f, 0.068f), trim);
        if (lintel) { Box("lintel", parent, new Vector3(r.center.x, r.yMax + fw + 0.022f, z - 0.03f), new Vector3(r.width + 0.16f, 0.044f, 0.06f), trim); Box("lintel2", parent, new Vector3(r.center.x, r.yMax + fw + 0.052f, z - 0.036f), new Vector3(r.width + 0.20f, 0.018f, 0.072f), trim); }
        for (int k = 1; k <= vbars; k++) Box("bar", parent, new Vector3(r.xMin + r.width * k / (vbars + 1), r.center.y, z + 0.012f), new Vector3(0.014f, r.height, 0.014f), trim);
        if (hbar) Box("bar_h", parent, new Vector3(r.center.x, r.yMin + r.height * 0.62f, z + 0.012f), new Vector3(r.width, 0.014f, 0.014f), trim);
        Box("cut_l", parent, new Vector3(r.xMin + 0.002f, r.center.y, z + thick * 0.5f), new Vector3(0.004f, r.height, thick), core); Box("cut_r", parent, new Vector3(r.xMax - 0.002f, r.center.y, z + thick * 0.5f), new Vector3(0.004f, r.height, thick), core);
        Box("cut_t", parent, new Vector3(r.center.x, r.yMax - 0.002f, z + thick * 0.5f), new Vector3(r.width, 0.004f, thick), core); Box("cut_b", parent, new Vector3(r.center.x, r.yMin + 0.002f, z + thick * 0.5f), new Vector3(r.width, 0.004f, thick), core);
    }

    /// the little room behind a window: four walls, a cut-paper silhouette standing in it, backlit parchment at the back, one small lamp
    static void Room(Transform parent, Rect r, float z, string sil, float silAspect, bool lit, List<Renderer> panes, List<Light> plights, int idx)
    {
        const float DEEP = 0.24f; float ww = r.width + 0.10f, hh = r.height + 0.10f;
        var room = new GameObject("Room" + idx).transform; room.SetParent(parent, false); room.localPosition = new Vector3(r.center.x, r.center.y, z);
        var wallM = CardM("room_wall", new Color(0.36f, 0.22f, 0.12f), 0.05f, 1);
        Box("rw_l", room, new Vector3(-ww * 0.5f, 0, DEEP * 0.5f), new Vector3(0.01f, hh, DEEP), wallM); Box("rw_r", room, new Vector3(ww * 0.5f, 0, DEEP * 0.5f), new Vector3(0.01f, hh, DEEP), wallM);
        Box("rw_t", room, new Vector3(0, hh * 0.5f, DEEP * 0.5f), new Vector3(ww, 0.01f, DEEP), wallM); Box("rw_b", room, new Vector3(0, -hh * 0.5f, DEEP * 0.5f), new Vector3(ww, 0.01f, DEEP), wallM);
        var parT = Tex(ART + "parchment.png", false, false, false);
        var back = Quad("Pane" + idx, room, new Vector3(0, 0, DEEP), new Vector2(ww, hh), Paper(lit ? "room_parchment" : "room_dark", parT, lit ? parT : null, Color.black, false), default, false);
        if (!lit) { var mpb = new MaterialPropertyBlock(); mpb.SetColor("_BaseColor", new Color(0.07f, 0.08f, 0.12f)); back.GetComponent<Renderer>().SetPropertyBlock(mpb); return; }
        panes.Add(back.GetComponent<Renderer>());
        float sh = Mathf.Min(r.height * 0.94f, r.width * 0.86f / silAspect), sw = sh * silAspect;
        Quad("Sil" + idx, room, new Vector3((idx % 2 == 0 ? -1 : 1) * r.width * 0.06f, -r.height * 0.5f + sh * 0.5f, DEEP * 0.5f), new Vector2(sw, sh), Paper("room_" + sil, Tex(ART + sil + ".png", false, true, false), null, Color.black, true));
        plights.Add(AddLight("PaneLight" + idx, room, LightType.Point, new Vector3(0, r.height * 0.2f, DEEP * 0.78f), Vector3.zero, new Color(1f, 0.70f, 0.36f), 0f, 0.9f, 60, false));
    }

    static void Balcony(Transform parent, Rect r, float z, Material slab, Material iron)
    {
        float w = r.width + 0.20f, d = 0.28f, y0 = r.yMin - 0.045f;
        Box("bal_slab", parent, new Vector3(r.center.x, y0 - 0.018f, z - d * 0.5f), new Vector3(w, 0.036f, d), slab);
        Box("bal_fascia", parent, new Vector3(r.center.x, y0 - 0.05f, z - d + 0.006f), new Vector3(w, 0.03f, 0.012f), slab);
        foreach (float yy in new[] { 0.045f, 0.32f }) { Box("rail", parent, new Vector3(r.center.x, y0 + yy, z - d + 0.012f), new Vector3(w, 0.016f, 0.016f), iron); Box("rail_s1", parent, new Vector3(r.center.x - w * 0.5f + 0.008f, y0 + yy, z - d * 0.5f), new Vector3(0.016f, 0.016f, d), iron); Box("rail_s2", parent, new Vector3(r.center.x + w * 0.5f - 0.008f, y0 + yy, z - d * 0.5f), new Vector3(0.016f, 0.016f, d), iron); }
        int nb = Mathf.RoundToInt(w / 0.055f);
        for (int k = 0; k <= nb; k++) Box("baluster", parent, new Vector3(r.center.x - w * 0.5f + 0.008f + (w - 0.016f) * k / nb, y0 + 0.18f, z - d + 0.012f), new Vector3(0.009f, 0.27f, 0.009f), iron);
        for (int k = 1; k <= 3; k++) foreach (float sx in new[] { -1f, 1f }) Box("baluster_s", parent, new Vector3(r.center.x + sx * (w * 0.5f - 0.008f), y0 + 0.18f, z - d + 0.012f + (d - 0.02f) * k / 4f), new Vector3(0.009f, 0.27f, 0.009f), iron);
    }

    /// a hipped roof in courses of tile: every course a step, every tile end a small barrel
    static void TileRoof(Transform parent, float w, float d, float y, Material tile, Material trim)
    {
        Box("eave", parent, new Vector3(w * 0.5f, y + 0.02f, d * 0.5f), new Vector3(w + 0.30f, 0.04f, d + 0.30f), trim);
        Box("eave2", parent, new Vector3(w * 0.5f, y - 0.012f, d * 0.5f), new Vector3(w + 0.20f, 0.024f, d + 0.20f), trim);
        for (int k = 0; k < 6; k++)
        {
            float cw = w + 0.26f - 0.30f * k, cd = d + 0.26f - 0.14f * k, cy = y + 0.065f + 0.058f * k; if (cw < 0.3f || cd < 0.1f) break;
            Box("course" + k, parent, new Vector3(w * 0.5f, cy, d * 0.5f), new Vector3(cw, 0.058f, cd), tile);
            int n = Mathf.RoundToInt(cw / 0.064f);
            for (int i = 0; i < n; i++) Cyl("tile", parent, new Vector3(w * 0.5f - cw * 0.5f + cw * (i + 0.5f) / n, cy + 0.024f, d * 0.5f - cd * 0.5f + 0.05f), 0.030f, 0.12f, tile, new Vector3(90, 0, 0));
        }
    }

    static void Shutter(Transform parent, Rect r, float z, Material slat, Material trim)
    {
        int n = Mathf.RoundToInt(r.height / 0.046f);
        for (int k = 0; k < n; k++) Box("slat", parent, new Vector3(r.center.x, r.yMin + r.height * (k + 0.5f) / n, z - (k % 2 == 0 ? 0.012f : 0.006f)), new Vector3(r.width, r.height / n * 0.92f, k % 2 == 0 ? 0.024f : 0.012f), slat);
        Box("sh_box", parent, new Vector3(r.center.x, r.yMax + 0.04f, z - 0.035f), new Vector3(r.width + 0.08f, 0.08f, 0.07f), trim);
        Box("sh_gl", parent, new Vector3(r.xMin - 0.015f, r.center.y, z - 0.016f), new Vector3(0.03f, r.height, 0.032f), trim); Box("sh_gr", parent, new Vector3(r.xMax + 0.015f, r.center.y, z - 0.016f), new Vector3(0.03f, r.height, 0.032f), trim);
        Box("sh_handle", parent, new Vector3(r.center.x, r.yMin + 0.06f, z - 0.03f), new Vector3(0.12f, 0.016f, 0.014f), trim);
    }

    static void Awning(Transform parent, float cx, float y, float w, float z, Material a, Material b)
    {
        var g = new GameObject("Awning").transform; g.SetParent(parent, false); g.localPosition = new Vector3(cx, y, z); g.localEulerAngles = new Vector3(24, 0, 0);
        int n = 9; for (int k = 0; k < n; k++) { Box("stripe", g, new Vector3(-w * 0.5f + w * (k + 0.5f) / n, 0, -0.18f), new Vector3(w / n, 0.012f, 0.36f), k % 2 == 0 ? a : b); Box("valance", g, new Vector3(-w * 0.5f + w * (k + 0.5f) / n, -0.035f, -0.36f), new Vector3(w / n, 0.07f, 0.008f), k % 2 == 0 ? a : b); }
    }

    static void Door(Transform parent, Rect r, float z, Material wood, Material trim, Material brass)
    {
        Box("door", parent, new Vector3(r.center.x, r.center.y, z + 0.05f), new Vector3(r.width, r.height, 0.02f), wood);
        for (int i = 0; i < 2; i++) for (int j = 0; j < 2; j++) Box("panel", parent, new Vector3(r.center.x + (i - 0.5f) * r.width * 0.46f, r.yMin + r.height * (j == 0 ? 0.26f : 0.70f), z + 0.036f), new Vector3(r.width * 0.34f, r.height * (j == 0 ? 0.34f : 0.36f), 0.008f), wood);
        Ball("knob", parent, new Vector3(r.xMax - 0.05f, r.yMin + r.height * 0.47f, z + 0.03f), 0.014f, brass);
        Box("step", parent, new Vector3(r.center.x, 0.025f, z - 0.10f), new Vector3(r.width + 0.16f, 0.05f, 0.20f), trim);
        Box("door_top", parent, new Vector3(r.center.x, r.yMax + 0.03f, z - 0.02f), new Vector3(r.width + 0.14f, 0.05f, 0.06f), trim);
    }

    static void Pipe(Transform parent, float x, float z, float y0, float y1, Material m)
    {
        Rod("pipe", parent, new Vector3(x, y0 + 0.10f, z), new Vector3(x, y1, z), 0.020f, m); Rod("pipe_elbow", parent, new Vector3(x, y0 + 0.11f, z), new Vector3(x, y0 + 0.02f, z - 0.09f), 0.020f, m);
        Box("hopper", parent, new Vector3(x, y1 + 0.03f, z), new Vector3(0.07f, 0.07f, 0.06f), m);
        for (int k = 0; k < 4; k++) Box("clip", parent, new Vector3(x, y0 + 0.3f + (y1 - y0 - 0.4f) * k / 3f, z + 0.012f), new Vector3(0.064f, 0.014f, 0.05f), m);
    }

    static Transform House(Transform root, string name, Vector3 corner, int style, List<Renderer> panes, List<Light> plights, Dictionary<string, float> silAspect, out Transform frontCentre)
    {
        var g = new GameObject(name).transform; g.SetParent(root, false); g.localPosition = corner;
        var core = CardM("core", new Color(0.86f, 0.80f, 0.68f), 0.05f, 1);
        var brass = CardM("brass", new Color(0.72f, 0.55f, 0.24f), 0.45f);
        const float T = 0.03f; var holes = new List<Rect>(); var lit = new List<bool>(); var bars = new List<int>(); var balc = new List<bool>();
        float W, H, D;
        Material wall, trim, iron = CardM("iron", new Color(0.20f, 0.23f, 0.27f), 0.20f);
        if (style == 0)
        {
            // the house with the six windows that light one by one: slate-blue card, terracotta roof, a shuttered shop
            W = 2.05f; H = 2.62f; D = 0.80f; wall = CardM("wall_slate", new Color(0.36f, 0.46f, 0.58f)); trim = CardM("trim_stone", new Color(0.70f, 0.70f, 0.64f));
            float[] rows = { 1.06f, 1.68f, 2.30f };
            foreach (float y in rows) { holes.Add(new Rect(0.34f, y - 0.20f, 0.52f, 0.40f)); lit.Add(true); bars.Add(1); balc.Add(false); holes.Add(new Rect(1.15f, y - 0.24f, 0.58f, 0.50f)); lit.Add(true); bars.Add(2); balc.Add(true); }
            var door = new Rect(1.28f, 0.0f, 0.40f, 0.72f); holes.Add(door);
            Wall(g, 0, 0, W, H, 0, T, holes, wall);
            Door(g, door, 0, CardM("wood", new Color(0.36f, 0.24f, 0.15f), 0.12f, 1), trim, brass);
            Shutter(g, new Rect(0.22f, 0.04f, 0.78f, 0.62f), 0, CardM("shutter", new Color(0.78f, 0.70f, 0.52f)), trim);
            Awning(g, 0.61f, 0.86f, 0.94f, -0.004f, CardM("awn_a", new Color(0.20f, 0.36f, 0.40f)), CardM("awn_b", new Color(0.82f, 0.80f, 0.70f)));
            TileRoof(g, W, D, H, CardM("tile", new Color(0.34f, 0.33f, 0.38f), 0.14f), trim);
            Cyl("tank", g, new Vector3(0.55f, H + 0.56f, 0.45f), 0.15f, 0.26f, CardM("tank", new Color(0.52f, 0.56f, 0.58f), 0.2f)); for (int k = 0; k < 4; k++) Box("tank_leg", g, new Vector3(0.55f + (k % 2 == 0 ? -0.1f : 0.1f), H + 0.38f, 0.45f + (k < 2 ? -0.1f : 0.1f)), new Vector3(0.02f, 0.12f, 0.02f), iron);
            Rod("antenna", g, new Vector3(1.62f, H + 0.30f, 0.5f), new Vector3(1.62f, H + 1.0f, 0.5f), 0.008f, iron); for (int k = 0; k < 4; k++) Rod("antenna_x", g, new Vector3(1.46f + 0.02f * k, H + 0.70f + 0.08f * k, 0.5f), new Vector3(1.78f - 0.02f * k, H + 0.70f + 0.08f * k, 0.5f), 0.005f, iron);
            Pipe(g, W - 0.07f, -0.03f, 0, H, CardM("pipe", new Color(0.30f, 0.36f, 0.40f), 0.25f));
            Box("sign", g, new Vector3(-0.09f, 1.45f, -0.12f), new Vector3(0.022f, 0.52f, 0.20f), CardM("sign", new Color(0.84f, 0.78f, 0.62f))); Box("sign_arm1", g, new Vector3(-0.045f, 1.66f, -0.12f), new Vector3(0.09f, 0.012f, 0.012f), iron); Box("sign_arm2", g, new Vector3(-0.045f, 1.24f, -0.12f), new Vector3(0.09f, 0.012f, 0.012f), iron);
            Box("meter", g, new Vector3(1.86f, 0.52f, -0.035f), new Vector3(0.14f, 0.20f, 0.07f), CardM("meter", new Color(0.62f, 0.64f, 0.62f), 0.2f)); Cyl("meter_dial", g, new Vector3(1.86f, 0.55f, -0.075f), 0.04f, 0.008f, core, new Vector3(90, 0, 0));
            Box("band1", g, new Vector3(W * 0.5f, 0.80f, -0.008f), new Vector3(W + 0.02f, 0.03f, 0.016f), trim);
        }
        else
        {
            // the older house across the street: grey-blue plaster, a cornice on brackets, a fire ladder; its windows stay dark
            W = 2.00f; H = 2.74f; D = 0.80f; wall = CardM("wall_grey", new Color(0.42f, 0.48f, 0.54f)); trim = CardM("trim_olive", new Color(0.50f, 0.50f, 0.42f));
            float[] rows = { 0.98f, 1.66f, 2.32f }; float[] cols = { 0.40f, 1.00f, 1.60f };
            foreach (float y in rows) foreach (float x in cols) { holes.Add(new Rect(x - 0.17f, y - 0.24f, 0.34f, 0.48f)); lit.Add(false); bars.Add(1); balc.Add(false); }
            holes.RemoveAt(0); lit.RemoveAt(0); bars.RemoveAt(0); balc.RemoveAt(0);
            var door = new Rect(0.22f, 0.0f, 0.36f, 0.78f); holes.Add(door);
            Wall(g, 0, 0, W, H, 0, T, holes, wall);
            Door(g, door, 0, CardM("wood_dark", new Color(0.24f, 0.26f, 0.22f), 0.12f, 1), trim, brass);
            Box("canopy", g, new Vector3(0.40f, 0.92f, -0.10f), new Vector3(0.52f, 0.02f, 0.22f), CardM("tile", new Color(0.34f, 0.33f, 0.38f), 0.14f), new Vector3(12, 0, 0));
            Box("cornice1", g, new Vector3(W * 0.5f, H + 0.03f, D * 0.5f), new Vector3(W + 0.20f, 0.06f, D + 0.20f), trim); Box("cornice2", g, new Vector3(W * 0.5f, H + 0.08f, D * 0.5f), new Vector3(W + 0.30f, 0.045f, D + 0.30f), CardM("roof_dark", new Color(0.30f, 0.22f, 0.18f)));
            for (int k = 0; k < 14; k++) Box("bracket", g, new Vector3(0.08f + (W - 0.16f) * k / 13f, H - 0.045f, -0.035f), new Vector3(0.05f, 0.09f, 0.07f), trim);
            Box("parapet", g, new Vector3(W * 0.5f, H + 0.16f, D * 0.5f), new Vector3(W + 0.02f, 0.12f, D + 0.02f), wall);
            foreach (float lx in new[] { -0.10f, -0.36f }) Rod("ladder", g, new Vector3(lx, 0.55f, -0.10f), new Vector3(lx, 2.72f, -0.10f), 0.010f, iron);
            for (int k = 0; k < 20; k++) Rod("rung", g, new Vector3(-0.10f, 0.6f + 0.105f * k, -0.10f), new Vector3(-0.36f, 0.6f + 0.105f * k, -0.10f), 0.007f, iron);
            foreach (float py in new[] { 1.36f, 2.04f }) { Box("landing", g, new Vector3(-0.23f, py, -0.13f), new Vector3(0.40f, 0.018f, 0.26f), iron); Box("landing_r", g, new Vector3(-0.23f, py + 0.26f, -0.25f), new Vector3(0.40f, 0.014f, 0.014f), iron); for (int k = 0; k < 6; k++) Box("landing_b", g, new Vector3(-0.42f + 0.076f * k, py + 0.13f, -0.25f), new Vector3(0.008f, 0.26f, 0.008f), iron); }
            Box("ac", g, new Vector3(1.30f, 0.34f, -0.07f), new Vector3(0.30f, 0.22f, 0.14f), CardM("ac", new Color(0.80f, 0.80f, 0.76f), 0.2f)); for (int k = 0; k < 6; k++) Box("ac_slat", g, new Vector3(1.30f, 0.27f + 0.028f * k, -0.143f), new Vector3(0.24f, 0.008f, 0.008f), iron);
            Box("mail1", g, new Vector3(0.74f, 0.56f, -0.03f), new Vector3(0.11f, 0.15f, 0.06f), CardM("mail", new Color(0.30f, 0.42f, 0.36f), 0.2f)); Box("mail2", g, new Vector3(0.87f, 0.56f, -0.03f), new Vector3(0.11f, 0.15f, 0.06f), CardM("meter", new Color(0.62f, 0.64f, 0.62f), 0.2f));
            Pipe(g, W - 0.06f, -0.03f, 0, H, CardM("pipe", new Color(0.30f, 0.36f, 0.40f), 0.25f));
        }
        // the rest of the box: sides, back, floors between the storeys
        Box("side_l", g, new Vector3(T * 0.5f, H * 0.5f, D * 0.5f + T * 0.5f), new Vector3(T, H, D - T), wall); Box("side_r", g, new Vector3(W - T * 0.5f, H * 0.5f, D * 0.5f + T * 0.5f), new Vector3(T, H, D - T), wall);
        Box("back", g, new Vector3(W * 0.5f, H * 0.5f, D - T * 0.5f), new Vector3(W - 2 * T, H, T), wall);
        string[] sil = { "sil_bike", "sil_clock", "sil_book", "sil_umbrella", "sil_lamp", "sil_bag" }; int idx = 0;
        for (int i = 0; i < lit.Count; i++)
        {
            WindowTrim(g, holes[i], 0, T, trim, core, bars[i], style == 0 && !balc[i], !balc[i], style == 1);
            if (balc[i]) Balcony(g, holes[i], 0, trim, iron);
            string sn = sil[idx % sil.Length]; if (!silAspect.ContainsKey(sn)) sn = "sil_clock";
            Room(g, holes[i], T, sn, silAspect[sn], lit[i], panes, plights, style * 10 + i); if (lit[i]) idx++;
        }
        frontCentre = new GameObject(name + "_front").transform; frontCentre.SetParent(g, false); frontCentre.localPosition = new Vector3(W * 0.5f, H * 0.5f, 0);
        return g;
    }

    static void FarRow(Transform root, float z)
    {
        var g = new GameObject("FarRow").transform; g.SetParent(root, false); g.localPosition = new Vector3(0, 0, z);
        var iron = CardM("iron", new Color(0.20f, 0.23f, 0.27f), 0.20f); float x = -2.7f; int k = 0; var tops = new List<Vector3>();
        while (x < 2.7f)
        {
            float w = 0.42f + 0.5f * (float)rnd.NextDouble(), h = 0.50f + 0.75f * (float)rnd.NextDouble(); float c = 0.26f + 0.10f * (float)rnd.NextDouble();
            var m = CardM("far" + (k % 4), new Color(c * 0.86f, c * 0.96f, c * 1.16f)); var roof = CardM("far_roof" + (k % 3), new Color(0.36f + 0.1f * (k % 3), 0.30f, 0.32f));
            Box("far_b", g, new Vector3(x + w * 0.5f, h * 0.5f, 0.15f), new Vector3(w, h, 0.30f), m); Box("far_r", g, new Vector3(x + w * 0.5f, h + 0.02f, 0.15f), new Vector3(w + 0.05f, 0.04f, 0.36f), roof);
            int cols = Mathf.Max(1, (int)(w / 0.16f)), rows = Mathf.Max(1, (int)((h - 0.15f) / 0.2f));
            for (int i = 0; i < cols; i++) for (int j = 0; j < rows; j++) if (rnd.NextDouble() < 0.5) Box("far_w", g, new Vector3(x + w * (i + 0.5f) / cols, 0.2f + (h - 0.3f) * (j + 0.5f) / rows, -0.002f), new Vector3(0.04f + 0.03f * (float)rnd.NextDouble(), 0.07f, 0.006f), CardM("far_win", new Color(0.06f, 0.07f, 0.11f)));
            if (k % 3 == 1) { Cyl("far_tank", g, new Vector3(x + w * 0.3f, h + 0.12f, 0.15f), 0.06f, 0.10f, roof); }
            if (k % 2 == 0) { var top = new Vector3(x + w + 0.03f, 1.45f, 0.05f); Rod("pole", g, new Vector3(top.x, 0, top.z), top, 0.012f, iron); Box("pole_x", g, top + Vector3.down * 0.08f, new Vector3(0.16f, 0.012f, 0.012f), iron); tops.Add(top + Vector3.down * 0.08f); }
            x += w + 0.02f + 0.05f * (float)rnd.NextDouble(); k++;
        }
        for (int i = 0; i + 1 < tops.Count; i++) foreach (float off in new[] { -0.07f, 0.07f }) { Vector3 a = tops[i] + Vector3.right * off, b = tops[i + 1] + Vector3.right * off, mid = (a + b) * 0.5f + Vector3.down * 0.07f; Rod("wire", g, a, mid, 0.0025f, iron); Rod("wire", g, mid, b, 0.0025f, iron); }
    }

    static void LampPost(Transform root, Vector3 at)
    {
        var g = new GameObject("LampPost").transform; g.SetParent(root, false); g.localPosition = at;
        var m = CardM("lamp_green", new Color(0.16f, 0.24f, 0.20f), 0.25f); var glass = CardM("lamp_glass", new Color(0.62f, 0.66f, 0.70f), 0.6f);
        Cyl("base", g, new Vector3(0, 0.16f, 0), 0.075f, 0.32f, m); Cyl("collar", g, new Vector3(0, 0.34f, 0), 0.09f, 0.03f, m); Rod("pole", g, new Vector3(0, 0.3f, 0), new Vector3(0, 2.95f, 0), 0.034f, m);
        Cyl("collar2", g, new Vector3(0, 2.2f, 0), 0.045f, 0.03f, m); Rod("arm", g, new Vector3(0, 2.80f, 0), new Vector3(0.34f, 2.98f, 0), 0.018f, m); Rod("arm2", g, new Vector3(0.34f, 2.98f, 0), new Vector3(0.34f, 2.90f, 0), 0.014f, m); Rod("stay", g, new Vector3(0, 2.6f, 0), new Vector3(0.22f, 2.92f, 0), 0.008f, m);
        Cyl("cap", g, new Vector3(0.34f, 2.89f, 0), 0.11f, 0.03f, m); Ball("globe", g, new Vector3(0.34f, 2.80f, 0), 0.085f, glass); Ball("finial", g, new Vector3(0, 2.98f, 0), 0.04f, m);
        Box("box", g, new Vector3(-0.06f, 1.5f, 0), new Vector3(0.09f, 0.22f, 0.10f), m);
    }

    static void Clouds(Transform root)
    {
        var paper = CardM("cloud", new Color(0.80f, 0.84f, 0.90f)); var str = CardM("string", new Color(0.75f, 0.72f, 0.64f));
        Vector3[] at = { new Vector3(-1.5f, 2.95f, 4.25f), new Vector3(0.6f, 3.25f, 4.35f), new Vector3(2.0f, 2.75f, 4.15f) };
        for (int c = 0; c < at.Length; c++)
        {
            var g = new GameObject("Cloud" + c).transform; g.SetParent(root, false); g.localPosition = at[c];
            int n = 5 + c; for (int i = 0; i < n; i++) { float r = 0.14f + 0.10f * (float)rnd.NextDouble(); Cyl("puff", g, new Vector3(-0.34f + 0.68f * i / (n - 1), 0.05f * (float)rnd.NextDouble() + (i % 2) * 0.06f, 0.006f * i), r, 0.008f, paper, new Vector3(90, 0, 0)); }
            Box("cloud_base", g, new Vector3(0, -0.07f, 0.02f), new Vector3(0.86f, 0.12f, 0.008f), paper);
            foreach (float sx in new[] { -0.22f, 0.22f }) Rod("string", g, new Vector3(sx, 0.1f, 0.01f), new Vector3(sx, 4.4f - at[c].y, 0.01f), 0.0025f, str);
        }
    }

    /// the proscenium, modelled: plinth with panels, paired columns, entablature with dentils, a segmental pediment, bulbs round the opening
    static void Proscenium(Transform root, List<Renderer> bulbs)
    {
        var g = new GameObject("Proscenium").transform; g.SetParent(root, false);
        var ivory = CardM("pros_ivory", new Color(0.84f, 0.78f, 0.64f)); var gold = CardM("pros_gold", new Color(0.74f, 0.56f, 0.24f), 0.35f); var shade = CardM("pros_shade", new Color(0.66f, 0.58f, 0.44f));
        const float OW = 4.04f, OH = 3.0f, FW = 6.0f;
        Box("apron", g, new Vector3(0, -0.52f, -0.20f), new Vector3(FW, 1.04f, 0.40f), ivory); Box("apron_top", g, new Vector3(0, -0.012f, -0.24f), new Vector3(FW + 0.08f, 0.04f, 0.50f), shade);
        for (int k = 0; k < 5; k++) { float px = -2.2f + 1.1f * k; Box("panel_f", g, new Vector3(px, -0.52f, -0.406f), new Vector3(0.92f, 0.62f, 0.012f), shade); Box("panel_i", g, new Vector3(px, -0.52f, -0.414f), new Vector3(0.80f, 0.50f, 0.012f), ivory); Cyl("rosette", g, new Vector3(px, -0.52f, -0.424f), 0.09f, 0.012f, gold, new Vector3(90, 0, 0)); }
        foreach (float sx in new[] { -1f, 1f })
        {
            float cx = sx * (OW * 0.5f + 0.49f);
            Box("pier", g, new Vector3(cx, OH * 0.5f + 0.1f, -0.13f), new Vector3(0.98f, OH + 0.2f, 0.26f), ivory);
            Box("pedestal", g, new Vector3(cx, 0.30f, -0.34f), new Vector3(0.86f, 0.60f, 0.34f), shade); Box("ped_cap", g, new Vector3(cx, 0.62f, -0.34f), new Vector3(0.92f, 0.05f, 0.40f), ivory);
            foreach (float ox in new[] { -0.21f, 0.21f })
            {
                Box("col_base", g, new Vector3(cx + ox, 0.69f, -0.34f), new Vector3(0.30f, 0.08f, 0.30f), ivory); Cyl("col_torus", g, new Vector3(cx + ox, 0.75f, -0.34f), 0.135f, 0.04f, gold);
                Cyl("column", g, new Vector3(cx + ox, 1.86f, -0.34f), 0.105f, 2.20f, ivory);
                for (int f = 0; f < 10; f++) { float a = f * Mathf.PI * 2 / 10f; Box("flute", g, new Vector3(cx + ox + Mathf.Sin(a) * 0.105f, 1.86f, -0.34f - Mathf.Cos(a) * 0.105f), new Vector3(0.016f, 2.10f, 0.008f), shade, new Vector3(0, -a * Mathf.Rad2Deg, 0)); }
                Cyl("col_neck", g, new Vector3(cx + ox, 2.98f, -0.34f), 0.13f, 0.05f, gold); Box("capital", g, new Vector3(cx + ox, 3.05f, -0.34f), new Vector3(0.32f, 0.09f, 0.32f), ivory);
            }
            Cyl("medallion", g, new Vector3(cx, 1.9f, -0.265f), 0.15f, 0.014f, gold, new Vector3(90, 0, 0)); Cyl("medallion_i", g, new Vector3(cx, 1.9f, -0.274f), 0.11f, 0.014f, shade, new Vector3(90, 0, 0));
        }
        Box("architrave", g, new Vector3(0, OH + 0.36f, -0.20f), new Vector3(FW, 0.32f, 0.40f), ivory); Box("frieze", g, new Vector3(0, OH + 0.64f, -0.18f), new Vector3(FW, 0.24f, 0.36f), shade);
        Box("plate", g, new Vector3(0, OH + 0.64f, -0.366f), new Vector3(2.4f, 0.17f, 0.014f), ivory); Box("plate_rim", g, new Vector3(0, OH + 0.64f, -0.362f), new Vector3(2.5f, 0.21f, 0.008f), gold);
        for (int k = 0; k < 44; k++) Box("dentil", g, new Vector3(-FW * 0.5f + 0.07f + (FW - 0.14f) * k / 43f, OH + 0.80f, -0.40f), new Vector3(0.075f, 0.07f, 0.06f), ivory);
        Box("cornice1", g, new Vector3(0, OH + 0.87f, -0.22f), new Vector3(FW + 0.10f, 0.06f, 0.48f), ivory); Box("cornice2", g, new Vector3(0, OH + 0.93f, -0.24f), new Vector3(FW + 0.22f, 0.06f, 0.56f), shade); Box("cornice3", g, new Vector3(0, OH + 0.985f, -0.26f), new Vector3(FW + 0.34f, 0.05f, 0.64f), ivory);
        // pediment: a low arc built of segments, a board behind it, a round ornament
        float pr = 5.2f, py0 = OH + 1.0f; int seg = 14; float span = Mathf.Asin((FW * 0.5f - 0.3f) / pr);
        for (int k = 0; k < seg; k++) { float a = -span + 2 * span * (k + 0.5f) / seg; Box("arc", g, new Vector3(Mathf.Sin(a) * pr, py0 + Mathf.Cos(a) * pr - Mathf.Cos(span) * pr + 0.05f, -0.26f), new Vector3(2 * span * pr / seg + 0.01f, 0.10f, 0.52f), ivory, new Vector3(0, 0, -a * Mathf.Rad2Deg)); }
        Box("tympanum", g, new Vector3(0, py0 + 0.22f, -0.10f), new Vector3(FW - 1.2f, 0.40f, 0.10f), shade);
        Cyl("orn1", g, new Vector3(0, py0 + 0.30f, -0.17f), 0.26f, 0.02f, gold, new Vector3(90, 0, 0)); Cyl("orn2", g, new Vector3(0, py0 + 0.30f, -0.185f), 0.19f, 0.02f, ivory, new Vector3(90, 0, 0)); Ball("orn3", g, new Vector3(0, py0 + 0.30f, -0.20f), 0.07f, gold);
        foreach (float sx in new[] { -1f, 1f }) { Box("urn_b", g, new Vector3(sx * 2.75f, OH + 1.08f, -0.26f), new Vector3(0.22f, 0.12f, 0.22f), ivory); Ball("urn", g, new Vector3(sx * 2.75f, OH + 1.26f, -0.26f), 0.12f, gold); Cyl("urn_n", g, new Vector3(sx * 2.75f, OH + 1.40f, -0.26f), 0.05f, 0.08f, gold); }
        // a scalloped valance under the beam, and bulbs round the opening
        for (int k = 0; k < 21; k++) Cyl("scallop", g, new Vector3(-OW * 0.5f + OW * (k + 0.5f) / 21f, OH + 0.045f, -0.05f), 0.10f, 0.012f, gold, new Vector3(90, 0, 0));
        var bulbM = new Material(Shader.Find("Universal Render Pipeline/Lit")); bulbM.SetColor("_BaseColor", new Color(1f, 0.9f, 0.7f)); bulbM.EnableKeyword("_EMISSION"); bulbM.SetColor("_EmissionColor", new Color(1f, 0.72f, 0.36f) * 0.35f); bulbM = Save(bulbM, "kit_bulb");
        var pts = new List<Vector3>(); for (int k = 0; k < 15; k++) pts.Add(new Vector3(-OW * 0.5f + 0.14f + (OW - 0.28f) * k / 14f, OH - 0.07f, -0.02f)); for (int k = 0; k < 7; k++) { pts.Add(new Vector3(-OW * 0.5f + 0.05f, 0.35f + 0.38f * k, -0.02f)); pts.Add(new Vector3(OW * 0.5f - 0.05f, 0.35f + 0.38f * k, -0.02f)); }
        foreach (var q in pts) { Cyl("socket", g, q + new Vector3(0, 0, 0.03f), 0.03f, 0.02f, gold, new Vector3(90, 0, 0)); bulbs.Add(Ball("bulb", g, q, 0.036f, bulbM).GetComponent<Renderer>()); }
        // footlights along the front of the stage
        for (int k = 0; k < 7; k++) { float fx = -1.8f + 0.6f * k; Box("foot_hood", g, new Vector3(fx, 0.07f, -0.40f), new Vector3(0.26f, 0.14f, 0.012f), gold, new Vector3(-18, 0, 0)); Ball("foot_bulb", g, new Vector3(fx, 0.06f, -0.34f), 0.03f, bulbM); }
        // the house curtain: a modelled drape (Sketchfab, fuglee, CC-BY) in red velvet
        var cp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/sf2_stage_curtains/scene.gltf");
        if (cp != null)
        {
            var cu = (GameObject)PrefabUtility.InstantiatePrefab(cp); cu.name = "Curtain"; cu.transform.SetParent(g, false); cu.transform.localScale = Vector3.one * 0.60f;
            var velvet = CardM("velvet", new Color(0.50f, 0.05f, 0.06f), 0.22f, 1); foreach (var r in cu.GetComponentsInChildren<Renderer>()) r.sharedMaterial = velvet;
            var bb = new Bounds(); bool first = true; foreach (var r in cu.GetComponentsInChildren<Renderer>()) { if (first) { bb = r.bounds; first = false; } else bb.Encapsulate(r.bounds); }
            cu.transform.position += new Vector3(-bb.center.x, OH + 0.62f - bb.max.y, 0.16f - bb.center.z);
            log.AppendLine($"curtain {bb.size.x:F2} x {bb.size.y:F2} x {bb.size.z:F2}");
        }
    }

    static void StageHouse(Transform root)
    {
        var g = new GameObject("StageHouse").transform; g.SetParent(root, false);
        var boards = Lit("kit_boards", Tex("Assets/Textures/tex_laminate_floor/laminate_floor_diff_2k.jpg"), Tex("Assets/Textures/tex_laminate_floor/laminate_floor_nor_gl_2k.jpg", true), new Vector2(0.30f, 0.30f), new Color(0.46f, 0.30f, 0.22f), 0.72f);
        var ply = Lit("kit_ply", Tex("Assets/Textures/tex_plywood/plywood_diff_2k.jpg"), Tex("Assets/Textures/tex_plywood/plywood_nor_gl_2k.jpg", true), new Vector2(0.6f, 0.6f), new Color(0.80f, 0.70f, 0.56f), 0.2f);
        Box("floor", g, new Vector3(0, -0.04f, 2.5f), new Vector3(6.0f, 0.08f, 5.4f), boards);
        foreach (float sx in new[] { -2.85f, 2.85f }) foreach (float sz in new[] { 0.35f, 5.05f }) Box("post", g, new Vector3(sx, 2.6f, sz), new Vector3(0.12f, 5.2f, 0.12f), ply);
        foreach (float sx in new[] { -2.85f, 2.85f }) Box("beam_s", g, new Vector3(sx, 5.14f, 2.7f), new Vector3(0.12f, 0.12f, 4.8f), ply);
        foreach (float sz in new[] { 0.35f, 5.05f }) Box("beam_x", g, new Vector3(0, 5.14f, sz), new Vector3(5.8f, 0.12f, 0.12f), ply);
        foreach (float sz in new[] { 1.0f, 2.6f, 3.4f, 4.3f }) Rod("batten", g, new Vector3(-2.85f, 4.4f, sz), new Vector3(2.85f, 4.4f, sz), 0.03f, ply);
        Box("backwall", g, new Vector3(0, 2.2f, 5.2f), new Vector3(6.0f, 4.6f, 0.04f), CardM("backwall", new Color(0.05f, 0.05f, 0.07f)));
    }

    static void Ssao(float intensity, float radius)
    {
        foreach (var path in new[] { "Assets/Settings/PC_Renderer.asset" })
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o != null && o.name == "ScreenSpaceAmbientOcclusion")
            {
                var so = new SerializedObject(o); var a = so.FindProperty("m_Settings.Intensity"); var b = so.FindProperty("m_Settings.Radius"); var c = so.FindProperty("m_Settings.DirectLightingStrength");
                if (a != null) a.floatValue = intensity; if (b != null) b.floatValue = radius; if (c != null) c.floatValue = 0.45f; so.ApplyModifiedProperties(); EditorUtility.SetDirty(o);
                log.AppendLine("contact shading: intensity " + (a != null ? a.floatValue.ToString() : "?") + " radius " + (b != null ? b.floatValue.ToString() : "?"));
            }
    }

    public static string Build()
    {
        log = new StringBuilder(); rnd = new System.Random(20261113); kitM.Clear();
        AssetDatabase.Refresh();
        var silAspect = new Dictionary<string, float>(); var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var ln in File.ReadAllLines(ART + "stage_art.txt")) { var q = ln.Split(' '); if (q[0] == "sil") silAspect[q[1]] = float.Parse(q[2], inv); }
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MV/Characters/lin.vrm") == null) { AssetDatabase.ImportAsset("Assets/MV/Characters/lin.vrm", ImportAssetOptions.ForceUpdate); }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Pipeline(); Ssao(1.6f, 0.22f);
        RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.020f, 0.028f, 0.055f);
        RenderSettings.fog = false; RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        var root = new GameObject("Stage").transform;

        StageHouse(root);
        var bulbs = new List<Renderer>(); Proscenium(root, bulbs);
        // sky cloth: a crisp sheet with pin-prick stars, lit from behind; cut-paper clouds hang in front of it on strings
        var skyT = Tex(ART + "sky_crisp.png", false, false, false, false, 8192);
        var skyM = Paper("stage_sky3", skyT, skyT, Color.white * 1.0f, false);
        Quad("SkyDrop", root, new Vector3(0, 2.2f, 4.9f), new Vector2(5.9f, 4.4f), skyM, default, false);
        // the sky cloth wraps round the sides like a cyclorama, so that a sight line past the end of a house still ends on sky
        Quad("SkyWrapR", root, new Vector3(3.75f, 2.2f, 3.515f), new Vector2(3.2f, 4.4f), skyM, new Vector3(0, 60, 0), false);
        Quad("SkyWrapL", root, new Vector3(-3.75f, 2.2f, 3.515f), new Vector2(3.2f, 4.4f), skyM, new Vector3(0, -60, 0), false);
        Clouds(root); FarRow(root, 4.2f);
        var panes = new List<Renderer>(); var plights = new List<Light>(); var dummyP = new List<Renderer>(); var dummyL = new List<Light>();
        Transform front, frontL;
        House(root, "HouseR", new Vector3(0.50f, 0, 3.0f), 0, panes, plights, silAspect, out front);
        House(root, "HouseL", new Vector3(-2.62f, 0, 3.15f), 1, dummyP, dummyL, silAspect, out frontL);
        LampPost(root, new Vector3(0.05f, 0, 2.30f));
        Box("kerb", root, new Vector3(0, 0.02f, 3.0f), new Vector3(5.6f, 0.04f, 0.9f), CardM("kerb", new Color(0.52f, 0.52f, 0.50f)));

        AddLight("FollowSpot", root, LightType.Spot, new Vector3(-2.77f, 1.40f, -1.23f), MARK + Vector3.up * 1.38f, new Color(1f, 0.93f, 0.82f), SPOT, 9f, 17f);
        AddLight("BackCool", root, LightType.Spot, new Vector3(0.95f, 3.3f, 3.5f), MARK + Vector3.up * 1.35f, new Color(0.50f, 0.66f, 1f), RIM, 8f, 26f);
        AddLight("SceneryWash", root, LightType.Spot, new Vector3(-0.6f, 3.9f, 0.2f), new Vector3(0.2f, 1.2f, 3.4f), new Color(0.36f, 0.48f, 0.95f), WASH, 10f, 92f, true);
        var house = AddLight("HouseLight", root, LightType.Spot, new Vector3(0.9f, 2.4f, -6.5f), new Vector3(0, 2.2f, 0), new Color(1f, 0.82f, 0.60f), 30f, 16f, 66f, true); house.enabled = false;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MV/Characters/lin.vrm");
        StagePerformer sp = null;
        if (prefab == null) log.AppendLine("lin.vrm did not import as a model");
        else
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = "Lin"; go.transform.position = MARK; go.transform.rotation = Quaternion.Euler(0, 202, 0);
            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var r in smrs) { r.forceMatrixRecalculationPerRender = true; r.updateWhenOffscreen = true; }
            sp = go.AddComponent<StagePerformer>();
            sp.animator = go.GetComponent<Animator>();
            sp.face = smrs.FirstOrDefault(x => x.sharedMesh.GetBlendShapeIndex("Fcl_MTH_A") >= 0);
            sp.lipJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/MV/Data/lipsync_song2_vroid.json");
            sp.CaptureRest(); sp.pose = 1; sp.poseStart = 55.24f;
        }

        var prof = Post("post_stage");
        var cg = new GameObject("ShotCam"); var cam = cg.AddComponent<Camera>();
        cam.usePhysicalProperties = true; cam.sensorSize = new Vector2(36f, 20.25f); cam.gateFit = Camera.GateFitMode.Horizontal; cam.focalLength = 70f;
        cam.nearClipPlane = 0.05f; cam.farClipPlane = 60; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.004f, 0.005f, 0.010f); cam.allowHDR = true; cam.allowMSAA = true;
        var ad = cg.AddComponent<UniversalAdditionalCameraData>(); ad.renderPostProcessing = true; ad.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; ad.renderShadows = true; ad.stopNaN = true;
        var vg = new GameObject("PostVolume"); var vol = vg.AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = prof;
        var gz = new GameObject("GazeTarget"); gz.transform.SetParent(cg.transform, false);
        if (sp != null) sp.gazeTarget = gz.transform;

        var rpg = new GameObject("StageReflection"); rpg.transform.position = new Vector3(0, 1.2f, 1.6f);
        var rp = rpg.AddComponent<ReflectionProbe>(); rp.mode = ReflectionProbeMode.Realtime; rp.refreshMode = ReflectionProbeRefreshMode.EveryFrame; rp.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        rp.resolution = 256; rp.size = new Vector3(7f, 6f, 10f); rp.boxProjection = true; rp.hdr = true; rp.clearFlags = ReflectionProbeClearFlags.SolidColor; rp.backgroundColor = Color.black; rp.intensity = 1f;

        var ck = new GameObject("StageClock").AddComponent<StageClock>();
        ck.panes = panes.ToArray(); ck.paneLights = plights.ToArray(); ck.paneOn = PANE_ON; ck.cam = cam; ck.volume = vol; ck.singer = sp; ck.nearR = front; ck.closePane = 4;
        if (sp != null) sp.Apply(57.0f);
        ck.Apply(57.0f);
        int parts = root.GetComponentsInChildren<MeshRenderer>().Length;
        log.AppendLine($"parts {parts}, lit windows {panes.Count}");
        EnsureDir("Assets/Scenes"); EditorSceneManager.SaveScene(scene, "Assets/Scenes/Stage.unity"); AssetDatabase.SaveAssets();
        log.AppendLine("scene saved: Assets/Scenes/Stage.unity");
        return log.ToString();
    }

    /// the whole theatre from the house, with the warm house light on the proscenium
    public static string Wide(float t, float dist, float focal, string name)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null) return "no stage";
        if (ck.singer != null) ck.singer.Apply(t);
        ck.Apply(t, 26);
        var hl = GameObject.Find("Stage").GetComponentsInChildren<Light>(true).FirstOrDefault(l => l.name == "HouseLight"); if (hl != null) hl.enabled = true;
        var cam = ck.cam; Vector3 p = new Vector3(1.1f, 1.9f, -dist), c = new Vector3(0, 2.0f, 1.2f);
        cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(c - p); cam.focalLength = focal;
        DepthOfField dof; if (ck.volume.profile.TryGet(out dof)) { dof.focusDistance.value = dist + 1.4f; dof.aperture.value = 8f; dof.focalLength.value = focal; }
        if (ck.singer != null) ck.singer.Apply(t);
        RenderTo(cam, 1920, 1080, "Capture/" + name + ".jpg");
        if (hl != null) hl.enabled = false;
        return "wrote Capture/" + name + ".jpg";
    }

    /// any view: camera position, what it looks at, focal length, f-stop
    public static string Free(float t, float px, float py, float pz, float lx, float ly, float lz, float focal, float fstop, string name)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null) return "no stage";
        if (ck.singer != null) ck.singer.Apply(t);
        ck.Apply(t, 26);
        var cam = ck.cam; Vector3 p = new Vector3(px, py, pz), c = new Vector3(lx, ly, lz);
        cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(c - p); cam.focalLength = focal;
        DepthOfField dof; if (ck.volume.profile.TryGet(out dof)) { dof.focusDistance.value = Vector3.Distance(p, c); dof.aperture.value = fstop; dof.focalLength.value = focal; }
        RenderTo(cam, 1920, 1080, "Capture/" + name + ".jpg");
        return "wrote Capture/" + name + ".jpg";
    }

    // ---------------------------------------------------------------- indirect light, baked into probes
    /// Direct light stays real-time (sharp shadows, lights that change); what the floor, the flats and the sky cloth throw back is
    /// baked once into an adaptive probe volume. on = false switches the baked light off again for the comparison still.
    public static string BakeGI(float spacing)
    {
        log = new StringBuilder();
        var urp = Urp(); var so = new SerializedObject(urp);
        var lp = so.FindProperty("m_LightProbeSystem"); if (lp != null) { lp.intValue = 1; so.ApplyModifiedProperties(); EditorUtility.SetDirty(urp); log.AppendLine("light probe system -> adaptive probe volumes"); }
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            GameObjectUtility.SetStaticEditorFlags(mr.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
            mr.receiveGI = ReceiveGI.LightProbes;
        }
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) l.lightmapBakeType = l.name.StartsWith("PaneLight") ? LightmapBakeType.Realtime : LightmapBakeType.Mixed;
        var ls = new LightingSettings { bakedGI = true, realtimeGI = false, mixedBakeMode = MixedLightingMode.IndirectOnly, lightmapper = LightingSettings.Lightmapper.ProgressiveGPU, indirectSampleCount = 512, directSampleCount = 32, maxBounces = 4 };
        Lightmapping.lightingSettings = ls;
        var old = GameObject.Find("ProbeVolume"); if (old != null) Object.DestroyImmediate(old);
        var pg = new GameObject("ProbeVolume"); pg.transform.position = new Vector3(0, 2.0f, 1.6f);
        var pv = pg.AddComponent<ProbeVolume>(); pv.mode = ProbeVolume.Mode.Local; pv.size = new Vector3(6.8f, 4.4f, 8.6f);
        var scene = EditorSceneManager.GetActiveScene(); EditorSceneManager.SaveScene(scene);
        string guid = AssetDatabase.AssetPathToGUID(scene.path);
        string sp = GEN + "/stage_bakingset.asset";
        var set = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(sp);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();
            var sd = typeof(ProbeVolumeBakingSet).GetMethod("SetDefaults", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (sd != null) sd.Invoke(set, null); else log.AppendLine("  (no SetDefaults)");
            AssetDatabase.CreateAsset(set, sp);
        }
        var sso = new SerializedObject(set); var md = sso.FindProperty("minDistanceBetweenProbes"); if (md != null) { md.floatValue = spacing; sso.ApplyModifiedProperties(); } else log.AppendLine("  (no minDistanceBetweenProbes)");
        log.AppendLine("scene added to baking set: " + set.TryAddScene(guid));
        EditorUtility.SetDirty(set); AssetDatabase.SaveAssets();
        var t0 = System.DateTime.Now;
        bool ok = Lightmapping.Bake();
        log.AppendLine($"bake returned {ok} after {(System.DateTime.Now - t0).TotalSeconds:F1} s");
        EditorSceneManager.SaveScene(scene);
        return log.ToString();
    }

    public static string GIState(bool on)
    {
        var urp = Urp(); var so = new SerializedObject(urp); var lp = so.FindProperty("m_LightProbeSystem"); lp.intValue = on ? 1 : 0; so.ApplyModifiedProperties(); EditorUtility.SetDirty(urp);
        return "light probe system = " + lp.intValue;
    }

    // ---------------------------------------------------------------- stills (edit mode)
    static void RenderTo(Camera cam, int w, int h, string path)
    {
        EnsureDir(GEN); string ap = GEN + $"/capture_{w}x{h}.renderTexture";
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(ap);
        if (rt == null) { rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 }; AssetDatabase.CreateAsset(rt, ap); }
        var keep = cam.targetTexture; cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); RenderTexture.active = null;
        cam.targetTexture = keep;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, path.EndsWith(".png") ? tex.EncodeToPNG() : tex.EncodeToJPG(95));
        Object.DestroyImmediate(tex);
    }

    /// pose the stage at a film time and write a still; shot 16 or 17, or 0 for the shot that time belongs to
    public static string Still(float t, int shot, string name)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null) return "no stage in the open scene";
        foreach (var rp in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) rp.RenderProbe();
        if (ck.singer != null) ck.singer.Apply(t);
        ck.Apply(t, shot);
        if (ck.singer != null) ck.singer.Apply(t);                               // the gaze target rides on the camera, which has just moved
        RenderTo(ck.cam, 1920, 1080, "Capture/" + name + ".jpg");
        return "wrote Capture/" + name + ".jpg at t " + t;
    }

    /// free camera still for checking a pose from the side
    public static string View(float t, float yaw, float dist, float height, float focal, string name)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null || ck.singer == null) return "no stage";
        ck.singer.Apply(t); ck.Apply(t, 21); ck.singer.Apply(t);
        var cam = ck.cam; Vector3 c = ck.singer.transform.position + Vector3.up * height;
        Vector3 p = c + Quaternion.Euler(0, yaw, 0) * Vector3.back * dist;
        cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(c - p); cam.focalLength = focal;
        DepthOfField dof; if (ck.volume.profile.TryGet(out dof)) { dof.focusDistance.value = dist; dof.aperture.value = 11f; dof.focalLength.value = focal; }
        RenderTo(cam, 1920, 1080, "Capture/" + name + ".jpg");
        return "wrote Capture/" + name + ".jpg";
    }

    /// real-mesh clearance: the nearest any vertex of the right hand and forearm comes to the torso, head and skirt at film time t (metres)
    public static string Clearance(float t)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null || ck.singer == null) return "no stage";
        var sp = ck.singer; sp.Apply(t);
        var an = sp.animator; var armBones = new HashSet<Transform>();
        foreach (var b in new[] { HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand }) foreach (var tr in an.GetBoneTransform(b).GetComponentsInChildren<Transform>()) armBones.Add(tr);
        var upperArms = new HashSet<Transform>();
        foreach (var b in new[] { HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm }) { var x = an.GetBoneTransform(b); if (x != null) foreach (var tr in x.GetComponentsInChildren<Transform>()) upperArms.Add(tr); }
        var arm = new List<Vector3>(); var body = new List<Vector3>();
        foreach (var smr in sp.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var m = new Mesh(); smr.BakeMesh(m, true); var v = m.vertices; var bw = smr.sharedMesh.boneWeights; var bones = smr.bones; var l2w = smr.transform.localToWorldMatrix;
            for (int i = 0; i < v.Length; i++)
            {
                var tb = bones[bw[i].boneIndex0]; Vector3 wpos = l2w.MultiplyPoint3x4(v[i]);
                if (armBones.Contains(tb)) arm.Add(wpos); else if (!upperArms.Contains(tb)) body.Add(wpos);
            }
            Object.DestroyImmediate(m);
        }
        float best = 1e9f; Vector3 pa = Vector3.zero, pb = Vector3.zero;
        foreach (var a in arm) foreach (var b in body) { float d = (a - b).sqrMagnitude; if (d < best) { best = d; pa = a; pb = b; } }
        return $"t {t}: right hand and forearm vertices {arm.Count}, body vertices {body.Count}, nearest distance {Mathf.Sqrt(best) * 100f:F1} cm (arm point {pa:F3}, body point {pb:F3})";
    }

    /// Is anything in frame that was never built? Renders shot `shot` at film time t against a magenta void with the finish switched off
    /// and counts the magenta pixels (nothing there at all) and the near-black ones (there, but no light reaches it).
    public static string Cover(float t, int shot, string name)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null) return "no stage";
        if (shot >= 0) { if (ck.singer != null) ck.singer.Apply(t); ck.Apply(t, shot); }          // shot -1: the camera as Free() left it
        var cam = ck.cam; var ad = cam.GetComponent<UniversalAdditionalCameraData>(); var keepC = cam.backgroundColor; bool keepP = ad.renderPostProcessing;
        cam.backgroundColor = new Color(1, 0, 1); ad.renderPostProcessing = false;
        string path = "Capture/cover/" + name + ".png"; RenderTo(cam, 480, 270, path);
        cam.backgroundColor = keepC; ad.renderPostProcessing = keepP;
        var tex = new Texture2D(2, 2); tex.LoadImage(File.ReadAllBytes(path)); var px = tex.GetPixels32(); int voidN = 0, dark = 0;
        foreach (var c in px) { if (c.r > 200 && c.b > 200 && c.g < 70) voidN++; else if (c.r < 6 && c.g < 6 && c.b < 8) dark++; }
        Object.DestroyImmediate(tex);
        Vector3 p = cam.transform.position, f = cam.transform.forward;
        bool houseSide = p.z < 2.2f && p.y < 2.9f && Mathf.Abs(p.x) < 2.0f && f.z > 0.80f;
        return $"shot {shot} t {t:F2}: void {100f * voidN / px.Length:F1}%  unlit {100f * dark / px.Length:F1}%  camera {p:F2} forward {f:F2}  house side {houseSide}";
    }

    // ---------------------------------------------------------------- the play-mode pass
    public static string SetupBaker(int warmup)
    {
        foreach (var old in Object.FindObjectsByType<SliceBaker>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null || ck.singer == null) return "no stage";
        var g = new GameObject("SliceBaker"); var sb = g.AddComponent<SliceBaker>(); sb.singer = ck.singer; sb.clock = ck; sb.cam = ck.cam; sb.warmup = warmup;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return "baker ready";
    }

    public static string Progress() { return $"playing {EditorApplication.isPlaying} written {SliceBaker.Written} done {SliceBaker.Done} {SliceBaker.Note}"; }
}
