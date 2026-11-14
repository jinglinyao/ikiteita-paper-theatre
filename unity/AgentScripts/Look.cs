using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

/// Look test for the paper-theatre film: a lamp-lit room with a CRT television, and inside the television a life-size
/// cut-paper street. Everything is built by code so it can be rebuilt from nothing.
public static class Look
{
    const string GEN = "Assets/Gen";
    static readonly Vector3 T = new Vector3(100, 0, 0);            // where the paper street stands (far from the room, its lights never reach)
    static StringBuilder log;

    // ---------------------------------------------------------------- helpers
    static void EnsureDir(string p) { if (!AssetDatabase.IsValidFolder(p)) { var parent = Path.GetDirectoryName(p).Replace('\\', '/'); EnsureDir(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(p)); } }

    static Texture2D Tex(string path, bool normal = false, bool alpha = false, bool repeat = true, bool linear = false)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { log.AppendLine("MISSING texture " + path); return null; }
        bool dirty = false;
        var want = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (ti.textureType != want) { ti.textureType = want; dirty = true; }
        if (ti.alphaIsTransparency != alpha) { ti.alphaIsTransparency = alpha; dirty = true; }
        var wm = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        if (ti.wrapMode != wm) { ti.wrapMode = wm; dirty = true; }
        if (ti.maxTextureSize != 4096) { ti.maxTextureSize = 4096; dirty = true; }
        if (!normal && ti.sRGBTexture == linear) { ti.sRGBTexture = !linear; dirty = true; }
        if (ti.anisoLevel != 8) { ti.anisoLevel = 8; dirty = true; }
        if (dirty) ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material Save(Material m, string name)
    {
        EnsureDir(GEN); string p = GEN + "/" + name + ".mat";
        var old = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (old != null) { old.shader = m.shader; old.CopyPropertiesFromMaterial(m); old.shaderKeywords = m.shaderKeywords; old.renderQueue = m.renderQueue; EditorUtility.SetDirty(old); return old; }
        AssetDatabase.CreateAsset(m, p); return m;
    }

    static Material Lit(string name, Texture2D albedo, Texture2D normal, Texture2D rough, Vector2 tiling, Color tint, float smooth = 0.2f)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", tint); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0);
        if (albedo != null) { m.SetTexture("_BaseMap", albedo); m.SetTextureScale("_BaseMap", tiling); }
        if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
        return Save(m, name);
    }

    /// cut paper: alpha-clipped, two-sided, matte; optional emission map (windows, bulbs)
    static Material Paper(string name, Texture2D albedo, Texture2D emit, float emission, Texture2D grainNormal)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetTexture("_BaseMap", albedo); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.06f); m.SetFloat("_Metallic", 0);
        m.SetFloat("_AlphaClip", 1); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cutoff", 0.5f); m.SetFloat("_Cull", 0); m.renderQueue = 2450;
        m.SetOverrideTag("RenderType", "TransparentCutout");
        if (emit != null) { m.SetTexture("_EmissionMap", emit); m.SetColor("_EmissionColor", Color.white * emission); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive; }
        if (grainNormal != null) { m.SetTexture("_DetailNormalMap", grainNormal); m.SetFloat("_DetailNormalMapScale", 0.6f); m.SetTextureScale("_DetailAlbedoMap", new Vector2(6, 6)); m.EnableKeyword("_DETAIL_MULX2"); }
        return Save(m, name);
    }

    static GameObject Quad(string name, Transform parent, Vector3 pos, Vector2 size, Material mat, Vector3 euler = default, bool shadows = true)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Quad); g.name = name; Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localEulerAngles = euler; g.transform.localScale = new Vector3(size.x, size.y, 1);
        var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = shadows ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off; r.receiveShadows = true;
        return g;
    }

    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = size; g.GetComponent<MeshRenderer>().sharedMaterial = mat; return g;
    }

    /// place an imported model so that the bottom centre of its bounds sits at `pos`
    static GameObject Prop(string path, string name, Transform parent, Vector3 pos, float yaw, float scale = 1f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { log.AppendLine("MISSING model " + path); return null; }
        var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab); g.name = name; g.transform.SetParent(parent, false);
        g.transform.localScale = Vector3.one * scale; g.transform.localEulerAngles = new Vector3(0, yaw, 0); g.transform.position = Vector3.zero;
        var rs = g.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) { log.AppendLine("no renderers in " + path); return g; }
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        g.transform.position = parent.TransformPoint(pos) - new Vector3(b.center.x, b.min.y, b.center.z);
        log.AppendLine($"  {name}: size {b.size.x:F2} x {b.size.y:F2} x {b.size.z:F2}");
        return g;
    }

    static Light AddLight(string name, Transform parent, LightType type, Vector3 pos, Vector3 lookAt, Color col, float intensity, float range, float spot = 60, bool shadows = true)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = pos;
        if (type != LightType.Point) g.transform.rotation = Quaternion.LookRotation(parent.TransformPoint(lookAt) - g.transform.position);
        var l = g.AddComponent<Light>(); l.type = type; l.color = col; l.intensity = intensity; l.range = range; l.spotAngle = spot; l.innerSpotAngle = spot * 0.55f;
        l.shadows = shadows ? LightShadows.Soft : LightShadows.None; l.shadowStrength = 0.92f; l.shadowBias = 0.03f; l.shadowNormalBias = 0.25f;
        var ad = g.AddComponent<UniversalAdditionalLightData>(); ad.softShadowQuality = SoftShadowQuality.High;
        return l;
    }

    // ---------------------------------------------------------------- pipeline settings
    static void Pipeline()
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) { log.AppendLine("no URP asset active"); return; }
        var so = new SerializedObject(urp);
        void Set(string n, float v) { var p = so.FindProperty(n); if (p == null) { log.AppendLine("  (no prop " + n + ")"); return; } if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = v > 0.5f; else if (p.propertyType == SerializedPropertyType.Float) p.floatValue = v; else p.intValue = (int)v; }
        Set("m_SupportsHDR", 1); Set("m_MSAA", 4); Set("m_MainLightShadowsSupported", 1); Set("m_MainLightShadowmapResolution", 4096);
        Set("m_AdditionalLightsRenderingMode", 1); Set("m_AdditionalLightsPerObjectLimit", 8); Set("m_AdditionalLightShadowsSupported", 1); Set("m_AdditionalLightsShadowmapResolution", 4096);
        Set("m_ShadowDistance", 30); Set("m_ShadowCascadeCount", 2); Set("m_SoftShadowsSupported", 1); Set("m_SoftShadowQuality", 3);
        so.ApplyModifiedProperties(); EditorUtility.SetDirty(urp);
        log.AppendLine("URP asset: " + urp.name + " (quality level " + QualitySettings.names[QualitySettings.GetQualityLevel()] + ")");
    }

    static VolumeProfile Post(string name, float exposure, float bloom, float focusDist, float aperture, float focal)
    {
        EnsureDir(GEN); string p = GEN + "/" + name + ".asset";
        var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(p);
        if (prof != null) AssetDatabase.DeleteAsset(p);
        prof = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(prof, p);
        var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
        var bl = prof.Add<Bloom>(true); bl.intensity.Override(bloom); bl.threshold.Override(1.0f); bl.scatter.Override(0.72f); bl.highQualityFiltering.Override(true);
        var ca = prof.Add<ColorAdjustments>(true); ca.postExposure.Override(exposure); ca.contrast.Override(12f); ca.saturation.Override(6f);
        var vg = prof.Add<Vignette>(true); vg.intensity.Override(0.32f); vg.smoothness.Override(0.45f);
        var fg = prof.Add<FilmGrain>(true); fg.type.Override(FilmGrainLookup.Thin1); fg.intensity.Override(0.22f); fg.response.Override(0.7f);
        var wb = prof.Add<WhiteBalance>(true); wb.temperature.Override(6f);
        var dof = prof.Add<DepthOfField>(true); dof.mode.Override(DepthOfFieldMode.Bokeh); dof.focusDistance.Override(focusDist); dof.aperture.Override(aperture); dof.focalLength.Override(focal); dof.bladeCount.Override(7);
        foreach (var c in prof.components) AssetDatabase.AddObjectToAsset(c, prof);
        EditorUtility.SetDirty(prof); return prof;
    }

    static Camera Cam(string name, Vector3 pos, Vector3 lookAt, float fov, VolumeProfile prof, int layerMaskVolume)
    {
        var g = new GameObject(name); var c = g.AddComponent<Camera>(); g.transform.position = pos; g.transform.rotation = Quaternion.LookRotation(lookAt - pos);
        c.fieldOfView = fov; c.nearClipPlane = 0.05f; c.farClipPlane = 80; c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(0.01f, 0.012f, 0.02f); c.allowHDR = true; c.allowMSAA = true;
        var ad = g.AddComponent<UniversalAdditionalCameraData>(); ad.renderPostProcessing = true; ad.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; ad.renderShadows = true; ad.volumeLayerMask = layerMaskVolume;
        ad.stopNaN = true;                                           // one NaN pixel (a toon material at a grazing angle) and bloom paints the whole frame black
        var vg = new GameObject(name + "_Volume"); vg.transform.SetParent(g.transform, false); vg.layer = name == "RoomCam" ? 6 : 7;
        var v = vg.AddComponent<Volume>(); v.isGlobal = true; v.sharedProfile = prof; v.priority = 1;
        return c;
    }

    // ---------------------------------------------------------------- the paper street
    static void Street(Transform root)
    {
        string P = "Assets/Paper/street/";
        var grain = Tex("Assets/Textures/paper001/Paper001_2K-JPG_NormalGL.jpg", normal: true);
        Material PM(string n, float em = 0f) => Paper("paper_" + n, Tex(P + n + ".png", alpha: true, repeat: false), em > 0 ? Tex(P + n + "_emit.png", repeat: false) : null, em, grain);
        // back to front. x: left/right, z: depth away from the audience
        Quad("sky", root, new Vector3(0, 3.0f, 8.0f), new Vector2(12f, 7.2f), Paper("paper_sky", Tex(P + "sky.png", alpha: true, repeat: false), Tex(P + "sky.png", alpha: true, repeat: false), 1.25f, grain), default, false);
        Quad("moon", root, new Vector3(0.75f, 4.55f, 7.6f), new Vector2(.9f, .9f), PM("moon", 1.8f));
        Quad("far_buildings", root, new Vector3(0, 2.0f, 6.0f), new Vector2(9f, 4f), PM("far_buildings", 1.5f));
        Quad("block_L", root, new Vector3(-3.25f, 2.3f, 3.6f), new Vector2(3.3f, 4.6f), PM("block_L", 2.2f), new Vector3(0, -8, 0));
        Quad("block_R", root, new Vector3(3.35f, 2.3f, 3.9f), new Vector2(3.3f, 4.6f), PM("block_R", 2.2f), new Vector3(0, 9, 0));
        Quad("lamp", root, new Vector3(-1.05f, 1.8f, 2.7f), new Vector2(1.2f, 3.6f), PM("lamp", 6f));
        Quad("crowd_A", root, new Vector3(-2.55f, 1.025f, 1.55f), new Vector2(3.42f, 2.05f), PM("crowd_A", 0.9f), new Vector3(0, -14, 0));
        Quad("crowd_B", root, new Vector3(2.6f, 1.025f, 1.25f), new Vector2(3.42f, 2.05f), PM("crowd_B", 0.9f), new Vector3(0, 16, 0));
        Quad("tape", root, new Vector3(0, 1.02f, 0.85f), new Vector2(7f, .12f), PM("tape"), new Vector3(0, 0, -1.2f));
        var gm = Paper("paper_ground", Tex(P + "ground.png", alpha: true, repeat: false), null, 0, Tex("Assets/Textures/tex_asphalt_02/asphalt_02_nor_gl_2k.jpg", normal: true)); gm.SetFloat("_DetailNormalMapScale", 1.1f);
        Quad("ground", root, new Vector3(0, 0, 4.0f), new Vector2(12f, 10f), gm, new Vector3(90, 0, 0), false);
        Quad("red_paper", root, new Vector3(-0.2f, 0.012f, 2.25f), new Vector2(.36f, .30f), PM("red_paper"), new Vector3(90, 20, 0));
        if (File.Exists("Assets/Paper/street/puppet_jinglin.png"))
        {
            var pm = Paper("paper_puppet", Tex(P + "puppet_jinglin.png", alpha: true, repeat: false), null, 0, grain);
            var pt = AssetDatabase.LoadAssetAtPath<Texture2D>(P + "puppet_jinglin.png"); float ph = 1.72f, pw = ph * pt.width / pt.height;
            Quad("puppet_jinglin", root, new Vector3(0.80f, ph / 2 + 0.01f, 1.95f), new Vector2(pw, ph), pm, new Vector3(0, 10, 0));
            var rod = Lit("rod", null, null, null, Vector2.one, new Color(.55f, .42f, .28f), 0.2f);
            Box("puppet_rod", root, new Vector3(0.82f, 0.55f, 2.0f), new Vector3(.018f, 1.1f, .018f), rod);
        }
        // sticks holding the flats up, seen when the camera drifts: plywood struts behind each block
        var wood = Lit("plywood", Tex("Assets/Textures/tex_plywood/plywood_diff_2k.jpg"), Tex("Assets/Textures/tex_plywood/plywood_nor_gl_2k.jpg", normal: true), null, new Vector2(1, 2), Color.white, 0.1f);
        Box("strut_L", root, new Vector3(-3.3f, 1.2f, 3.95f), new Vector3(.05f, 2.4f, .05f), wood).transform.localEulerAngles = new Vector3(18, 0, 0);
        Box("strut_R", root, new Vector3(3.4f, 1.2f, 4.25f), new Vector3(.05f, 2.4f, .05f), wood).transform.localEulerAngles = new Vector3(18, 0, 0);
        // light: the street lamp does the work; a cold wash from the moon side; the shop window spills warm
        AddLight("StreetLamp", root, LightType.Spot, new Vector3(-0.55f, 3.12f, 2.55f), new Vector3(-0.25f, 0f, 2.2f), new Color(1f, 0.83f, 0.56f), 60f, 9f, 96f);
        AddLight("ShopSpill", root, LightType.Point, new Vector3(-3.0f, 0.9f, 3.0f), Vector3.zero, new Color(1f, 0.78f, 0.45f), 2.2f, 4.5f, 0, false);
        // the theatre's own front light: a soft cool wash from above the audience, so each flat throws its shadow on the one behind
        var front = AddLight("FrontWash", root, LightType.Spot, new Vector3(1.6f, 4.6f, -4.2f), new Vector3(-0.2f, 1.2f, 3.2f), new Color(0.86f, 0.86f, 0.92f), 36f, 16f, 70f);
        front.shadowStrength = 0.8f;
        AddLight("FootWarm", root, LightType.Spot, new Vector3(-2.6f, 0.25f, -1.6f), new Vector3(0.5f, 1.3f, 2.5f), new Color(1f, 0.80f, 0.60f), 9f, 9f, 100f, false);
    }

    // ---------------------------------------------------------------- the room
    static Transform tvScreen;
    // where she sits: on the chair, facing the screen (the screen is at about (-1.42, 0.80, 0.63) and faces (0.74, 0, -0.67))
    static readonly Vector3 TVF = new Vector3(0.74f, 0, -0.67f).normalized;
    static readonly Vector3 SEAT = new Vector3(-1.42f, 0, 0.63f) + new Vector3(0.74f, 0, -0.67f).normalized * 1.30f;
    static readonly float SEAT_YAW = Mathf.Atan2(-0.74f, 0.67f) * Mathf.Rad2Deg;      // her yaw when she faces the screen
    const float CHAIR_YAW_OFFSET = 180f, CHAIR_SCALE = 0.92f, SEAT_BACK = 0.10f;
    static Vector3 SCR = new Vector3(0f, 0.046f, -0.004f);          // screen centre relative to the set's bounds centre / front face (tuned by eye, see TvClose)
    static Vector2 SCR_SIZE = new Vector2(0.305f, 0.229f);
    static void Room(Transform root, RenderTexture rt)
    {
        var floor = Lit("floor", Tex("Assets/Textures/tex_laminate_floor/laminate_floor_diff_2k.jpg"), Tex("Assets/Textures/tex_laminate_floor/laminate_floor_nor_gl_2k.jpg", normal: true), null, new Vector2(3, 3), new Color(.80f, .74f, .68f), 0.38f);
        var wall = Lit("wall", Tex("Assets/Textures/tex_beige_wall_001/beige_wall_001_diff_2k.jpg"), Tex("Assets/Textures/tex_beige_wall_001/beige_wall_001_nor_gl_2k.jpg", normal: true), null, new Vector2(2, 1.3f), new Color(.86f, .82f, .76f), 0.08f);
        var dark = Lit("darkwood", null, null, null, Vector2.one, new Color(.10f, .07f, .05f), 0.3f);
        Quad("floor", root, new Vector3(0, 0, 0), new Vector2(4.6f, 4.2f), floor, new Vector3(90, 0, 0), false);
        // back wall in four pieces round the window opening (x 0.55 +- 0.75, y 0.95 .. 2.0); each piece keeps the plaster at the same scale
        const float WX = 0.55f, WW = 1.5f, WY0 = 0.95f, WY1 = 2.0f, WZ = 1.9f;
        void WallPiece(string n, float x0, float x1, float y0, float y1)
        {
            var m = new Material(wall); m.SetTextureScale("_BaseMap", new Vector2((x1 - x0) / 2.3f, (y1 - y0) / 2.0f)); m.SetTextureOffset("_BaseMap", new Vector2(x0 / 2.3f, y0 / 2.0f));
            Quad(n, root, new Vector3((x0 + x1) / 2, (y0 + y1) / 2, WZ), new Vector2(x1 - x0, y1 - y0), Save(m, "wall_" + n));
        }
        WallPiece("back_l", -2.3f, WX - WW / 2, 0, 2.6f); WallPiece("back_r", WX + WW / 2, 2.3f, 0, 2.6f);
        WallPiece("back_below", WX - WW / 2, WX + WW / 2, 0, WY0); WallPiece("back_above", WX - WW / 2, WX + WW / 2, WY1, 2.6f);
        Quad("wall_left", root, new Vector3(-2.3f, 1.3f, 0), new Vector2(4.2f, 2.6f), wall, new Vector3(0, -90, 0));
        Quad("wall_right", root, new Vector3(2.3f, 1.3f, 0), new Vector2(4.2f, 2.6f), wall, new Vector3(0, 90, 0));
        Quad("ceiling", root, new Vector3(0, 2.6f, 0), new Vector2(4.6f, 4.2f), wall, new Vector3(-90, 0, 0));
        Box("skirting", root, new Vector3(0, .04f, 1.885f), new Vector3(4.6f, .08f, .02f), dark);
        // the window: reveals (the wall's thickness), a wooden frame set into it, a sill; outside, the night is cut paper standing well back from the glass
        string P = "Assets/Paper/street/";
        var win = new GameObject("window").transform; win.SetParent(root, false); win.localPosition = new Vector3(WX, (WY0 + WY1) / 2, WZ);
        float wh = WY1 - WY0; var plaster = Lit("reveal", null, null, null, Vector2.one, new Color(.62f, .58f, .52f), 0.05f);
        Box("reveal_top", win, new Vector3(0, wh / 2 + .01f, .08f), new Vector3(WW + .04f, .02f, .16f), plaster); Box("reveal_bot", win, new Vector3(0, -wh / 2 - .01f, .08f), new Vector3(WW + .04f, .02f, .16f), plaster);
        Box("reveal_l", win, new Vector3(-WW / 2 - .01f, 0, .08f), new Vector3(.02f, wh, .16f), plaster); Box("reveal_r", win, new Vector3(WW / 2 + .01f, 0, .08f), new Vector3(.02f, wh, .16f), plaster);
        foreach (var (n, p2, s2) in new[] { ("top", new Vector3(0, wh / 2 - .025f, .09f), new Vector3(WW, .05f, .05f)), ("bot", new Vector3(0, -wh / 2 + .025f, .09f), new Vector3(WW, .05f, .05f)),
                                           ("l", new Vector3(-WW / 2 + .025f, 0, .09f), new Vector3(.05f, wh, .05f)), ("r", new Vector3(WW / 2 - .025f, 0, .09f), new Vector3(.05f, wh, .05f)),
                                           ("mid", new Vector3(0, 0, .09f), new Vector3(.04f, wh, .04f)), ("sill", new Vector3(0, -wh / 2 - .015f, -.045f), new Vector3(WW + .14f, .03f, .13f)) })
            Box("frame_" + n, win, p2, s2, dark);
        var outside = new GameObject("outside").transform; outside.SetParent(root, false);
        Quad("out_sky", outside, new Vector3(WX, 1.9f, WZ + 2.3f), new Vector2(7.0f, 4.2f), Paper("paper_sky_win", Tex(P + "sky.png", alpha: true, repeat: false), Tex(P + "sky.png", alpha: true, repeat: false), 0.9f, null), default, false);
        Quad("out_moon", outside, new Vector3(WX + 0.55f, 2.45f, WZ + 2.2f), new Vector2(.42f, .42f), Paper("paper_moon_win", Tex(P + "moon.png", alpha: true, repeat: false), Tex(P + "moon_emit.png", repeat: false), 2.0f, null), default, false);
        Quad("out_far", outside, new Vector3(WX + 0.2f, 1.42f, WZ + 1.7f), new Vector2(4.6f, 2.05f), Paper("paper_far_win", Tex(P + "far_buildings.png", alpha: true, repeat: false), Tex(P + "far_buildings_emit.png", repeat: false), 1.5f, null), default, false);
        Quad("out_near", outside, new Vector3(WX - 0.75f, 0.62f, WZ + 0.9f), new Vector2(1.65f, 2.3f), Paper("paper_near_win", Tex(P + "block_R.png", alpha: true, repeat: false), Tex(P + "block_R_emit.png", repeat: false), 1.1f, null), default, false);

        string R = "Assets/Props/";
        Prop(R + "side_table_01/side_table_01_2k.gltf", "TvTable", root, new Vector3(-1.55f, 0, 0.75f), -48);
        var tv = Prop(R + "television_02/television_02_2k.gltf", "TV", root, new Vector3(-1.55f, 0.55f, 0.75f), -48);
        Prop(R + "wooden_bookshelf_worn/wooden_bookshelf_worn_2k.gltf", "Shelf", root, new Vector3(-1.45f, 0, 1.6f), 0, 0.82f);
        Prop(R + "book_encyclopedia_set_01/book_encyclopedia_set_01_2k.gltf", "Books", root, new Vector3(-1.45f, 0.86f, 1.62f), 0);
        Prop(R + "schooldesk_01/SchoolDesk_01_2k.gltf", "Desk", root, new Vector3(1.35f, 0, 1.45f), 180);
        Prop(R + "schoolchair_01/SchoolChair_01_2k.gltf", "SeatChair", root, SEAT, SEAT_YAW + CHAIR_YAW_OFFSET, CHAIR_SCALE);
        Prop(R + "desk_lamp_arm_01/desk_lamp_arm_01_2k.gltf", "DeskLamp", root, new Vector3(1.62f, 0.76f, 1.55f), 215);
        Prop(R + "alarm_clock_01/alarm_clock_01_2k.gltf", "Clock", root, new Vector3(1.12f, 0.76f, 1.6f), 170);
        Prop(R + "binder_notebook/binder_notebook_2k.gltf", "Notebook", root, new Vector3(1.32f, 0.76f, 1.38f), 100, 0.8f);
        Prop(R + "sf_bento/scene.gltf", "Bento", root, new Vector3(1.05f, 0.76f, 1.32f), 15, 0.21f);
        Prop(R + "sf_schoolbag/scene.gltf", "Bag", root, new Vector3(0.78f, 0, 1.55f), 150);
        Prop(R + "sf_zabuton/scene.gltf", "Zabuton", root, new Vector3(-0.95f, 0, -0.55f), 35, 0.22f);
        Prop(R + "potted_plant_04/potted_plant_04_2k.gltf", "Plant", root, new Vector3(-1.2f, 1.66f, 1.6f), 0);
        var wc = Prop(R + "wall_clock/wall_clock_2k.gltf", "WallClock", root, new Vector3(-0.55f, 1.85f, 1.87f), 180);

        // the screen: what the theatre camera sees, glowing a little in front of the tube
        if (tv != null)
        {
            var scr = new Material(Shader.Find("Universal Render Pipeline/Unlit")); scr.SetTexture("_BaseMap", rt); scr.SetColor("_BaseColor", new Color(3.4f, 3.4f, 3.4f, 1));
            scr = Save(scr, "tv_screen");
            // the tube's face in the television's own space (front = local -Z after import)
            Bounds lb = new Bounds(); bool first = true;
            foreach (var mf in tv.GetComponentsInChildren<MeshFilter>()) { var mb = mf.sharedMesh.bounds; var m2 = tv.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                for (int k = 0; k < 8; k++) { var c = m2.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((k & 1) * 2 - 1, ((k >> 1) & 1) * 2 - 1, ((k >> 2) & 1) * 2 - 1))); if (first) { lb = new Bounds(c, Vector3.zero); first = false; } else lb.Encapsulate(c); } }
            log.AppendLine($"  TV local bounds centre {lb.center:F3} size {lb.size:F3}");
            var holder = new GameObject("ScreenHolder").transform; holder.SetParent(tv.transform, false);
            holder.localPosition = new Vector3(lb.center.x + SCR.x, lb.center.y + SCR.y, lb.min.z + SCR.z);
            var q = Quad("Screen", holder, Vector3.zero, new Vector2(SCR_SIZE.x, SCR_SIZE.y), scr, default, false);
            tvScreen = q.transform;
            var mk = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mk.SetTexture("_BaseMap", Tex("Assets/Paper/tv_mask.png", alpha: true, repeat: false)); mk.SetColor("_BaseColor", Color.white);
            mk.SetFloat("_AlphaClip", 1); mk.EnableKeyword("_ALPHATEST_ON"); mk.SetFloat("_Cutoff", 0.5f); mk.renderQueue = 2450; mk = Save(mk, "tv_mask");
            Quad("ScreenMask", holder, new Vector3(0, 0, -0.0008f), new Vector2(SCR_SIZE.x * 1.02f, SCR_SIZE.y * 1.02f), mk, default, false);   // the tube's rounded corners
            AddLight("TvGlow", holder, LightType.Spot, new Vector3(0, 0, -0.04f), new Vector3(0, -0.12f, -2f), new Color(0.70f, 0.80f, 1f), 1.9f, 5.5f, 140f);
        }
        // light: the desk lamp is the only lamp on; the window lets in a little night blue
        AddLight("DeskLampLight", root, LightType.Spot, new Vector3(1.47f, 1.38f, 1.47f), new Vector3(1.05f, 0.74f, 1.15f), new Color(1f, 0.74f, 0.44f), 34f, 6f, 124f);
        // what the lamp's light does after its first bounce off the desk and wall (stand-in for indirect light until it is baked)
        AddLight("DeskLampBounce", root, LightType.Point, new Vector3(1.1f, 1.05f, 1.05f), Vector3.zero, new Color(1f, 0.70f, 0.42f), 1.5f, 4.6f, 0, false);
        AddLight("RoomFill", root, LightType.Point, new Vector3(-0.2f, 2.2f, 0.0f), Vector3.zero, new Color(0.78f, 0.66f, 0.58f), 0.50f, 6f, 0, false);
        // the lamp-lit floor throws warm light back up at her, so the screen is not the only thing on her face
        AddLight("FloorBounce", root, LightType.Point, SEAT - TVF * 0.55f + new Vector3(0, 0.30f, 0), Vector3.zero, new Color(1f, 0.62f, 0.38f), 0.58f, 2.2f, 0, false);
        AddLight("OutsideFill", root, LightType.Spot, new Vector3(0.55f, 2.3f, 2.25f), new Vector3(0.45f, 1.2f, 4.0f), new Color(0.42f, 0.52f, 0.85f), 9f, 6f, 130f, false);
        // moonlight: a lamp far outside, so the window frame draws its cross on the floor
        AddLight("Moonlight", root, LightType.Spot, new Vector3(2.35f, 3.55f, 4.6f), new Vector3(-0.15f, 0f, 0.35f), new Color(0.46f, 0.60f, 1f), 520f, 14f, 30f);
    }

    // ---------------------------------------------------------------- build
    public static string Build()
    {
        log = new StringBuilder();
        AssetDatabase.Refresh();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Pipeline();
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.035f, 0.042f, 0.065f); RenderSettings.fog = false;
        RenderSettings.skybox = null; RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom; RenderSettings.reflectionIntensity = 0.3f;
        RenderSettings.ambientLight = new Color(0.05f, 0.055f, 0.08f); DynamicGI.UpdateEnvironment();

        EnsureDir(GEN);
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(GEN + "/tv_rt.renderTexture");
        if (rt == null) { rt = new RenderTexture(1280, 960, 24, RenderTextureFormat.DefaultHDR) { antiAliasing = 4 }; AssetDatabase.CreateAsset(rt, GEN + "/tv_rt.renderTexture"); }

        var street = new GameObject("PaperStreet").transform; street.position = T; Street(street);
        var room = new GameObject("Room").transform; Room(room, rt);

        var pStreet = Post("post_street", 0.35f, 0.55f, 5.4f, 5.6f, 60f);
        var pRoom = Post("post_room", 0.55f, 0.50f, 2.6f, 2.8f, 42f);
        var tc = Cam("TheatreCam", T + new Vector3(0.15f, 1.55f, -3.6f), T + new Vector3(0f, 1.75f, 3.2f), 43f, pStreet, 1 << 7);
        var rc = Cam("RoomCam", new Vector3(-0.05f, 1.48f, -2.05f), new Vector3(-0.42f, 0.86f, 0.8f), 50f, pRoom, 1 << 6);
        tc.targetTexture = rt; tc.gameObject.tag = "Untagged"; rc.gameObject.tag = "MainCamera";

        // the singer
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MV/Characters/jinglin_ps2.vrm");
        if (prefab != null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = "Jinglin"; go.transform.position = SEAT - TVF * SEAT_BACK;
            go.transform.rotation = Quaternion.Euler(0, SEAT_YAW, 0);
            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            // several renders inside one Editor frame: without this the skin keeps the pose of the last real frame and she never moves
            foreach (var r in smrs) { r.forceMatrixRecalculationPerRender = true; r.updateWhenOffscreen = true; }
            var anim = go.GetComponent<Animator>(); var face = smrs.OrderByDescending(x => x.sharedMesh.blendShapeCount).First();
            var d = go.AddComponent<SingDriver>(); d.animator = anim; d.face = face; d.audioSource = go.AddComponent<AudioSource>();
            d.lipJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/MV/Data/lipsync_24_47.json"); d.mouth = 0f;
            d.stageOrigin = go.transform.position; d.CaptureRest(); d.plantFeet = false; d.bodyClips = new AnimationClip[0]; d.bodyStart = new float[0];
            // hands folded in her lap; a little after two seconds one hand comes up to her chest, later to her throat
            d.restPose = "lap"; d.gestureNames = new[] { "chest", "throat" }; d.gestureStart = new[] { 2.3f, 6.2f }; d.gestureDur = new[] { 3.4f, 3.0f };
            var sp = go.AddComponent<SeatPose>(); sp.driver = d; var scrGo = GameObject.Find("Screen"); sp.lookTarget = scrGo != null ? scrGo.transform : null;
            try { PoseAt(0f); log.AppendLine("jinglin seated; " + lastSeatNote); } catch (System.Exception e) { log.AppendLine("pose failed: " + e.Message + " | " + e.StackTrace.Split('\n')[0]); }
        } else log.AppendLine("MISSING jinglin prefab");

        EnsureDir("Assets/Scenes");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/Look.unity");
        AssetDatabase.SaveAssets();
        log.AppendLine("scene saved");
        return log.ToString();
    }

    // ---------------------------------------------------------------- the seated pose (Assets/Look/SeatPose.cs)
    static string lastSeatNote => SeatPose.lastNote;
    static Vector3 ScreenPos() { var s = GameObject.Find("Screen"); return s != null ? s.transform.position : new Vector3(-1.42f, 0.80f, 0.63f); }

    /// pose her for film time t (seconds): gestures, breathing, blinks and gaze from the rig, then seated
    public static void PoseAt(float t)
    {
        var d = Object.FindObjectsByType<SingDriver>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == "Jinglin"); if (d == null) return;
        SeatPose.EnsureLap();
        d.Apply(t); SeatPose.Seat(d, ScreenPos());
    }

    // ---------------------------------------------------------------- capture
    static void RenderTo(Camera cam, int w, int h, string path)
    {
        // one persistent target per size, kept as an asset and never destroyed (destroying a target a camera has just used crashed the Editor;
        // pooled temporaries handed back another camera's picture)
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

    struct View { public Vector3 pos, look; public float fov; }
    static Vector3 HerRight => Quaternion.Euler(0, SEAT_YAW, 0) * Vector3.right;
    static Vector3 HeadPos() { var d = Object.FindFirstObjectByType<SingDriver>(); return d != null ? d.animator.GetBoneTransform(HumanBodyBones.Head).position + Vector3.up * 0.06f : SEAT + Vector3.up * 1.1f; }
    /// over her shoulder toward the screen (u 0 → 1 pushes in)
    static View Ots(float u) { Vector3 s = ScreenPos(); return new View { pos = Vector3.Lerp(SEAT + TVF * 0.95f + HerRight * 0.52f + Vector3.up * 1.20f, SEAT + TVF * 0.50f + HerRight * 0.40f + Vector3.up * 1.12f, u), look = s + Vector3.up * 0.02f, fov = Mathf.Lerp(40f, 29f, u) }; }
    /// from beside the television back at her face (u 0 → 1 drifts closer)
    static View Face(float u) { Vector3 s = ScreenPos(); return new View { pos = Vector3.Lerp(s + TVF * 0.10f - HerRight * 0.62f + Vector3.up * 0.26f, s + TVF * 0.30f - HerRight * 0.44f + Vector3.up * 0.30f, u), look = HeadPos() - Vector3.up * 0.10f, fov = Mathf.Lerp(37f, 31f, u) }; }
    static void Put(Camera c, View v, DepthOfField dof, Vector3 focusOn) { c.transform.position = v.pos; c.transform.rotation = Quaternion.LookRotation(v.look - v.pos); c.fieldOfView = v.fov; if (dof != null) dof.focusDistance.value = Vector3.Distance(v.pos, focusOn); }

    /// stills: 0 = all four, 1 = room views only, 2 = street only, 3 = + television close-up
    public static string Still(int which)
    {
        var tc = GameObject.Find("TheatreCam").GetComponent<Camera>(); var rc = GameObject.Find("RoomCam").GetComponent<Camera>();
        PoseAt(3.4f);                                                 // mid-reaction: the hand is at her chest
        tc.Render();
        DepthOfField dofR = null; AssetDatabase.LoadAssetAtPath<VolumeProfile>(GEN + "/post_room.asset").TryGet(out dofR); float f0 = dofR != null ? dofR.focusDistance.value : 0;
        Vector3 p0 = rc.transform.position; Quaternion r0 = rc.transform.rotation; float fov0 = rc.fieldOfView;
        if (which != 2)
        {
            if (dofR != null) dofR.focusDistance.value = Vector3.Distance(p0, HeadPos());
            RenderTo(rc, 1920, 1080, "Capture/still_wide.jpg");
            Put(rc, Ots(0.3f), dofR, ScreenPos()); RenderTo(rc, 1920, 1080, "Capture/still_ots.jpg");
            Put(rc, Face(0.5f), dofR, HeadPos()); RenderTo(rc, 1920, 1080, "Capture/still_face.jpg");
        }
        if (which == 3) { var s = GameObject.Find("Screen").transform; rc.transform.position = s.position - s.forward * 0.9f + Vector3.up * 0.08f; rc.transform.LookAt(s.position); rc.fieldOfView = 34; RenderTo(rc, 1280, 720, "Capture/tv_close.jpg"); }
        rc.transform.position = p0; rc.transform.rotation = r0; rc.fieldOfView = fov0; if (dofR != null) dofR.focusDistance.value = f0;
        if (which != 1) RenderTo(tc, 1920, 1080, "Capture/still_street.jpg");
        return "stills written; " + lastSeatNote;
    }

    /// her picture on a transparent ground, standing, front on, to be cut out as a paper puppet (Capture/jinglin_cut.png)
    public static string Cutout()
    {
        // a fresh standing copy of her, far to one side of the room (the seated one keeps its leg pose between calls)
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MV/Characters/jinglin_ps2.vrm"); if (prefab == null) return "no prefab";
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = "CutModel"; go.transform.position = new Vector3(0, 0, -30f); go.transform.rotation = Quaternion.identity;
        var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(); foreach (var r in smrs) { r.updateWhenOffscreen = true; r.forceMatrixRecalculationPerRender = true; }
        var dr = go.AddComponent<SingDriver>(); dr.animator = go.GetComponent<Animator>(); dr.face = smrs.OrderByDescending(x => x.sharedMesh.blendShapeCount).First(); dr.audioSource = go.AddComponent<AudioSource>();
        dr.lipJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/MV/Data/lipsync_24_47.json"); dr.mouth = 0f; dr.stageOrigin = go.transform.position; dr.CaptureRest(); dr.plantFeet = true;
        dr.bodyClips = new AnimationClip[0]; dr.bodyStart = new float[0]; dr.restPose = "folded"; dr.gestureNames = new string[0]; dr.gestureStart = new float[0]; dr.gestureDur = new float[0]; dr.Apply(1.0f);
        // she stays where she is (teleporting a skinned mesh and rendering at once culls it on stale bounds); only her layer is drawn
        var trs = go.GetComponentsInChildren<Transform>(true); var layers = trs.Select(t => t.gameObject.layer).ToArray(); foreach (var t in trs) t.gameObject.layer = 8;
        Vector3 c = go.transform.position + Vector3.up * 0.80f;
        var cg = new GameObject("CutCam"); var cam = cg.AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 0.86f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 10; cam.cullingMask = 1 << 8;
        cam.transform.position = c + go.transform.forward * 3f; cam.transform.LookAt(c);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
        var ad = cg.AddComponent<UniversalAdditionalCameraData>(); ad.renderPostProcessing = false; ad.renderShadows = false;
        var lg = new GameObject("CutLight"); var l = lg.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 0.80f; l.color = Color.white; l.shadows = LightShadows.None;
        l.transform.rotation = Quaternion.LookRotation(-go.transform.forward + go.transform.right * -0.25f + Vector3.down * 0.35f);
        var others = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(x => x != l && x.enabled).ToArray(); foreach (var o in others) o.enabled = false;
        var amb0 = RenderSettings.ambientLight; RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.34f);
        int w = 1100, h = 2064; var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 }; cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGBA32, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); RenderTexture.active = null;
        Directory.CreateDirectory("Capture"); File.WriteAllBytes("Capture/jinglin_cut.png", tex.EncodeToPNG());
        cam.targetTexture = null; Object.DestroyImmediate(tex); Object.DestroyImmediate(cg); Object.DestroyImmediate(lg); RenderSettings.ambientLight = amb0;
        foreach (var o in others) o.enabled = true; for (int i = 0; i < trs.Length; i++) trs[i].gameObject.layer = layers[i];
        Object.DestroyImmediate(go); PoseAt(0f);
        return "cut-out source written";
    }

    // ---------------------------------------------------------------- moving shots
    static float Ease(float u) { u = Mathf.Clamp01(u); return u * u * (3 - 2 * u); }
    struct Sway { public Transform t; public Vector3 pos; public Quaternion rot; public float amp, speed, phase; }

    /// shot 1 = her face, lit by the screen, the camera drifting in (her hand comes up to her chest);
    /// shot 2 = over her shoulder, pushing in on the television; shot 3 = the paper street, the camera sliding sideways so the flats part.
    /// t0 = film time of the first frame. Frames go to Capture/shot<N>/f_00000.jpg at 30 fps. Paper moves on twos, the camera on ones.
    public static string Shot(int which, int frames, float t0)
    {
        var tc = GameObject.Find("TheatreCam").GetComponent<Camera>(); var rc = GameObject.Find("RoomCam").GetComponent<Camera>();
        var sways = new List<Sway>();
        void Add(string n, float amp, float speed, float phase) { var g = GameObject.Find(n); if (g != null) sways.Add(new Sway { t = g.transform, pos = g.transform.position, rot = g.transform.rotation, amp = amp, speed = speed, phase = phase }); }
        Add("crowd_A", 1.3f, 1.9f, 0f); Add("crowd_B", 1.1f, 2.3f, 1.7f); Add("puppet_jinglin", 1.6f, 1.3f, 0.6f); Add("tape", 0.35f, 2.9f, 2.2f);
        Vector3 rp0 = rc.transform.position, tp0 = tc.transform.position; Quaternion rr0 = rc.transform.rotation, tr0 = tc.transform.rotation; float rf0 = rc.fieldOfView, tf0 = tc.fieldOfView;
        DepthOfField dofR = null; AssetDatabase.LoadAssetAtPath<VolumeProfile>(GEN + "/post_room.asset").TryGet(out dofR); float fr0 = dofR != null ? dofR.focusDistance.value : 0;
        var glowGo = GameObject.Find("TvGlow"); Light glow = glowGo != null ? glowGo.GetComponent<Light>() : null; float g0 = glow != null ? glow.intensity : 0;
        string dir = "Capture/shot" + which; if (Directory.Exists(dir)) Directory.Delete(dir, true); Directory.CreateDirectory(dir);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int f = 0; f < frames; f++)
        {
            float t = t0 + f / 30f, u = Ease(f / (float)(frames - 1)), tq = Mathf.Floor(t * 12f) / 12f;
            foreach (var s in sways) { s.t.position = s.pos; s.t.rotation = s.rot; if (s.amp > 0) s.t.RotateAround(s.pos - s.t.up * s.t.lossyScale.y * 0.5f, s.t.forward, s.amp * Mathf.Sin(tq * s.speed + s.phase)); }
            if (glow != null) glow.intensity = g0 * (0.80f + 0.34f * Mathf.PerlinNoise(tq * 2.3f, 0.5f));          // the picture changes, so does its light
            PoseAt(t);
            if (which == 1) { Put(rc, Face(u), dofR, HeadPos()); tc.Render(); RenderTo(rc, 1920, 1080, $"{dir}/f_{f:00000}.jpg"); }
            else if (which == 2) { Put(rc, Ots(u), dofR, Vector3.Lerp(HeadPos(), ScreenPos(), Ease(u * 1.6f))); tc.Render(); RenderTo(rc, 1920, 1080, $"{dir}/f_{f:00000}.jpg"); }
            else
            {
                Vector3 a = T + new Vector3(-1.5f, 1.45f, -3.9f), b = T + new Vector3(1.35f, 1.62f, -3.3f), look = T + new Vector3(0.15f, 1.55f, 2.6f);
                tc.transform.position = Vector3.Lerp(a, b, u); tc.transform.rotation = Quaternion.LookRotation(look - tc.transform.position); tc.fieldOfView = Mathf.Lerp(46f, 41f, u);
                RenderTo(tc, 1920, 1080, $"{dir}/f_{f:00000}.jpg");
            }
        }
        foreach (var s in sways) { s.t.position = s.pos; s.t.rotation = s.rot; }
        rc.transform.position = rp0; rc.transform.rotation = rr0; rc.fieldOfView = rf0; tc.transform.position = tp0; tc.transform.rotation = tr0; tc.fieldOfView = tf0;
        if (dofR != null) dofR.focusDistance.value = fr0; if (glow != null) glow.intensity = g0;
        PoseAt(0f);
        return $"shot {which}: {frames} frames in {sw.Elapsed.TotalSeconds:F1} s ({frames / sw.Elapsed.TotalSeconds:F1} fps)";
    }

    /// add the play-mode capture component (Assets/Look/ShotDumper.cs); then enter play mode and poll Progress()
    public static string SetupDumper(int warmup)
    {
        foreach (var old in Object.FindObjectsByType<ShotDumper>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
        var d = Object.FindObjectsByType<SingDriver>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == "Jinglin"); if (d == null) return "no character";
        var g = new GameObject("ShotDumper"); var sd = g.AddComponent<ShotDumper>();
        sd.driver = d; sd.roomCam = GameObject.Find("RoomCam").GetComponent<Camera>(); sd.theatreCam = GameObject.Find("TheatreCam").GetComponent<Camera>();
        sd.screen = GameObject.Find("Screen").transform; var gl = GameObject.Find("TvGlow"); sd.tvGlow = gl != null ? gl.GetComponent<Light>() : null;
        sd.seat = SEAT; sd.tvFacing = TVF; sd.seatYaw = SEAT_YAW; sd.streetOrigin = T; sd.warmupFrames = warmup;
        d.usePreviewTime = true; d.previewTime = sd.t0;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return "dumper ready: " + (sd.frames1 + sd.frames2 + sd.frames3) + " frames + " + warmup + " warm-up";
    }

    public static string Progress() { return $"playing {EditorApplication.isPlaying} written {ShotDumper.Written} done {ShotDumper.Done} {ShotDumper.Note}"; }
}
