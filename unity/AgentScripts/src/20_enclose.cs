    // ---------------------------------------------------------------- closing a space: walls, deck, ground
    /// A child frame for one wall, seen from inside the room: local x runs along the wall, y up, and the room is on the -z side.
    static Transform WallFrame(Transform root, string name, Vector3 origin, float yaw)
    {
        var g = new GameObject(name).transform; g.SetParent(root, false); g.localPosition = origin; g.localEulerAngles = new Vector3(0, yaw, 0); return g;
    }

    /// A wall of card panels on a timber frame, the way the inside of a toy theatre box looks: panels 1000 wide with a 6 mm joint,
    /// a 60 x 40 stud over every joint, rails every `railStep`, a diagonal brace in every other bay, a numbered label and glue tabs.
    static void PanelWall(Transform g, float width, float height, List<Rect> holes, Material card, Material wood, Material label, float railStep = 0.866f, float y0 = 0f)
    {
        const float T = 0.03f, PW = 1.0f, J = 0.006f;
        if (holes == null) holes = new List<Rect>();
        Wall(g, 0, y0, width, height, T, 0.012f, holes, CardM("backing", new Color(0.05f, 0.05f, 0.07f)));
        int n = Mathf.CeilToInt(width / PW - 0.001f);
        for (int i = 0; i < n; i++)
        {
            float xa = i * PW + J * 0.5f, xb = Mathf.Min(width, (i + 1) * PW) - J * 0.5f; if (xb - xa < 0.02f) continue;
            var hs = holes.Where(r => r.xMax > xa && r.xMin < xb).Select(r => Rect.MinMaxRect(Mathf.Max(r.xMin, xa), r.yMin, Mathf.Min(r.xMax, xb), r.yMax)).ToList();
            Wall(g, xa, y0, xb - xa, height, 0, T, hs, card);
            bool open = hs.Count > 0;
            float sx = i * PW;                                                                                              // the stud over the joint on the left of this panel
            if (i > 0 && !holes.Any(r => r.xMin < sx + 0.03f && r.xMax > sx - 0.03f && r.yMin <= y0 + 0.01f && r.yMax >= y0 + height - 0.01f))
            {
                var cuts = holes.Where(r => r.xMin < sx + 0.03f && r.xMax > sx - 0.03f).OrderBy(r => r.yMin).ToList(); float ya = y0;
                foreach (var r in cuts) { if (r.yMin - ya > 0.05f) Box("stud", g, new Vector3(sx, (ya + r.yMin) * 0.5f, -0.02f), new Vector3(0.06f, r.yMin - ya, 0.04f), wood); ya = r.yMax; }
                if (y0 + height - ya > 0.05f) Box("stud", g, new Vector3(sx, (ya + y0 + height) * 0.5f, -0.02f), new Vector3(0.06f, y0 + height - ya, 0.04f), wood);
            }
            if (open) continue;
            float cx = (xa + xb) * 0.5f, w = xb - xa; int rails = Mathf.Max(1, Mathf.RoundToInt(height / railStep));
            for (int k = 1; k < rails; k++) Box("rail", g, new Vector3(cx, y0 + height * k / rails, -0.015f), new Vector3(w - 0.06f, 0.05f, 0.03f), wood);
            for (int k = 0; k < rails; k++)
            {
                float ya = y0 + height * k / rails, yb = y0 + height * (k + 1) / rails, cy = (ya + yb) * 0.5f, hh = yb - ya;
                if ((i + k) % 2 == 0)
                {
                    float len = Mathf.Sqrt((w - 0.08f) * (w - 0.08f) + (hh - 0.07f) * (hh - 0.07f)), ang = Mathf.Atan2(hh - 0.07f, w - 0.08f) * Mathf.Rad2Deg * ((i / 2 + k) % 2 == 0 ? 1 : -1);
                    Box("brace", g, new Vector3(cx, cy, -0.012f), new Vector3(len, 0.04f, 0.024f), wood, new Vector3(0, 0, ang));
                }
                else
                {
                    Box("label", g, new Vector3(cx - w * 0.22f, cy + hh * 0.18f, -0.0015f), new Vector3(0.12f, 0.08f, 0.003f), label);
                    Box("label_line", g, new Vector3(cx - w * 0.22f, cy + hh * 0.18f - 0.012f, -0.0034f), new Vector3(0.08f, 0.006f, 0.001f), wood);
                    for (int q = 0; q < 2; q++) Box("tab", g, new Vector3(xa + 0.02f, ya + hh * (0.3f + 0.4f * q), -0.0015f), new Vector3(0.04f, 0.12f, 0.003f), label);
                    Box("score", g, new Vector3(cx, cy - hh * 0.2f, -0.001f), new Vector3(w - 0.14f, 0.004f, 0.002f), CardM("score", new Color(0.10f, 0.10f, 0.13f)));
                }
            }
        }
        Box("sole", g, new Vector3(width * 0.5f, y0 + 0.03f, -0.03f), new Vector3(width, 0.06f, 0.06f), wood);
        Box("head", g, new Vector3(width * 0.5f, y0 + height - 0.03f, -0.03f), new Vector3(width, 0.06f, 0.06f), wood);
    }

    /// The deck over a space, seen from below: a closed lid of dark card, main beams 120 x 120 every 1500, joists 60 x 90 every 300.
    static void GridDeck(Transform root, string name, float x0, float x1, float z0, float z1, float y, Material wood, Material lid)
    {
        var g = new GameObject(name).transform; g.SetParent(root, false);
        Box("lid", g, new Vector3((x0 + x1) * 0.5f, y + 0.015f, (z0 + z1) * 0.5f), new Vector3(x1 - x0 + 0.16f, 0.03f, z1 - z0 + 0.16f), lid);          // overhangs the walls, so their chamfered tops are covered
        int nj = Mathf.RoundToInt((z1 - z0) / 0.30f); for (int k = 0; k <= nj; k++) Box("joist", g, new Vector3((x0 + x1) * 0.5f, y - 0.045f, z0 + 0.03f + (z1 - z0 - 0.06f) * k / nj), new Vector3(x1 - x0, 0.09f, 0.06f), wood);
        int nb = Mathf.RoundToInt((x1 - x0) / 1.5f); for (int k = 0; k <= nb; k++) Box("beam", g, new Vector3(x0 + 0.06f + (x1 - x0 - 0.12f) * k / nb, y - 0.15f, (z0 + z1) * 0.5f), new Vector3(0.12f, 0.12f, z1 - z0), wood);
    }

    /// A horizontal slab with rectangular cuts (x, z): its top at `top`, going down by `thick`. Built as a wall lying on its back.
    static Transform Slab(Transform parent, string name, float x0, float x1, float z0, float z1, float top, float thick, List<Rect> cuts, Material mat)
    {
        var f = new GameObject(name).transform; f.SetParent(parent, false); f.localEulerAngles = new Vector3(90, 0, 0);
        Wall(f, x0, z0, x1 - x0, z1 - z0, -top, thick, cuts ?? new List<Rect>(), mat); return f;
    }

    // the cuts in the stage floor (x, z): two grooves the paper people slide in, and the slot the lamp post lies in before it is raised
    const float G1 = 0.80f, G2 = 2.22f, GW = 0.014f, LS_Z = 2.32f, LS_W = 0.09f, LS_X0 = -2.95f, LS_X1 = 0.10f;
    static List<Rect> FloorCuts() { return new List<Rect> { Rect.MinMaxRect(-3f, G1 - GW * 0.5f, 3f, G1 + GW * 0.5f), Rect.MinMaxRect(-3f, G2 - GW * 0.5f, 3f, G2 + GW * 0.5f) }; }

    /// The stage house as a closed box: boards with the grooves cut through them, the under-stage, four walls, the fly loft.
    /// The loft is 8.2 m high: the tallest piece (3.6 m) hangs under a batten at 7.9 m with its foot at 4.3 m, above the borders' lower edge (3.55 m).
    public const float LOFT = 8.2f, BATTEN = 7.9f, UNDER = -1.04f;
    static void StageBox(Transform root)
    {
        var g = new GameObject("StageHouse").transform; g.SetParent(root, false);
        var boards = Lit("kit_boards", Tex("Assets/Textures/tex_laminate_floor/laminate_floor_diff_2k.jpg"), Tex("Assets/Textures/tex_laminate_floor/laminate_floor_nor_gl_2k.jpg", true), new Vector2(0.5f, 0.5f), new Color(0.46f, 0.30f, 0.22f), 0.72f);
        var ply = Lit("kit_ply", Tex("Assets/Textures/tex_plywood/plywood_diff_2k.jpg"), Tex("Assets/Textures/tex_plywood/plywood_nor_gl_2k.jpg", true), new Vector2(0.6f, 0.6f), new Color(0.80f, 0.70f, 0.56f), 0.2f);
        var card = CardM("house_card", new Color(0.50f, 0.43f, 0.34f), 0.08f, 2); var label = CardM("label", new Color(0.82f, 0.78f, 0.66f)); var lid = CardM("lid", new Color(0.10f, 0.10f, 0.12f));
        var dark = CardM("under_dark", new Color(0.06f, 0.06f, 0.07f)); var iron = CardM("iron", new Color(0.20f, 0.23f, 0.27f), 0.20f);
        // boards, 80 thick, cut by the grooves and the lamp slot
        Slab(g, "Floor", -3f, 3f, -0.2f, 5.2f, 0f, 0.08f, FloorCuts(), boards);
        // each cut is a channel with cheeks and a bottom, so that looking down it one sees a bottom and not the room below
        foreach (float gz in new[] { G1, G2 }) { Box("groove_bottom", g, new Vector3(0, -0.065f, gz), new Vector3(6f, 0.01f, GW + 0.04f), dark); foreach (float sd in new[] { -1f, 1f }) Box("groove_cheek", g, new Vector3(0, -0.07f, gz + sd * (GW * 0.5f + 0.008f)), new Vector3(6f, 0.06f, 0.016f), ply); }
        // the under-stage: closed, structural only (no camera goes there)
        Box("under_floor", g, new Vector3(0, UNDER - 0.02f, 2.6f), new Vector3(6.1f, 0.04f, 5.3f), dark);
        Box("under_back", g, new Vector3(0, UNDER * 0.5f, 5.215f), new Vector3(6.1f, -UNDER, 0.03f), dark); Box("under_l", g, new Vector3(-3.015f, UNDER * 0.5f, 2.6f), new Vector3(0.03f, -UNDER, 5.3f), dark); Box("under_r", g, new Vector3(3.015f, UNDER * 0.5f, 2.6f), new Vector3(0.03f, -UNDER, 5.3f), dark);
        for (int ix = 0; ix < 7; ix++) for (int iz = 0; iz < 6; iz++) Box("under_post", g, new Vector3(-2.9f + 0.9667f * ix, (UNDER - 0.08f) * 0.5f, 0.15f + 0.98f * iz), new Vector3(0.09f, -UNDER - 0.08f, 0.09f), ply);
        // the timber frame, the battens
        foreach (float sx in new[] { -2.85f, 2.85f }) foreach (float sz in new[] { 5.05f }) Box("post", g, new Vector3(sx, LOFT * 0.5f, sz), new Vector3(0.12f, LOFT, 0.12f), ply);
        foreach (float sz in new[] { 1.0f, 1.8f, 2.6f, 3.4f, 4.3f }) { Rod("batten", g, new Vector3(-2.85f, BATTEN, sz), new Vector3(2.85f, BATTEN, sz), 0.03f, iron); foreach (float sx in new[] { -2.4f, 0f, 2.4f }) Rod("batten_line", g, new Vector3(sx, BATTEN, sz), new Vector3(sx, LOFT - 0.2f, sz), 0.004f, label); }
        // walls: plywood sheets at the back (it has to take her shadow when the stage is struck), card on timber at the sides and behind the proscenium
        PlyWall(WallFrame(g, "WallBack", new Vector3(-3f, 0, 5.2f), 0), 6.0f, LOFT, ply, iron, label);
        PanelWall(WallFrame(g, "WallLeft", new Vector3(-3f, 0, 0), -90), 5.2f, LOFT, null, card, ply, label);
        PanelWall(WallFrame(g, "WallRight", new Vector3(3f, 0, 5.2f), 90), 5.2f, LOFT, null, card, ply, label);
        PanelWall(WallFrame(g, "WallFront", new Vector3(3f, 0, 0.002f), 180), 6.0f, LOFT, new List<Rect> { new Rect(1.0f, 0f, 4.0f, 3.0f) }, card, ply, label);
        GridDeck(g, "Grid", -3f, 3f, 0f, 5.2f, LOFT, ply, lid);
        // the chamfered ends of two walls leave a hairline at every corner: a post stands in each
        foreach (float sx in new[] { -2.96f, 2.96f }) foreach (float sz in new[] { 0.04f, 5.16f }) Box("corner_post", g, new Vector3(sx, LOFT * 0.5f, sz), new Vector3(0.08f, LOFT, 0.08f), ply);
    }

    /// A wall of plywood sheets 1220 x 2440 with 3 mm joints, a screw head every 300 along every edge, a stencilled mark on each sheet.
    static void PlyWall(Transform g, float width, float height, Material ply, Material iron, Material paint)
    {
        const float SW = 1.22f, SH = 2.44f, J = 0.003f, T = 0.018f;
        Box("backing", g, new Vector3(width * 0.5f, height * 0.5f, T + 0.006f), new Vector3(width, height, 0.012f), CardM("backing", new Color(0.05f, 0.05f, 0.07f)));
        int row = 0;
        for (float y = 0; y < height - 0.01f; y += SH, row++)
        {
            float off = row % 2 == 0 ? 0 : -SW * 0.5f;
            for (float x = off; x < width - 0.01f; x += SW)
            {
                float xa = Mathf.Max(0, x) + J * 0.5f, xb = Mathf.Min(width, x + SW) - J * 0.5f, ya = y + J * 0.5f, yb = Mathf.Min(height, y + SH) - J * 0.5f; if (xb - xa < 0.05f) continue;
                Box("sheet", g, new Vector3((xa + xb) * 0.5f, (ya + yb) * 0.5f, T * 0.5f), new Vector3(xb - xa, yb - ya, T), ply);
                int nx = Mathf.Max(1, Mathf.RoundToInt((xb - xa - 0.06f) / 0.30f)), ny = Mathf.Max(1, Mathf.RoundToInt((yb - ya - 0.06f) / 0.30f));
                if (xb - xa > 0.6f) { for (int q = 0; q < 3; q++) Box("stencil", g, new Vector3(xa + 0.16f + 0.05f * q, ya + 0.20f, -0.0006f), new Vector3(0.03f, q == 1 ? 0.06f : 0.045f, 0.0012f), paint); Box("stencil_bar", g, new Vector3(xa + 0.21f, ya + 0.14f, -0.0006f), new Vector3(0.14f, 0.008f, 0.0012f), paint); }
            }
        }
    }

    /// The sky: a cloth of crisp paper lit from behind, inside the side walls, its two ends folded forward twice so that it wraps the
    /// set like a cyclorama. One-sided (it is only ever seen from the stage side), hung from a roller.
    static Material DayCloth()
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetColor("_BaseColor", new Color(1, 1, 1, 0)); m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0); m.SetFloat("_Cull", 2); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000;
        return Save(m, "stage_sky_day");
    }

    static List<Renderer> Cyc(Transform root, Texture2D skyT)
    {
        var g = new GameObject("Cyc").transform; g.SetParent(root, false); var rs = new List<Renderer>();
        Vector2[] pts = { new Vector2(-2.95f, 3.3f), new Vector2(-2.80f, 4.5f), new Vector2(-2.5f, 4.9f), new Vector2(2.5f, 4.9f), new Vector2(2.80f, 4.5f), new Vector2(2.95f, 3.3f) };
        float total = 0; for (int i = 0; i + 1 < pts.Length; i++) total += (pts[i + 1] - pts[i]).magnitude; float run = 0; const float H = 5.2f;
        for (int i = 0; i + 1 < pts.Length; i++)
        {
            Vector2 a = pts[i], b = pts[i + 1]; float len = (b - a).magnitude;
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetTexture("_BaseMap", skyT); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.05f); m.SetFloat("_Cull", 2);
            m.SetTexture("_EmissionMap", skyT); m.SetColor("_EmissionColor", Color.white); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            foreach (var pn in new[] { "_BaseMap", "_EmissionMap" }) { m.SetTextureScale(pn, new Vector2(len / total, 1)); m.SetTextureOffset(pn, new Vector2(run / total, 0)); }
            m = Save(m, "stage_cyc" + i);
            Vector3 mid = new Vector3((a.x + b.x) * 0.5f, H * 0.5f, (a.y + b.y) * 0.5f); float yaw = -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            rs.Add(Quad("Sky" + i, g, mid, new Vector2(len, H), m, new Vector3(0, yaw, 0), false).GetComponent<Renderer>()); run += len;
            /* a second cloth 4 mm in front, clear by night: the paper day (white, then orange) is faded in on it */
            var dq = Quad("SkyDay" + i, g, mid + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, -0.004f), new Vector2(len, H), DayCloth(), new Vector3(0, yaw, 0), false); dq.GetComponent<Renderer>().enabled = false;
        }
        var dusk = new Material(Shader.Find("Universal Render Pipeline/Lit")); dusk.SetColor("_BaseColor", new Color(0.9f, 0.5f, 0.3f)); dusk.SetFloat("_Smoothness", 0.05f); dusk.SetFloat("_Cull", 2); dusk.EnableKeyword("_EMISSION"); dusk.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; dusk.SetColor("_EmissionColor", new Color(1.0f, 0.46f, 0.18f) * 0.9f); Save(dusk, "stage_cyc_dusk");
        Rod("sky_roller", g, new Vector3(-2.5f, H + 0.07f, 4.9f), new Vector3(2.5f, H + 0.07f, 4.9f), 0.06f, CardM("roller", new Color(0.62f, 0.52f, 0.36f), 0.2f, 2));
        return rs;
    }

    /// The street on the stage floor: a carriageway of dark card in 1200 mm sheets, a zebra crossing where she stands, a manhole,
    /// a drain, a kerb of separate stones and a pavement of 400 mm flags.
    static void Street(Transform root)
    {
        var g = new GameObject("Street").transform; g.SetParent(root, false);
        var road = CardM("road", new Color(0.15f, 0.16f, 0.20f), 0.16f, 1); var paint = CardM("road_paint", new Color(0.78f, 0.78f, 0.72f), 0.12f);
        var flag = CardM("flag", new Color(0.46f, 0.47f, 0.48f), 0.10f); var flag2 = CardM("flag2", new Color(0.40f, 0.42f, 0.45f), 0.10f); var stone = CardM("kerb_stone", new Color(0.56f, 0.56f, 0.54f), 0.14f);
        var iron = CardM("iron", new Color(0.20f, 0.23f, 0.27f), 0.20f); var gap = CardM("gap", new Color(0.03f, 0.03f, 0.04f));
        const float Z0 = 0.60f, ZK = 2.55f, X0 = -3.0f, X1 = 3.0f;
        Box("road_bed", g, new Vector3(0, 0.001f, (Z0 + ZK) * 0.5f), new Vector3(X1 - X0, 0.002f, ZK - Z0), gap);
        for (int i = 0; i < 6; i++) Slab(g, "road_sheet" + i, X0 + i + 0.002f, X0 + i + 0.998f, Z0, ZK - 0.004f, 0.008f, 0.006f, FloorCuts(), road);
        Box("road_edge", g, new Vector3(0, 0.004f, Z0 - 0.02f), new Vector3(X1 - X0, 0.008f, 0.04f), stone);                 // a strip of stone where the road meets the stage boards
        foreach (float zc in new[] { 0.955f, 1.455f, 1.93f }) Box("zebra", g, new Vector3(-0.5f, 0.009f, zc), new Vector3(1.60f, 0.002f, zc > 1.9f ? 0.20f : 0.25f), paint);      // she stands on the crossing, between the two grooves
        for (int k = 0; k < 9; k++) { float dx = X0 + 0.40f + 0.66f * k; if (dx > -1.5f && dx < 0.5f) continue; Box("centre_dash", g, new Vector3(dx, 0.009f, 1.43f), new Vector3(0.36f, 0.002f, 0.05f), paint); }
        // the worn lips of the grooves: raised paper fibre every 20-60 mm, 1-3 mm high
        foreach (float gz in new[] { G1, G2 }) foreach (float sd in new[] { -1f, 1f })
            for (float fx = -0.8f; fx < 0.8f; fx += 0.012f + 0.10f * (float)rnd.NextDouble() * (float)rnd.NextDouble()) Box("fray", g, new Vector3(fx, 0.0086f, gz + sd * (GW * 0.5f + 0.001f + 0.003f * (float)rnd.NextDouble())), new Vector3(0.004f + 0.018f * (float)rnd.NextDouble(), 0.0006f + 0.0014f * (float)rnd.NextDouble(), 0.0015f + 0.004f * (float)rnd.NextDouble()), CardM("fray", new Color(0.40f, 0.38f, 0.36f), 0.04f, 2), new Vector3(0, 50f * (float)rnd.NextDouble() - 25f, 0));
        // spike marks, as on any stage floor: crosses and corners of pale tape where the singer, the red paper and the paper people stand
        var tape = CardM("tape", new Color(0.86f, 0.80f, 0.52f), 0.30f); var tape2 = CardM("tape2", new Color(0.52f, 0.66f, 0.72f), 0.30f);
        void Cross(Vector3 at, Material m, float yaw) { var c = new GameObject("spike").transform; c.SetParent(g, false); c.localPosition = at; c.localEulerAngles = new Vector3(0, yaw, 0); Box("spike_a", c, Vector3.zero, new Vector3(0.07f, 0.0008f, 0.014f), m, new Vector3(0, 45, 0)); Box("spike_b", c, new Vector3(0, 0.0004f, 0), new Vector3(0.07f, 0.0008f, 0.014f), m, new Vector3(0, -45, 0)); }
        void Corner(Vector3 at, Material m, float yaw) { var c = new GameObject("spike").transform; c.SetParent(g, false); c.localPosition = at; c.localEulerAngles = new Vector3(0, yaw, 0); Box("spike_a", c, new Vector3(0.03f, 0, 0), new Vector3(0.07f, 0.0008f, 0.014f), m); Box("spike_b", c, new Vector3(0, 0.0004f, 0.03f), new Vector3(0.014f, 0.0008f, 0.07f), m); }
        Corner(new Vector3(-0.62f, 0.0086f, 1.20f), tape, 0); Corner(new Vector3(-0.38f, 0.0086f, 1.20f), tape, 90); Corner(new Vector3(0.52f, 0.0086f, 1.44f), tape, 14);
        for (int k = 0; k < 6; k++) { Cross(new Vector3(-1.5f + 0.6f * k, 0.0086f, G1 + 0.07f), tape2, 14f * k); Cross(new Vector3(-1.35f + 0.6f * k, 0.0086f, G2 - 0.08f), tape2, -11f * k); }
        for (int k = 0; k < 5; k++) Box("chalk", g, new Vector3(0.9f + 0.045f * k, 0.0084f, 1.86f + 0.02f * (k % 2)), new Vector3(0.03f, 0.0004f, 0.004f), CardM("chalk", new Color(0.82f, 0.82f, 0.80f)), new Vector3(0, 20f - 9f * k, 0));
        // manhole: a disc with sixteen radial slots and a rim
        var mh = new Vector3(1.75f, 0.008f, 1.35f); Cyl("manhole_rim", g, mh, 0.32f, 0.008f, stone); Cyl("manhole", g, mh + Vector3.up * 0.004f, 0.30f, 0.008f, iron);
        for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8f; Box("mh_slot", g, mh + new Vector3(Mathf.Sin(a) * 0.18f, 0.0085f, Mathf.Cos(a) * 0.18f), new Vector3(0.012f, 0.002f, 0.14f), gap, new Vector3(0, a * Mathf.Rad2Deg, 0)); }
        Cyl("mh_hub", g, mh + Vector3.up * 0.009f, 0.06f, 0.003f, stone);
        // drain grate against the kerb
        var dr = new Vector3(1.20f, 0.008f, ZK - 0.16f); Box("drain_pit", g, dr, new Vector3(0.40f, 0.004f, 0.25f), gap); Box("drain_frame_a", g, dr + new Vector3(0, 0.003f, 0.115f), new Vector3(0.40f, 0.008f, 0.02f), iron); Box("drain_frame_b", g, dr + new Vector3(0, 0.003f, -0.115f), new Vector3(0.40f, 0.008f, 0.02f), iron);
        for (int k = 0; k < 8; k++) Box("drain_bar", g, dr + new Vector3(-0.175f + 0.05f * k, 0.003f, 0), new Vector3(0.022f, 0.008f, 0.25f), iron);
        // kerb stones, 600 long, standing 40 above the road
        for (int k = 0; k < 35; k++) Box("kerb", g, new Vector3(X0 + 6f * (k + 0.5f) / 35f, 0.02f, ZK + 0.075f), new Vector3(6f / 35f - 0.003f, 0.04f, 0.15f), stone);
        // pavement: a bed and 400 mm flags with 6 mm joints, from the kerb back to the far row; the houses stand on it
        Box("pave_bed", g, new Vector3(0, 0.014f, (ZK + 0.15f + 4.9f) * 0.5f), new Vector3(X1 - X0, 0.028f, 4.9f - ZK - 0.15f), gap);
        // flags at the scale of the houses: 115 x 115 x 6 with 2 mm joints, everywhere the houses do not stand
        for (int iz = 0; iz < 14; iz++) for (int ix = 0; ix < 52; ix++)
        {
            float cx = X0 + 0.0577f + 0.11538f * ix, cz = ZK + 0.15f + 0.0577f + 0.11538f * iz;
            if (cx > 0.52f && cx < 2.53f && cz > 3.02f && cz < 3.78f) continue; if (cx > -2.45f && cx < -0.49f && cz > 3.17f && cz < 3.93f) continue;
            Box("flag", g, new Vector3(cx, 0.031f, cz), new Vector3(0.1134f, 0.006f, 0.1134f), (ix * 7 + iz * 3) % 4 == 0 ? flag2 : flag);
        }
    }
