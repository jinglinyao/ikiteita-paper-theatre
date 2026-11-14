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
            Awning(g, 0.61f, 0.78f, 0.94f, -0.004f, CardM("awn_a", new Color(0.20f, 0.36f, 0.40f)), CardM("awn_b", new Color(0.82f, 0.80f, 0.70f)));
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
        string[] sil = { "sil_bike", "sil_clock", "sil_cat", "sil_plant", "sil_lamp", "sil_guitar" }; /* nothing that hangs, nothing that could be taken for a figure */ int idx = 0;
        for (int i = 0; i < lit.Count; i++)
        {
            WindowTrim(g, holes[i], 0, T, trim, core, bars[i], style == 0 && !balc[i], !balc[i], style == 1);
            if (balc[i]) Balcony(g, holes[i], 0, trim, iron);
            string sn = sil[idx % sil.Length]; if (!silAspect.ContainsKey(sn)) sn = "sil_clock";
            Room(g, holes[i], T, sn, silAspect[sn], lit[i], panes, plights, style * 10 + i); if (lit[i]) idx++;
            if (style == 1 && (i * 5 + 2) % 3 != 0) Box("blind", g, new Vector3(holes[i].center.x, holes[i].yMax - holes[i].height * (0.20f + 0.10f * (i % 3)), 0.036f), new Vector3(holes[i].width, holes[i].height * (0.40f + 0.20f * (i % 3)), 0.004f), CardM("blind", new Color(0.46f, 0.50f, 0.56f)));      // blinds drawn to different heights: no two dark windows alike
        }
        FinishHouse(g, W, H, D, wall, trim, CardM("tile", new Color(0.34f, 0.33f, 0.38f), 0.14f), style == 0);
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
        var m = CardM("lamp_green", new Color(0.16f, 0.24f, 0.20f), 0.25f); var glass = new Material(Shader.Find("Universal Render Pipeline/Lit")); glass.SetColor("_BaseColor", new Color(0.30f, 0.24f, 0.16f)); glass.SetFloat("_Smoothness", 0.6f); glass.EnableKeyword("_EMISSION"); glass.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; glass.SetColor("_EmissionColor", new Color(1f, 0.70f, 0.36f) * 1.5f); glass = Save(glass, "kit_lamp_glass");
        Cyl("base", g, new Vector3(0, 0.16f, 0), 0.075f, 0.32f, m); Cyl("collar", g, new Vector3(0, 0.34f, 0), 0.09f, 0.03f, m); Rod("pole", g, new Vector3(0, 0.3f, 0), new Vector3(0, 2.95f, 0), 0.034f, m);
        Cyl("collar2", g, new Vector3(0, 2.2f, 0), 0.045f, 0.03f, m); Rod("arm", g, new Vector3(0, 2.80f, 0), new Vector3(0.34f, 2.98f, 0), 0.018f, m); Rod("arm2", g, new Vector3(0.34f, 2.98f, 0), new Vector3(0.34f, 2.90f, 0), 0.014f, m); Rod("stay", g, new Vector3(0, 2.6f, 0), new Vector3(0.22f, 2.92f, 0), 0.008f, m);
        Cyl("cap", g, new Vector3(0.34f, 2.89f, 0), 0.11f, 0.03f, m); Ball("globe", g, new Vector3(0.34f, 2.80f, 0), 0.085f, glass).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;      /* the lamp's light sits inside it */
        Ball("finial", g, new Vector3(0, 2.98f, 0), 0.04f, m);
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
        const float OW = 4.00f, OH = 3.0f, FW = 6.0f;                          // the opening is 4 : 3, like the television screen
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
        // the reveals of the opening and the soffit over it are panelled: three sunk panels a side, eight coffers above
        foreach (float sx in new[] { -1f, 1f }) for (int k = 0; k < 3; k++)
        {
            float py = 0.56f + 0.98f * k; Box("reveal_panel", g, new Vector3(sx * (OW * 0.5f + 0.004f), py, -0.13f), new Vector3(0.008f, 0.84f, 0.18f), shade); Box("reveal_field", g, new Vector3(sx * (OW * 0.5f + 0.001f), py, -0.13f), new Vector3(0.006f, 0.70f, 0.12f), ivory);
            Box("reveal_bead", g, new Vector3(sx * (OW * 0.5f - 0.004f), py + 0.46f, -0.13f), new Vector3(0.010f, 0.016f, 0.20f), gold);
        }
        for (int k = 0; k < 8; k++) { float px = -1.75f + 0.5f * k; Box("soffit_panel", g, new Vector3(px, OH + 0.196f, -0.20f), new Vector3(0.42f, 0.008f, 0.26f), shade); Cyl("soffit_rose", g, new Vector3(px, OH + 0.190f, -0.20f), 0.05f, 0.008f, gold); }
        // a scalloped valance under the beam, and bulbs round the opening
        for (int k = 0; k < 21; k++) Cyl("scallop", g, new Vector3(-OW * 0.5f + OW * (k + 0.5f) / 21f, OH + 0.045f, -0.05f), 0.10f, 0.012f, gold, new Vector3(90, 0, 0));
        var bulbM = new Material(Shader.Find("Universal Render Pipeline/Lit")); bulbM.SetColor("_BaseColor", new Color(1f, 0.9f, 0.7f)); bulbM.EnableKeyword("_EMISSION"); bulbM.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; bulbM.SetColor("_EmissionColor", new Color(1f, 0.72f, 0.36f) * 0.35f); bulbM = Save(bulbM, "kit_bulb");
        var pts = new List<Vector3>(); for (int k = 0; k < 15; k++) pts.Add(new Vector3(-OW * 0.5f + 0.14f + (OW - 0.28f) * k / 14f, OH - 0.07f, -0.02f)); for (int k = 0; k < 7; k++) { pts.Add(new Vector3(-OW * 0.5f + 0.05f, 0.35f + 0.38f * k, -0.02f)); pts.Add(new Vector3(OW * 0.5f - 0.05f, 0.35f + 0.38f * k, -0.02f)); }
        foreach (var q in pts) { Cyl("socket", g, q + new Vector3(0, 0, 0.03f), 0.03f, 0.02f, gold, new Vector3(90, 0, 0)); bulbs.Add(Ball("bulb", g, q, 0.036f, bulbM).GetComponent<Renderer>()); }
        // footlights along the front of the stage
        for (int k = 0; k < 7; k++) { float fx = -1.8f + 0.6f * k; Box("foot_hood", g, new Vector3(fx, 0.07f, -0.40f), new Vector3(0.26f, 0.14f, 0.012f), gold, new Vector3(-18, 0, 0)); Ball("foot_bulb", g, new Vector3(fx, 0.06f, -0.34f), 0.03f, bulbM); }
    }

    static void StageHouse(Transform root)
    {
        var g = new GameObject("StageHouse").transform; g.SetParent(root, false);
        var boards = Lit("kit_boards", Tex("Assets/Textures/tex_laminate_floor/laminate_floor_diff_2k.jpg"), Tex("Assets/Textures/tex_laminate_floor/laminate_floor_nor_gl_2k.jpg", true), new Vector2(0.30f, 0.30f), new Color(0.46f, 0.30f, 0.22f), 0.72f);
        var ply = Lit("kit_ply", Tex("Assets/Textures/tex_plywood/plywood_diff_2k.jpg"), Tex("Assets/Textures/tex_plywood/plywood_nor_gl_2k.jpg", true), new Vector2(0.6f, 0.6f), new Color(0.80f, 0.70f, 0.56f), 0.2f);
        Box("floor", g, new Vector3(0, -0.04f, 2.5f), new Vector3(6.0f, 0.08f, 5.4f), boards);
        foreach (float sx in new[] { -2.85f, 2.85f }) foreach (float sz in new[] { 0.35f, 5.05f }) Box("post", g, new Vector3(sx, 2.6f, sz), new Vector3(0.12f, 5.2f, 0.12f), ply);
        foreach (float sz in new[] { 1.0f, 2.6f, 3.4f, 4.3f }) Rod("batten", g, new Vector3(-2.85f, 4.4f, sz), new Vector3(2.85f, 4.4f, sz), 0.03f, ply);
        // the box closes: four panelled walls on their timber frames and a deck of joists over everything
        var card = CardM("house_card", new Color(0.50f, 0.43f, 0.34f), 0.08f, 2); var label = CardM("label", new Color(0.82f, 0.78f, 0.66f)); var lid = CardM("lid", new Color(0.10f, 0.10f, 0.12f));
        PanelWall(WallFrame(g, "WallBack", new Vector3(-3f, 0, 5.2f), 0), 6.0f, 5.2f, null, card, ply, label);
        PanelWall(WallFrame(g, "WallLeft", new Vector3(-3f, 0, 0), -90), 5.2f, 5.2f, null, card, ply, label);
        PanelWall(WallFrame(g, "WallRight", new Vector3(3f, 0, 5.2f), 90), 5.2f, 5.2f, null, card, ply, label);
        PanelWall(WallFrame(g, "WallFront", new Vector3(3f, 0, 0.002f), 180), 6.0f, 5.2f, new List<Rect> { new Rect(1.0f, 0f, 4.0f, 3.0f) }, card, ply, label);
        GridDeck(g, "Grid", -3f, 3f, 0f, 5.2f, 5.2f, ply, lid);
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

