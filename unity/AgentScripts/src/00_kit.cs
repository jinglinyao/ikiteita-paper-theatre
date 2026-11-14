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
public static class Sets
{
    const string GEN = "Assets/Gen", PAPER = "Assets/Paper/stage/";
    static StringBuilder log;
    static readonly Vector3 MARK = new Vector3(-0.5f, 0.008f, 1.3f);                 // where she stands in the follow spot
    public static float SPOT = 5f, RIM = 6f, WASH = 9f;
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

    public static bool FORWARD_PLUS = true;
    static void Pipeline()
    {
        EditorSettings.asyncShaderCompilation = false;                            // a still must never be taken with a placeholder shader
        var urp = Urp(); if (urp == null) { log.AppendLine("no URP asset active"); return; }
        var so = new SerializedObject(urp);
        void Set(string n, float v) { var p = so.FindProperty(n); if (p == null) { log.AppendLine("  (no prop " + n + ")"); return; } if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = v > 0.5f; else if (p.propertyType == SerializedPropertyType.Float) p.floatValue = v; else p.intValue = (int)v; }
        Set("m_SupportsHDR", 1); Set("m_MSAA", 4); Set("m_MainLightShadowsSupported", 1); Set("m_MainLightShadowmapResolution", 4096);
        Set("m_AdditionalLightsRenderingMode", 1); Set("m_AdditionalLightsPerObjectLimit", 8); Set("m_AdditionalLightShadowsSupported", 1); Set("m_AdditionalLightsShadowmapResolution", 4096);
        Set("m_ShadowDistance", 30); Set("m_ShadowCascadeCount", 2); Set("m_SoftShadowsSupported", 1); Set("m_SoftShadowQuality", 3);
        so.ApplyModifiedProperties(); EditorUtility.SetDirty(urp);
        log.AppendLine("URP asset: " + urp.name);
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Settings/PC_Renderer.asset")) if (o is UniversalRendererData)
        {
            var rs = new SerializedObject(o); var rm = rs.FindProperty("m_RenderingMode"); if (rm != null) { rm.intValue = FORWARD_PLUS ? 2 : 0; rs.ApplyModifiedProperties(); EditorUtility.SetDirty(o); log.AppendLine("rendering path: " + (FORWARD_PLUS ? "Forward+" : "Forward")); }
        }
    }

    static VolumeProfile Post(string name)
    {
        EnsureDir(GEN); string p = GEN + "/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(p) != null) AssetDatabase.DeleteAsset(p);
        var prof = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(prof, p);
        var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.Neutral);                                   // softer shoulder than ACES: highlights roll off, colours stay
        var bl = prof.Add<Bloom>(true); bl.intensity.Override(0.55f); bl.threshold.Override(0.95f); bl.scatter.Override(0.78f); bl.tint.Override(new Color(1f, 0.82f, 0.62f)); bl.highQualityFiltering.Override(true);   // warm halation
        var ca = prof.Add<ColorAdjustments>(true); ca.postExposure.Override(0.0f); ca.contrast.Override(6f); ca.saturation.Override(-14f);
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
        m.SetFloat("_BumpScale", kind == 0 ? 0.55f : 0.5f);
        // a second layer of paper fibre ten times finer (one repeat every 60 mm), so that the card stays sharp in a macro shot
        m.SetTexture("_DetailNormalMap", Tex("Assets/Textures/paper006/Paper006_2K-JPG_NormalGL.jpg", true)); m.SetTextureScale("_DetailAlbedoMap", new Vector2(10f, 10f)); m.SetFloat("_DetailNormalMapScale", 0.16f); m.EnableKeyword("_DETAIL_MULX2");
        EditorUtility.SetDirty(m); kitM[name] = m; return m;
    }

    /// A solid block of card. Its texture coordinates come from its place in the parent, so neighbouring blocks share one sheet of
    /// paper. Every edge of a block at least 10 mm thick is cut back by a 3 mm chamfer, which is what catches the light along it.
    public static float CHAMFER = 0.003f;
    static GameObject Box(string name, Transform parent, Vector3 c, Vector3 s, Material mat, Vector3 euler = default, float chamfer = -1f)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = c; g.transform.localEulerAngles = euler;
        var h = s * 0.5f; float mn = Mathf.Min(s.x, Mathf.Min(s.y, s.z)); float ch = chamfer >= 0f ? chamfer : (mn >= 0.010f ? Mathf.Min(CHAMFER, mn * 0.25f) : 0f);
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        Vector3[] AX = { Vector3.right, Vector3.up, Vector3.forward };
        Vector3[] UU = { Vector3.forward, Vector3.right, Vector3.right }, VV = { Vector3.up, Vector3.forward, Vector3.up };          // paper direction for faces across x, y, z
        Vector3 P(int a, int sx, int sy, int sz) { var q = new Vector3(sx * (h.x - ch), sy * (h.y - ch), sz * (h.z - ch)); q[a] = (a == 0 ? sx : a == 1 ? sy : sz) * h[a]; return q; }
        void Poly(int a, Vector3 outward, params Vector3[] q)
        {
            Vector3 nrm = Vector3.Cross(q[1] - q[0], q[2] - q[0]).normalized; bool flip = Vector3.Dot(nrm, outward) < 0; if (flip) nrm = -nrm;
            int i = v.Count; foreach (var p in q) { v.Add(p); n.Add(nrm); uv.Add(new Vector2(Vector3.Dot(p + c, UU[a]), Vector3.Dot(p + c, VV[a])) / TILE); }
            for (int k = 1; k + 1 < q.Length; k++) { if (flip) { t.Add(i); t.Add(i + k + 1); t.Add(i + k); } else { t.Add(i); t.Add(i + k); t.Add(i + k + 1); } }
        }
        int[] S(int a, int sa, int b, int sb, int cc, int sc) { var r = new int[3]; r[a] = sa; r[b] = sb; r[cc] = sc; return r; }
        for (int a = 0; a < 3; a++)
        {
            int b = (a + 1) % 3, cc = (a + 2) % 3;
            foreach (int sa in new[] { -1, 1 })
            {
                var q = new List<Vector3>(); foreach (var (sb, sc) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) }) { var r = S(a, sa, b, sb, cc, sc); q.Add(P(a, r[0], r[1], r[2])); }
                Poly(a, AX[a] * sa, q.ToArray());
            }
            if (ch <= 0) continue;
            foreach (int sa in new[] { -1, 1 }) foreach (int sb in new[] { -1, 1 })                                         // the chamfer along the edge between the faces across a and b
            {
                var r0 = S(a, sa, b, sb, cc, -1); var r1 = S(a, sa, b, sb, cc, 1);
                Poly(a, AX[a] * sa + AX[b] * sb, P(a, r0[0], r0[1], r0[2]), P(a, r1[0], r1[1], r1[2]), P(b, r1[0], r1[1], r1[2]), P(b, r0[0], r0[1], r0[2]));
            }
        }
        if (ch > 0) foreach (int sx in new[] { -1, 1 }) foreach (int sy in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Poly(0, new Vector3(sx, sy, sz), P(0, sx, sy, sz), P(1, sx, sy, sz), P(2, sx, sy, sz));
        var mesh = new Mesh { name = name }; mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        g.AddComponent<MeshFilter>().sharedMesh = mesh; var r2 = g.AddComponent<MeshRenderer>(); r2.sharedMaterial = mat; r2.shadowCastingMode = ShadowCastingMode.On; r2.receiveShadows = true;
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
                if (!solid && run >= 0) { Box("wall", parent, new Vector3((X[run] + X[i]) * 0.5f, (Y[j] + Y[j + 1]) * 0.5f, z + thick * 0.5f), new Vector3(X[i] - X[run], Y[j + 1] - Y[j], thick), mat, default, 0f); run = -1; }      // the pieces of one wall meet flush: no chamfer between them
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
        var parT = Tex(ART + "pane_washi.png", false, false, false);
        var back = Quad("Pane" + idx, room, new Vector3(0, 0, DEEP), new Vector2(ww, hh), Paper(lit ? "room_parchment" : "room_dark", parT, lit ? parT : null, Color.black, false), default, false);
        if (!lit) { var mpb = new MaterialPropertyBlock(); mpb.SetColor("_BaseColor", new Color(0.07f, 0.08f, 0.12f)); back.GetComponent<Renderer>().SetPropertyBlock(mpb); return; }
        panes.Add(back.GetComponent<Renderer>());
        float sh = Mathf.Min(r.height * 0.94f, r.width * 0.86f / silAspect), sw = sh * silAspect;
        var silM = Paper("room_" + sil, Tex(ART + sil + ".png", false, true, false), null, Color.black, true); silM.SetColor("_BaseColor", new Color(0.20f, 0.12f, 0.08f)); EditorUtility.SetDirty(silM);
        for (int L = 0; L < 3; L++) Quad((L == 0 ? "Sil" : "SilEdge") + idx, room, new Vector3((idx % 2 == 0 ? -1 : 1) * r.width * 0.06f, -r.height * 0.5f + sh * 0.5f, DEEP * 0.5f + 0.004f * L), new Vector2(sw, sh), silM);      // three sheets 4 mm apart: the card has an edge
        plights.Add(AddLight("PaneLight" + idx, room, LightType.Point, new Vector3(0, r.height * 0.2f, DEEP * 0.78f), Vector3.zero, new Color(1f, 0.70f, 0.36f), 0f, 0.9f, 60, false));
    }

    static void Balcony(Transform parent, Rect r, float z, Material slab, Material iron)
    {
        float w = r.width + 0.20f, d = 0.28f, y0 = r.yMin - 0.045f;
        Box("bal_slab", parent, new Vector3(r.center.x, y0 - 0.018f, z - d * 0.5f), new Vector3(w, 0.036f, d), slab);
        Box("bal_fascia", parent, new Vector3(r.center.x, y0 - 0.05f, z - d + 0.006f), new Vector3(w, 0.03f, 0.012f), slab);
        foreach (float yy in new[] { 0.045f, 0.32f }) { Box("rail", parent, new Vector3(r.center.x, y0 + yy, z - d + 0.012f), new Vector3(w, 0.016f, 0.016f), iron); Box("rail_s1", parent, new Vector3(r.center.x - w * 0.5f + 0.008f, y0 + yy, z - d * 0.5f), new Vector3(0.016f, 0.016f, d), iron); Box("rail_s2", parent, new Vector3(r.center.x + w * 0.5f - 0.008f, y0 + yy, z - d * 0.5f), new Vector3(0.016f, 0.016f, d), iron); }
        int nb = Mathf.RoundToInt(w / 0.11f);                 // every 110 mm, so that what stands behind the rail can be read
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

