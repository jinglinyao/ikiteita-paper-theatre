    // ---------------------------------------------------------------- scenery that fills the stage box
    static Material Iron => CardM("iron", new Color(0.20f, 0.23f, 0.27f), 0.20f);
    static Material Core => CardM("core", new Color(0.86f, 0.80f, 0.68f), 0.05f, 1);
    static Material Stud => CardM("stud", new Color(0.70f, 0.60f, 0.44f), 0.12f, 2);
    static Material Label => CardM("label", new Color(0.82f, 0.78f, 0.66f));

    /// the back of a flat: studs 40 x 60 every 500, one diagonal brace, a pencilled number plate, glue tabs every 400
    static void FlatBack(Transform f, float w, float h)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(w / 0.5f));
        for (int k = 0; k <= n; k++) Box("stud", f, new Vector3(0.02f + (w - 0.04f) * k / n, h * 0.5f, -0.03f), new Vector3(0.04f, h - 0.02f, 0.06f), Stud);
        foreach (float yy in new[] { 0.03f, h * 0.5f, h - 0.03f }) Box("rail", f, new Vector3(w * 0.5f, yy, -0.02f), new Vector3(w - 0.08f, 0.05f, 0.04f), Stud);
        float len = Mathf.Sqrt(w * w + h * h * 0.25f) - 0.1f, ang = Mathf.Atan2(h * 0.5f, w) * Mathf.Rad2Deg;
        Box("brace", f, new Vector3(w * 0.5f, h * 0.25f, -0.045f), new Vector3(len, 0.04f, 0.024f), Stud, new Vector3(0, 0, ang));
        Box("number", f, new Vector3(w * 0.5f, h * 0.72f, -0.0015f), new Vector3(0.12f, 0.08f, 0.003f), Label);
        for (int q = 0; q < 3; q++) Box("number_stroke", f, new Vector3(w * 0.5f - 0.03f + 0.03f * q, h * 0.72f, -0.0034f), new Vector3(0.006f, 0.04f - 0.008f * q, 0.001f), Iron, new Vector3(0, 0, 12f * (q - 1)));
        for (float ty = 0.25f; ty < h - 0.1f; ty += 0.4f) foreach (float tx in new[] { 0.07f, w - 0.07f }) Box("tab", f, new Vector3(tx, ty, -0.0015f), new Vector3(0.04f, 0.12f, 0.003f), Label);
    }

    /// a blind window on a wall that has no opening: frame, bars, sill, a dark pane
    static void BlindWindow(Transform f, Rect r, Material trim)
    {
        WindowTrim(f, r, 0, 0.004f, trim, Core, 1, true, true, false);
        Box("blind_pane", f, new Vector3(r.center.x, r.center.y, -0.002f), new Vector3(r.width, r.height, 0.004f), CardM("far_win", new Color(0.06f, 0.07f, 0.11f)));
    }

    /// what the two houses lacked: windows and a pipe on the side walls, a framed back, tiles on all four sides, a ridge, a chimney
    static void FinishHouse(Transform g, float W, float H, float D, Material wall, Material trim, Material tile, bool tiled)
    {
        Box("corner_l", g, new Vector3(0.002f, H * 0.5f, 0.015f), new Vector3(0.004f, H, 0.03f), Core); Box("corner_r", g, new Vector3(W - 0.002f, H * 0.5f, 0.015f), new Vector3(0.004f, H, 0.03f), Core);      // the cut edge of the front sheet shows its pale core
        var r = WallFrame(g, "SideR", new Vector3(W, 0, 0), -90); var l = WallFrame(g, "SideL", new Vector3(0, 0, D), 90); var b = WallFrame(g, "Back", new Vector3(W, 0, D), 180);
        foreach (var f in new[] { r, l })
        {
            foreach (float yy in new[] { 1.06f, 1.68f, 2.30f }) BlindWindow(f, new Rect(D * 0.5f - 0.13f, yy - 0.18f, 0.26f, 0.36f), trim);
            Box("band", f, new Vector3(D * 0.5f, 0.80f, -0.008f), new Vector3(D, 0.03f, 0.016f), trim); Pipe(f, D - 0.09f, -0.03f, 0, H, CardM("pipe", new Color(0.30f, 0.36f, 0.40f), 0.25f));
            Box("vent", f, new Vector3(0.16f, 0.40f, -0.012f), new Vector3(0.16f, 0.12f, 0.024f), trim); for (int k = 0; k < 4; k++) Box("vent_slat", f, new Vector3(0.16f, 0.36f + 0.026f * k, -0.026f), new Vector3(0.13f, 0.008f, 0.006f), Iron);
        }
        FlatBack(b, W, H);
        if (!tiled)
        {
            // a flat roof: gravel-coloured felt in strips, a hatch, a vent stack
            for (int k = 0; k < 4; k++) Box("felt", g, new Vector3(W * 0.5f, H + 0.105f, 0.12f + (D - 0.24f) * (k + 0.5f) / 4f), new Vector3(W - 0.10f, 0.006f, (D - 0.24f) / 4f - 0.006f), tile);
            Box("hatch", g, new Vector3(W * 0.3f, H + 0.16f, D * 0.55f), new Vector3(0.24f, 0.10f, 0.24f), wall); Box("hatch_lid", g, new Vector3(W * 0.3f, H + 0.22f, D * 0.55f), new Vector3(0.28f, 0.02f, 0.28f), trim);
            Cyl("stack", g, new Vector3(W * 0.72f, H + 0.26f, D * 0.5f), 0.035f, 0.30f, trim); Cyl("stack_cap", g, new Vector3(W * 0.72f, H + 0.42f, D * 0.5f), 0.05f, 0.02f, trim);
            return;
        }
        // tiles: barrel ends along the other three edges of every course (the front edge has them already), a ridge, a chimney
        for (int k = 0; k < 6; k++)
        {
            float cw = W + 0.26f - 0.30f * k, cd = D + 0.26f - 0.14f * k, cy = H + 0.065f + 0.058f * k; if (cw < 0.3f || cd < 0.1f) break;
            int n = Mathf.RoundToInt(cw / 0.064f); for (int i = 0; i < n; i++) Cyl("tile_b", g, new Vector3(W * 0.5f - cw * 0.5f + cw * (i + 0.5f) / n, cy + 0.024f, D * 0.5f + cd * 0.5f - 0.05f), 0.030f, 0.12f, tile, new Vector3(90, 0, 0));
            int m = Mathf.Max(1, Mathf.RoundToInt((cd - 0.2f) / 0.064f)); for (int i = 0; i < m; i++) foreach (float sx in new[] { -1f, 1f }) Cyl("tile_s", g, new Vector3(W * 0.5f + sx * (cw * 0.5f - 0.05f), cy + 0.024f, D * 0.5f - (cd - 0.2f) * 0.5f + (cd - 0.2f) * (i + 0.5f) / m), 0.030f, 0.12f, tile, new Vector3(0, 0, 90));
        }
        Box("ridge", g, new Vector3(W * 0.5f, H + 0.065f + 0.058f * 6 - 0.004f, D * 0.5f), new Vector3(W - 1.5f, 0.05f, 0.08f), trim);
        var ch = new Vector3(W * 0.78f, H + 0.42f, D * 0.62f); Box("chimney", g, ch, new Vector3(0.22f, 0.40f, 0.22f), wall); Box("chimney_cap", g, ch + Vector3.up * 0.215f, new Vector3(0.27f, 0.03f, 0.27f), trim); Box("chimney_pot", g, ch + Vector3.up * 0.27f, new Vector3(0.10f, 0.08f, 0.10f), tile);
    }

    /// One wing: the front of a narrow house, 880 x 4600, three dark windows, turned 20 degrees toward the middle of the stage.
    static void WingFlat(Transform root, string name, Vector3 at, float yaw, int seed)
    {
        const float W = 0.88f, H = 4.6f, T = 0.03f;
        var g = new GameObject(name).transform; g.SetParent(root, false); g.localPosition = at; g.localEulerAngles = new Vector3(0, yaw, 0);
        Color[] tints = { new Color(0.30f, 0.36f, 0.48f), new Color(0.34f, 0.34f, 0.44f), new Color(0.26f, 0.34f, 0.42f) };
        var wall = CardM("wing_wall" + seed % 3, tints[seed % 3]); var trim = CardM("wing_trim", new Color(0.52f, 0.54f, 0.56f));
        var holes = new List<Rect>(); foreach (float yy in new[] { 1.15f, 2.15f, 3.15f }) holes.Add(new Rect(W * 0.5f - 0.17f, yy - 0.24f, 0.34f, 0.48f));
        Wall(g, 0, 0, W, H, 0, T, holes, wall);
        for (int i = 0; i < holes.Count; i++) { WindowTrim(g, holes[i], 0, T, trim, Core, 1, true, true, seed % 2 == 0); Room(g, holes[i], T, "sil_clock", 1f, false, null, null, 900 + seed * 10 + i); }
        Box("plinth", g, new Vector3(W * 0.5f, 0.20f, -0.012f), new Vector3(W, 0.40f, 0.024f), trim); Box("band", g, new Vector3(W * 0.5f, 0.74f, -0.008f), new Vector3(W, 0.03f, 0.016f), trim);
        foreach (float yy in new[] { 1.72f, 2.72f, 3.72f }) Box("string", g, new Vector3(W * 0.5f, yy, -0.006f), new Vector3(W, 0.022f, 0.012f), trim);
        Box("cornice1", g, new Vector3(W * 0.5f, H - 0.30f, -0.03f), new Vector3(W + 0.04f, 0.05f, 0.06f), trim); Box("cornice2", g, new Vector3(W * 0.5f, H - 0.25f, -0.045f), new Vector3(W + 0.08f, 0.04f, 0.09f), trim);
        for (int k = 0; k < 7; k++) Box("bracket", g, new Vector3(0.06f + (W - 0.12f) * k / 6f, H - 0.36f, -0.025f), new Vector3(0.04f, 0.07f, 0.05f), trim);
        Pipe(g, seed % 2 == 0 ? 0.07f : W - 0.07f, -0.03f, 0, H - 0.4f, CardM("pipe", new Color(0.30f, 0.36f, 0.40f), 0.25f));
        Box("edge_l", g, new Vector3(0.002f, H * 0.5f, T * 0.5f), new Vector3(0.004f, H, T), Core); Box("edge_r", g, new Vector3(W - 0.002f, H * 0.5f, T * 0.5f), new Vector3(0.004f, H, T), Core);
        var back = WallFrame(g, "Back", new Vector3(W, 0, T), 180); FlatBack(back, W, H);
        Box("foot", g, new Vector3(W * 0.5f, 0.02f, T + 0.11f), new Vector3(0.5f, 0.04f, 0.20f), Stud); Box("foot_brace", g, new Vector3(W * 0.5f, 0.40f, T + 0.11f), new Vector3(0.04f, 0.82f, 0.03f), Stud, new Vector3(-13, 0, 0));
    }

    /// a border: a strip of dark card across the stage, its lower edge cut into cloud scallops, hung on three lines
    static void Border(Transform root, int idx, float z)
    {
        var g = new GameObject("Border" + idx).transform; g.SetParent(root, false); g.localPosition = new Vector3(0, 0, z);
        var m = CardM("border" + idx % 2, idx % 2 == 0 ? new Color(0.12f, 0.16f, 0.28f) : new Color(0.15f, 0.19f, 0.32f));
        Box("strip", g, new Vector3(0, 4.05f, 0), new Vector3(3.96f, 0.50f, 0.012f), m);
        float x = -1.98f; int k = 0; while (x < 1.78f) { float r = 0.16f + 0.12f * (float)rnd.NextDouble(); Cyl("scallop", g, new Vector3(x + r * 0.8f, 3.80f + 0.05f * (k % 3), 0.001f * (k % 2)), r, 0.012f, m, new Vector3(90, 0, 0)); x += r * 1.5f; k++; }
        Box("border_lath", g, new Vector3(0, 4.31f, 0.012f), new Vector3(3.96f, 0.04f, 0.03f), Stud);
        foreach (float sx in new[] { -1.8f, 0f, 1.8f }) Rod("border_line", g, new Vector3(sx, 4.32f, 0.012f), new Vector3(sx, LOFT - 0.2f, 0.012f), 0.004f, Label);
    }

    /// far houses with real openings; `lit` of the panes can glow (their renderers are returned), the rest are dark paper
    static void FarRowBuilt(Transform root, float z, List<Renderer> farPanes)
    {
        var g = new GameObject("FarRow").transform; g.SetParent(root, false); g.localPosition = new Vector3(0, 0, z);
        var parT = Tex(ART + "pane_washi.png", false, false, false); var paneLit = Paper("room_parchment", parT, parT, Color.black, false); var darkPane = CardM("far_win", new Color(0.06f, 0.07f, 0.11f));
        var tops = new List<Vector3>(); float x = -2.92f; int k = 0;
        while (x < 2.90f)
        {
            float w = Mathf.Min(0.52f + 0.30f * (float)rnd.NextDouble(), 2.92f - x), h = 0.62f + 0.62f * (float)rnd.NextDouble(), d = 0.30f, c = 0.26f + 0.10f * (float)rnd.NextDouble(); if (2.92f - (x + w) < 0.35f) w = 2.92f - x;
            var hg = new GameObject("Far" + k).transform; hg.SetParent(g, false); hg.localPosition = new Vector3(x, 0, 0.04f * (k % 3));
            var wall = CardM("far" + (k % 4), new Color(c * 0.86f, c * 0.96f, c * 1.16f)); var trim = CardM("far_trim", new Color(0.44f, 0.46f, 0.50f)); var roof = CardM("far_roof" + (k % 3), new Color(0.30f + 0.06f * (k % 3), 0.30f, 0.34f));
            int cols = Mathf.Max(2, (int)(w / 0.17f)), rows = Mathf.Max(2, (int)((h - 0.16f) / 0.21f)); var holes = new List<Rect>();
            for (int j = 0; j < rows; j++) for (int i = 0; i < cols; i++) holes.Add(new Rect(w * (i + 0.5f) / cols - 0.045f, 0.16f + (h - 0.22f) * (j + 0.5f) / rows - 0.065f, 0.09f, 0.13f));
            Wall(hg, 0, 0, w, h, 0, 0.02f, holes, wall);
            for (int q = 0; q < holes.Count; q++)
            {
                var r = holes[q];
                Box("fw_t", hg, new Vector3(r.center.x, r.yMax + 0.006f, -0.004f), new Vector3(r.width + 0.024f, 0.012f, 0.008f), trim); Box("fw_b", hg, new Vector3(r.center.x, r.yMin - 0.008f, -0.008f), new Vector3(r.width + 0.036f, 0.012f, 0.016f), trim);
                Box("fw_l", hg, new Vector3(r.xMin - 0.006f, r.center.y, -0.004f), new Vector3(0.012f, r.height, 0.008f), trim); Box("fw_r", hg, new Vector3(r.xMax + 0.006f, r.center.y, -0.004f), new Vector3(0.012f, r.height, 0.008f), trim);
                Box("fw_bar", hg, new Vector3(r.center.x, r.center.y, 0.006f), new Vector3(0.006f, r.height, 0.006f), trim);
                float wx = x + r.center.x; bool glow = wx > -0.62f && wx < 0.62f && farPanes.Count < 20;
                if (glow) farPanes.Add(Quad("FarPane", hg, new Vector3(r.center.x, r.center.y, 0.10f), new Vector2(r.width + 0.03f, r.height + 0.03f), paneLit, default, false).GetComponent<Renderer>());
                if (!glow && (q + k) % 3 == 0) Box("fw_blind", hg, new Vector3(r.center.x, r.yMax - r.height * 0.3f, 0.022f), new Vector3(r.width, r.height * 0.6f, 0.003f), CardM("blind", new Color(0.46f, 0.50f, 0.56f)));
                if (glow) { } else Box("fw_dark", hg, new Vector3(r.center.x, r.center.y, 0.10f), new Vector3(r.width + 0.03f, r.height + 0.03f, 0.004f), darkPane);
                foreach (float sx in new[] { -1f, 1f }) Box("fw_cheek", hg, new Vector3(r.center.x + sx * (r.width * 0.5f + 0.012f), r.center.y, 0.06f), new Vector3(0.006f, r.height + 0.03f, 0.08f), darkPane);
                Box("fw_ceil", hg, new Vector3(r.center.x, r.yMax + 0.012f, 0.06f), new Vector3(r.width + 0.03f, 0.006f, 0.08f), darkPane); Box("fw_floor", hg, new Vector3(r.center.x, r.yMin - 0.012f, 0.06f), new Vector3(r.width + 0.03f, 0.006f, 0.08f), darkPane);
            }
            Box("far_l", hg, new Vector3(0.01f, h * 0.5f, d * 0.5f + 0.01f), new Vector3(0.02f, h, d - 0.02f), wall); Box("far_r", hg, new Vector3(w - 0.01f, h * 0.5f, d * 0.5f + 0.01f), new Vector3(0.02f, h, d - 0.02f), wall);
            Box("far_back", hg, new Vector3(w * 0.5f, h * 0.5f, d - 0.01f), new Vector3(w - 0.04f, h, 0.02f), wall); Box("far_top", hg, new Vector3(w * 0.5f, h + 0.02f, d * 0.5f), new Vector3(w + 0.05f, 0.04f, d + 0.06f), roof);
            Box("far_cornice", hg, new Vector3(w * 0.5f, h - 0.03f, -0.012f), new Vector3(w + 0.03f, 0.024f, 0.03f), trim); Box("far_string", hg, new Vector3(w * 0.5f, 0.16f + (h - 0.22f) / rows, -0.004f), new Vector3(w, 0.012f, 0.008f), trim); for (int c2 = 0; c2 <= cols; c2++) Box("far_pilaster", hg, new Vector3(Mathf.Clamp(w * c2 / cols, 0.012f, w - 0.012f), h * 0.5f + 0.06f, -0.004f), new Vector3(0.018f, h - 0.16f, 0.008f), wall);
            Box("far_parapet", hg, new Vector3(w * 0.5f, h + 0.06f, 0.012f), new Vector3(w, 0.05f, 0.024f), trim); Box("far_base", hg, new Vector3(w * 0.5f, 0.07f, -0.006f), new Vector3(w, 0.14f, 0.012f), trim);
            for (float sy = 0.2f; sy < h - 0.05f; sy += 0.25f) Box("far_stud", hg, new Vector3(w * 0.5f, sy, d + 0.01f), new Vector3(w - 0.06f, 0.03f, 0.02f), Stud);                     // the back of each far house is framed too
            Box("far_number", hg, new Vector3(w * 0.5f, h * 0.6f, d + 0.0015f), new Vector3(0.10f, 0.06f, 0.003f), Label);
            if (k % 3 == 1) { Cyl("far_tank", hg, new Vector3(w * 0.3f, h + 0.12f, d * 0.5f), 0.06f, 0.10f, roof); Cyl("far_tank_lid", hg, new Vector3(w * 0.3f, h + 0.175f, d * 0.5f), 0.066f, 0.01f, trim); }
            if (k % 3 == 2) { Box("far_stack", hg, new Vector3(w * 0.7f, h + 0.13f, d * 0.6f), new Vector3(0.07f, 0.18f, 0.07f), wall); Box("far_stack_cap", hg, new Vector3(w * 0.7f, h + 0.225f, d * 0.6f), new Vector3(0.09f, 0.012f, 0.09f), trim); }
            if (k % 2 == 0) { var top = new Vector3(x + w - 0.03f, 1.48f, 0.12f); Rod("pole", g, new Vector3(top.x, 0, top.z), top, 0.012f, Iron); Box("pole_x", g, top + Vector3.down * 0.08f, new Vector3(0.16f, 0.012f, 0.012f), Iron); Cyl("insulator", g, top + new Vector3(0.07f, -0.06f, 0), 0.008f, 0.03f, trim); Cyl("insulator", g, top + new Vector3(-0.07f, -0.06f, 0), 0.008f, 0.03f, trim); tops.Add(top + Vector3.down * 0.08f); }
            x += w; k++;
        }
        for (int i = 0; i + 1 < tops.Count; i++) foreach (float off in new[] { -0.07f, 0.07f }) { Vector3 a = tops[i] + Vector3.right * off, b = tops[i + 1] + Vector3.right * off; for (int sgm = 0; sgm < 6; sgm++) { float u0 = sgm / 6f, u1 = (sgm + 1) / 6f; Rod("wire", g, Vector3.Lerp(a, b, u0) + Vector3.down * 0.28f * u0 * (1 - u0), Vector3.Lerp(a, b, u1) + Vector3.down * 0.28f * u1 * (1 - u1), 0.0025f, Iron); } }
    }

    /// clouds of three layers of card each, 30 mm apart, hung on lines from the last batten
    static Transform CloudsBuilt(Transform root)
    {
        var all = new GameObject("Clouds").transform; all.SetParent(root, false);
        Color[] tint = { new Color(0.56f, 0.60f, 0.68f), new Color(0.46f, 0.52f, 0.62f), new Color(0.36f, 0.42f, 0.54f) };
        Vector3[] at = { new Vector3(-1.9f, 3.32f, 2.60f), new Vector3(-0.6f, 3.22f, 2.66f), new Vector3(1.5f, 3.36f, 2.58f) };      // none of them over the lamp post
        for (int c = 0; c < at.Length; c++)
        {
            var g = new GameObject("Cloud" + c).transform; g.SetParent(all, false); g.localPosition = at[c];
            for (int L = 0; L < 3; L++)
            {
                var m = CardM("cloud" + L, tint[L]); float z = 0.03f * L, sc = 1f - 0.12f * L, w = 0.90f * sc; int n = 5 + (c + L) % 2;
                Box("cloud_base", g, new Vector3(0.05f * L, -0.05f + 0.04f * L, z), new Vector3(w, 0.14f, 0.012f), m);
                for (int i = 0; i < n; i++) { float r = (0.14f + 0.10f * (float)rnd.NextDouble()) * sc; Cyl("puff", g, new Vector3(0.05f * L - w * 0.5f + r * 0.6f + (w - r * 1.2f) * i / (n - 1), 0.02f + 0.04f * L + 0.07f * ((i + L) % 2) + r * 0.3f, z), r, 0.012f, m, new Vector3(90, 0, 0)); }
                if (L == 0) for (int q = 0; q < 6; q++) { float cr = 0.05f + 0.05f * (float)rnd.NextDouble(); Cyl("curl", g, new Vector3(-w * 0.36f + w * 0.72f * q / 5f, 0.02f + 0.10f * ((q * 3) % 4) / 3f, -0.012f), cr, 0.012f, CardM("cloud_curl", new Color(0.64f, 0.68f, 0.76f)), new Vector3(90, 0, 0)); Cyl("curl_in", g, new Vector3(-w * 0.36f + w * 0.72f * q / 5f + cr * 0.2f, 0.02f + 0.10f * ((q * 3) % 4) / 3f - cr * 0.15f, -0.021f), cr * 0.55f, 0.006f, m, new Vector3(90, 0, 0)); }
                if (L < 2) foreach (float sx in new[] { -0.2f, 0.2f }) Box("cloud_spacer", g, new Vector3(sx, 0, z + 0.015f), new Vector3(0.03f, 0.03f, 0.018f), Stud);
            }
            foreach (float sx in new[] { -0.24f, 0.24f }) Rod("cloud_line", g, new Vector3(sx, 0.12f, 0.03f), new Vector3(sx, BATTEN - at[c].y, 0.03f), 0.0025f, Label);
        }
        return all;
    }

    /// the paper sun on its slide rod in front of the sky
    static Transform SunBuilt(Transform root)
    {
        var g = new GameObject("PaperSun").transform; g.SetParent(root, false); g.localPosition = new Vector3(-0.17f, 2.4f, 4.70f);                 // in the gap between the two houses, where it can be seen from the front
        var sunA = Emit("sun_a_glow", new Color(0.004f, 0.002f, 0f)); sunA.SetColor("_BaseColor", new Color(0.92f, 0.56f, 0.20f)); var sunB = Emit("sun_b_glow", new Color(0.004f, 0.003f, 0f)); sunB.SetColor("_BaseColor", new Color(0.98f, 0.80f, 0.46f));      /* oiled paper with a light behind it: the clock makes it glow */
        Cyl("sun_back", g, Vector3.zero, 0.31f, 0.012f, sunA, new Vector3(90, 0, 0)).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off; Cyl("sun_front", g, new Vector3(0, 0, -0.02f), 0.235f, 0.012f, sunB, new Vector3(90, 0, 0)).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6f; Box("sun_ray", g, new Vector3(Mathf.Sin(a) * 0.385f, Mathf.Cos(a) * 0.385f, 0), new Vector3(0.036f, 0.11f, 0.012f), sunA, new Vector3(0, 0, -a * Mathf.Rad2Deg)).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off; }
        Box("sun_collar", g, new Vector3(0, 0, 0.03f), new Vector3(0.06f, 0.10f, 0.05f), Stud);
        var rod = new GameObject("SunRod").transform; rod.SetParent(root, false); Rod("sun_rod", rod, new Vector3(-0.17f, 0.04f, 4.76f), new Vector3(-0.17f, 5.0f, 4.76f), 0.012f, Iron); Box("sun_rod_foot", rod, new Vector3(-0.17f, 0.06f, 4.76f), new Vector3(0.14f, 0.04f, 0.10f), Stud);
        return g;
    }

    /// the house curtain in red paper: two leaves of pleated card on a rod, drawn open and gathered at the sides, two draw lines
    static void PaperCurtain(Transform root)
    {
        var g = new GameObject("PaperCurtain").transform; g.SetParent(root, false);
        var red = CardM("curtain_red", new Color(0.50f, 0.06f, 0.07f), 0.10f, 1); var red2 = CardM("curtain_red2", new Color(0.42f, 0.05f, 0.06f), 0.10f, 1); var brass = CardM("brass", new Color(0.72f, 0.55f, 0.24f), 0.45f);
        Rod("curtain_rod", g, new Vector3(-2.3f, 3.13f, 0.12f), new Vector3(2.3f, 3.13f, 0.12f), 0.015f, brass);
        foreach (float sd in new[] { -1f, 1f })
            for (int k = 0; k < 12; k++)
            {
                float x = sd * (2.28f - 0.034f * k); var p = new GameObject("pleat_" + (sd < 0 ? "L" : "R") + "_" + k).transform; p.SetParent(g, false); p.localPosition = new Vector3(x, 0, 0.12f); p.localEulerAngles = new Vector3(0, (k % 2 == 0 ? 66 : -66) * sd, 0);
                Box("leaf", p, new Vector3(0, 1.56f, 0), new Vector3(0.17f, 3.10f, 0.006f), k % 2 == 0 ? red : red2); Cyl("ring", p, new Vector3(0, 3.13f, 0), 0.022f, 0.006f, brass, new Vector3(0, 0, 90));
                if (k % 3 == 0) Box("hem", p, new Vector3(0, 0.03f, -0.004f), new Vector3(0.17f, 0.05f, 0.003f), brass);
            }
        foreach (float sd in new[] { -1f, 1f }) { Rod("draw_line", g, new Vector3(sd * 2.34f, 3.13f, 0.14f), new Vector3(sd * 2.34f, 0.5f, 0.14f), 0.004f, Label); Box("draw_weight", g, new Vector3(sd * 2.34f, 0.44f, 0.14f), new Vector3(0.05f, 0.12f, 0.05f), Stud); Cyl("pulley", g, new Vector3(sd * 2.34f, 3.16f, 0.14f), 0.03f, 0.012f, brass, new Vector3(90, 0, 0)); }
    }

    /// street furniture folded from card at the scale of the houses (about 1 : 3.5)
    static void StreetProps(Transform root)
    {
        var g = new GameObject("StreetProps").transform; g.SetParent(root, false); const float Y = 0.04f;
        var ochre = CardM("prop_ochre", new Color(0.78f, 0.60f, 0.22f), 0.14f); var teal = CardM("prop_teal", new Color(0.20f, 0.40f, 0.42f), 0.14f); var grey = CardM("prop_grey", new Color(0.52f, 0.54f, 0.56f), 0.16f);
        var wood = CardM("prop_wood", new Color(0.46f, 0.32f, 0.20f), 0.12f, 2); var green = CardM("prop_leaf", new Color(0.22f, 0.40f, 0.26f)); var dark = CardM("gap", new Color(0.03f, 0.03f, 0.04f));
        // hydrant, 230 high
        var h = new Vector3(-0.42f, Y, 2.78f); Cyl("hyd_flange", g, h + Vector3.up * 0.006f, 0.05f, 0.012f, ochre); Cyl("hyd_body", g, h + Vector3.up * 0.09f, 0.034f, 0.16f, ochre); Cyl("hyd_collar", g, h + Vector3.up * 0.165f, 0.04f, 0.012f, ochre);
        Ball("hyd_dome", g, h + Vector3.up * 0.175f, 0.034f, ochre); Cyl("hyd_nut", g, h + Vector3.up * 0.215f, 0.010f, 0.02f, grey); foreach (float sx in new[] { -1f, 1f }) Cyl("hyd_cap", g, h + new Vector3(sx * 0.042f, 0.12f, 0), 0.018f, 0.022f, grey, new Vector3(0, 0, 90)); Cyl("hyd_front", g, h + new Vector3(0, 0.10f, -0.042f), 0.022f, 0.022f, grey, new Vector3(90, 0, 0));
        // litter bin, dia 150 x 260, ribbed, with a hooded lid
        var b = new Vector3(2.32f, Y, 2.80f); Cyl("bin", g, b + Vector3.up * 0.12f, 0.075f, 0.24f, teal); Cyl("bin_rim", g, b + Vector3.up * 0.245f, 0.08f, 0.012f, grey); Cyl("bin_lid", g, b + Vector3.up * 0.275f, 0.07f, 0.05f, teal); Box("bin_mouth", g, b + new Vector3(0, 0.275f, -0.066f), new Vector3(0.07f, 0.028f, 0.012f), dark);
        for (int k = 0; k < 10; k++) { float a = k * Mathf.PI / 5f; Box("bin_rib", g, b + new Vector3(Mathf.Sin(a) * 0.076f, 0.12f, Mathf.Cos(a) * 0.076f), new Vector3(0.012f, 0.22f, 0.006f), grey, new Vector3(0, a * Mathf.Rad2Deg, 0)); }
        // bench, 500 x 240
        var bn = new Vector3(-1.75f, Y, 2.86f); for (int k = 0; k < 3; k++) Box("bench_seat", g, bn + new Vector3(0, 0.13f, -0.07f + 0.07f * k), new Vector3(0.50f, 0.014f, 0.06f), wood); for (int k = 0; k < 2; k++) Box("bench_back", g, bn + new Vector3(0, 0.20f + 0.07f * k, 0.105f), new Vector3(0.50f, 0.055f, 0.014f), wood, new Vector3(-10, 0, 0));
        foreach (float sx in new[] { -0.21f, 0.21f }) { Box("bench_leg", g, bn + new Vector3(sx, 0.065f, 0), new Vector3(0.02f, 0.13f, 0.20f), Iron); Box("bench_arm", g, bn + new Vector3(sx, 0.20f, 0.02f), new Vector3(0.02f, 0.014f, 0.20f), Iron); Box("bench_post", g, bn + new Vector3(sx, 0.20f, 0.10f), new Vector3(0.02f, 0.16f, 0.02f), Iron); }
        // planter with cut-paper leaves, 400 x 120 x 110
        var pl = new Vector3(1.05f, Y, 2.84f); Box("planter", g, pl + Vector3.up * 0.055f, new Vector3(0.40f, 0.11f, 0.12f), wood); Box("planter_rim", g, pl + Vector3.up * 0.115f, new Vector3(0.43f, 0.014f, 0.15f), wood); Box("planter_soil", g, pl + Vector3.up * 0.108f, new Vector3(0.37f, 0.006f, 0.09f), dark);
        for (int k = 0; k < 18; k++) { float lx = -0.17f + 0.02f * k, lh = 0.06f + 0.07f * (float)rnd.NextDouble(); Box("leaf", g, pl + new Vector3(lx, 0.11f + lh * 0.5f, 0.02f * (float)rnd.NextDouble() - 0.01f), new Vector3(0.022f, lh, 0.003f), green, new Vector3(14f * (float)rnd.NextDouble() - 7f, 60f * (float)rnd.NextDouble() - 30f, 30f * (float)rnd.NextDouble() - 15f)); }
        // pillar box, 160 x 380, not red
        var mb = new Vector3(-0.92f, Y, 2.80f); Cyl("post_box", g, mb + Vector3.up * 0.17f, 0.075f, 0.34f, teal); Cyl("post_cap", g, mb + Vector3.up * 0.35f, 0.085f, 0.02f, teal); Ball("post_dome", g, mb + Vector3.up * 0.352f, 0.072f, teal); Cyl("post_base", g, mb + Vector3.up * 0.015f, 0.085f, 0.03f, grey);
        Box("post_slot", g, mb + new Vector3(0, 0.27f, -0.074f), new Vector3(0.07f, 0.012f, 0.006f), dark); Box("post_plate", g, mb + new Vector3(0, 0.20f, -0.075f), new Vector3(0.06f, 0.05f, 0.004f), Label);
        // a ladder against each side wall, a crate, a broom: the wings are not empty
        foreach (float sx in new[] { -1f, 1f })
        {
            var ld = new GameObject("Ladder").transform; ld.SetParent(g, false); ld.localPosition = new Vector3(sx * 2.80f, 0, 3.75f); ld.localEulerAngles = new Vector3(0, 0, sx * 6f);
            foreach (float rz in new[] { -0.16f, 0.16f }) Box("ladder_rail", ld, new Vector3(0, 1.3f, rz), new Vector3(0.03f, 2.6f, 0.05f), Stud); for (int k = 0; k < 9; k++) Box("ladder_rung", ld, new Vector3(0, 0.25f + 0.27f * k, 0), new Vector3(0.024f, 0.024f, 0.32f), Stud);
            Box("crate", g, new Vector3(sx * 2.72f, 0.14f, 1.02f), new Vector3(0.36f, 0.28f, 0.30f), wood); for (int k = 0; k < 3; k++) Box("crate_slat", g, new Vector3(sx * 2.72f - sx * 0.181f, 0.06f + 0.09f * k, 1.02f), new Vector3(0.006f, 0.06f, 0.30f), Stud); Box("crate_lid", g, new Vector3(sx * 2.72f, 0.287f, 1.02f), new Vector3(0.38f, 0.014f, 0.32f), Stud);
        }
    }

    /// three stage lanterns on the first batten: a can, a yoke, a clamp, a barn door. They are models; the light comes from the plan's lights.
    static void Lanterns(Transform root)
    {
        // the ghost light: a bare bulb on a stand in the back passage, the one lamp a theatre never switches off
        var gl = new GameObject("GhostLamp").transform; gl.SetParent(root, false); gl.localPosition = new Vector3(2.4f, 0, 5.02f);
        Cyl("ghost_base", gl, new Vector3(0, 0.02f, 0), 0.12f, 0.04f, Iron); Rod("ghost_stand", gl, new Vector3(0, 0.04f, 0), new Vector3(0, 1.42f, 0), 0.012f, Iron); Cyl("ghost_socket", gl, new Vector3(0, 1.45f, 0), 0.02f, 0.06f, Stud);
        var gb = Prop("lightbulb_01", "lightbulb_01_2k.gltf", "GhostBulb", gl, new Vector3(0, 1.48f, 0), 0); if (gb != null) { var bb = BoundsOf(gb); gb.transform.position += new Vector3(gl.position.x - bb.center.x, gl.position.y + 1.47f - bb.min.y, gl.position.z - bb.center.z); foreach (var r in gb.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; r.name = "ghost_glass"; var ms = r.sharedMaterials; for (int k = 0; k < ms.Length; k++) { if (ms[k] == null) continue; var m2 = new Material(ms[k]); m2.EnableKeyword("_EMISSION"); m2.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; if (m2.HasProperty("_EmissionColor")) m2.SetColor("_EmissionColor", new Color(0.3f, 0.22f, 0.12f)); ms[k] = Save(m2, "ghost_glass_" + k); } var bg = Shader.Find("Stage/BulbGlow"); if (bg != null) { var gm = new Material(bg); gm.SetFloat("_CoreGain", 2.2f); gm.SetFloat("_Power", 3f); gm.SetFloat("_RimGain", 0.7f); ms[0] = Save(gm, "ghost_glass_glow"); } r.sharedMaterials = ms; } }      /* the glass itself glows when the lamp burns: white at the heart, orange at the rim (the clock sets _Gain) */
        Ball("ghost_glow", gl, new Vector3(0, 1.535f, 0), 0.016f, Emit("stage_ghost_glow", new Color(1f, 0.78f, 0.50f) * 3f)).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var g = new GameObject("Lanterns").transform; g.SetParent(root, false); var black = CardM("lantern", new Color(0.10f, 0.11f, 0.13f), 0.3f); var lens = CardM("lantern_lens", new Color(0.70f, 0.74f, 0.80f), 0.7f);
        foreach (float x in new[] { -1.6f, -0.2f, 1.5f })
        {
            var l = new GameObject("Lantern").transform; l.SetParent(g, false); l.localPosition = new Vector3(x, BATTEN, 1.0f);
            Box("clamp", l, new Vector3(0, -0.05f, 0), new Vector3(0.05f, 0.10f, 0.07f), black); Box("yoke_top", l, new Vector3(0, -0.11f, 0), new Vector3(0.26f, 0.016f, 0.03f), black);
            foreach (float sx in new[] { -0.125f, 0.125f }) Box("yoke_arm", l, new Vector3(sx, -0.21f, 0), new Vector3(0.012f, 0.20f, 0.03f), black);
            var can = new GameObject("can").transform; can.SetParent(l, false); can.localPosition = new Vector3(0, -0.28f, 0); can.localEulerAngles = new Vector3(52, x > 0 ? -14 : 12, 0);
            Cyl("can_body", can, Vector3.zero, 0.10f, 0.30f, black, new Vector3(90, 0, 0)); Cyl("can_lens", can, new Vector3(0, 0, 0.152f), 0.085f, 0.006f, lens, new Vector3(90, 0, 0)); Cyl("can_back", can, new Vector3(0, 0, -0.16f), 0.07f, 0.03f, black, new Vector3(90, 0, 0));
            foreach (var (dx, dy) in new[] { (0.11f, 0f), (-0.11f, 0f), (0f, 0.11f), (0f, -0.11f) }) Box("barn_door", can, new Vector3(dx, dy, 0.20f), new Vector3(dx != 0 ? 0.006f : 0.18f, dx != 0 ? 0.18f : 0.006f, 0.10f), black);
        }
    }
