    // ---------------------------------------------------------------- B: the inside of the picture tube, in front of the proscenium
    /// A bulged sheet (a cap of a sphere) of w x h whose middle stands `bulge` in front of its edge, facing -z.
    static GameObject Cap(string name, Transform parent, Vector3 at, float w, float h, float bulge, Material mat, bool faceMinusZ = true, int nx = 32, int ny = 24)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = at;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>(); float half = Mathf.Sqrt(w * w + h * h) * 0.5f;
        for (int j = 0; j <= ny; j++) for (int i = 0; i <= nx; i++)
        {
            float x = (i / (float)nx - 0.5f) * w, y = (j / (float)ny - 0.5f) * h, r2 = (x * x + y * y) / (half * half);
            v.Add(new Vector3(x, y, (faceMinusZ ? -1 : 1) * bulge * (1f - r2))); uv.Add(new Vector2(i / (float)nx, j / (float)ny));
        }
        for (int j = 0; j < ny; j++) for (int i = 0; i < nx; i++)
        {
            int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
            if (faceMinusZ) { t.AddRange(new[] { a, c, b, b, c, d }); } else { t.AddRange(new[] { a, b, c, b, d, c }); }
        }
        var mesh = new Mesh { name = name }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; return g;
    }

    static Material Glass(string name, Color tint, float smooth, float alpha = 1f)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); tint.a = alpha; m.SetColor("_BaseColor", tint); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0f);
        if (alpha < 1f)
        {   // see-through, and the reflections on it stay at full strength
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_BlendModePreserveSpecular", 1); m.SetFloat("_SrcBlend", (float)BlendMode.One); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.EnableKeyword("_ALPHAPREMULTIPLY_ON"); m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000;
        }
        return Save(m, name);
    }

    /// The cavity the stage looks out into: dark card on timber all round, and at the far end the inside of the tube's faceplate, a
    /// bulged 4 : 3 glass in a black mask. Nothing here has a light of its own; it only catches what spills out of the proscenium.
    public const float B_X = 3.5f, B_Y0 = -1.04f, B_Y1 = 5.0f, B_Z0 = -5.4f, B_Z1 = -0.04f;
    static void Tube(Transform root)
    {
        var g = new GameObject("Tube").transform; g.SetParent(root, false);
        var card = CardM("tube_card", new Color(0.13f, 0.13f, 0.15f), 0.06f, 2); var wood = CardM("tube_wood", new Color(0.20f, 0.18f, 0.16f), 0.10f, 2); var label = CardM("tube_label", new Color(0.30f, 0.29f, 0.26f)); var mask = CardM("tube_mask", new Color(0.03f, 0.03f, 0.035f), 0.25f);
        float H = B_Y1 - B_Y0, D = B_Z1 - B_Z0;
        PanelWall(WallFrame(g, "TubeLeft", new Vector3(-B_X, B_Y0, B_Z0), -90), D, H, null, card, wood, label, 1.0f);
        PanelWall(WallFrame(g, "TubeRight", new Vector3(B_X, B_Y0, B_Z1), 90), D, H, null, card, wood, label, 1.0f);
        // beside and above the proscenium front, back to the cavity walls
        foreach (float sx in new[] { -1f, 1f }) Box("tube_cheek", g, new Vector3(sx * 3.27f, (B_Y0 + B_Y1) * 0.5f, B_Z1 + 0.015f), new Vector3(0.56f, H, 0.03f), card);
        Box("tube_over", g, new Vector3(0, 4.55f, B_Z1 + 0.012f), new Vector3(6.1f, 0.92f, 0.024f), card);
        Box("tube_floor", g, new Vector3(0, B_Y0 - 0.02f, (B_Z0 + B_Z1) * 0.5f), new Vector3(2 * B_X + 0.16f, 0.04f, D + 0.16f), card);
        for (float z = B_Z0 + 0.5f; z < B_Z1; z += 1.0f) Box("tube_floor_rib", g, new Vector3(0, B_Y0 + 0.02f, z), new Vector3(2 * B_X, 0.04f, 0.06f), wood);
        GridDeck(g, "TubeDeck", -B_X, B_X, B_Z0, B_Z1, B_Y1, wood, card);
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { B_Z0 + 0.04f, B_Z1 - 0.04f }) Box("tube_corner", g, new Vector3(sx * (B_X - 0.04f), (B_Y0 + B_Y1) * 0.5f, sz), new Vector3(0.08f, H, 0.08f), wood);
        // the faceplate: glass 6400 x 4800 bulging away from the stage, a mask round it, a closed black wall behind
        const float GW_ = 6.4f, GH_ = 4.8f; float gy = (B_Y0 + B_Y1) * 0.5f, gz = B_Z0 + 0.12f;
        Cap("TubeGlass", g, new Vector3(0, gy, gz), GW_, GH_, -0.26f, Glass("tube_glass", new Color(0.02f, 0.03f, 0.04f), 0.96f, 0.30f), false);          // seen from the stage: concave, its middle 260 further away
        Box("mask_l", g, new Vector3(-(B_X + GW_ * 0.5f) * 0.5f, gy, gz + 0.03f), new Vector3(B_X - GW_ * 0.5f + 0.06f, H, 0.06f), mask); Box("mask_r", g, new Vector3((B_X + GW_ * 0.5f) * 0.5f, gy, gz + 0.03f), new Vector3(B_X - GW_ * 0.5f + 0.06f, H, 0.06f), mask);
        Box("mask_t", g, new Vector3(0, (B_Y1 + gy + GH_ * 0.5f) * 0.5f, gz + 0.03f), new Vector3(GW_ + 0.1f, B_Y1 - gy - GH_ * 0.5f + 0.06f, 0.06f), mask); Box("mask_b", g, new Vector3(0, (B_Y0 + gy - GH_ * 0.5f) * 0.5f, gz + 0.03f), new Vector3(GW_ + 0.1f, gy - GH_ * 0.5f - B_Y0 + 0.06f, 0.06f), mask);
        Box("tube_front", g, new Vector3(0, gy, B_Z0 - 0.33f), new Vector3(2 * B_X + 0.16f, H + 0.16f, 0.03f), mask);
        foreach (float sx in new[] { -1f, 1f }) Box("tube_front_side", g, new Vector3(sx * (B_X + 0.06f), gy, B_Z0 - 0.15f), new Vector3(0.03f, H + 0.16f, 0.40f), mask);
        Box("tube_front_top", g, new Vector3(0, B_Y1 + 0.06f, B_Z0 - 0.15f), new Vector3(2 * B_X + 0.16f, 0.03f, 0.40f), mask); Box("tube_front_bot", g, new Vector3(0, B_Y0 - 0.06f, B_Z0 - 0.15f), new Vector3(2 * B_X + 0.16f, 0.03f, 0.40f), mask);
        // the room beyond the glass: a picture of it rendered from where the television stands, put in by Sets.TubeView once the room exists
        var rpg = new GameObject("TubeReflection"); rpg.transform.SetParent(g, false); rpg.transform.localPosition = new Vector3(0, 2.0f, -2.6f);
        var rp = rpg.AddComponent<ReflectionProbe>(); rp.mode = ReflectionProbeMode.Realtime; rp.refreshMode = ReflectionProbeRefreshMode.EveryFrame; rp.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        rp.resolution = 256; rp.size = new Vector3(7.2f, 6.2f, 5.6f); rp.boxProjection = true; rp.hdr = true; rp.clearFlags = ReflectionProbeClearFlags.SolidColor; rp.backgroundColor = Color.black; rp.importance = 2;
        var view = Quad("TubeView", g, new Vector3(0, gy, B_Z0 - 0.30f), new Vector2(GW_, GH_), Glass("tube_view_empty", new Color(0.01f, 0.012f, 0.02f), 0.2f), new Vector3(0, 180, 0), false);
    }
