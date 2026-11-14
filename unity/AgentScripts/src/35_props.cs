    // ---------------------------------------------------------------- the paper properties of the film (the film clock moves them)
    /// a triangular piece of card of the given thickness (both faces and the three cut edges)
    static GameObject Tri3(string name, Transform parent, Vector3 a, Vector3 b, Vector3 c, float thick, Material mat)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false);
        Vector3 n = Vector3.Cross(b - a, c - a).normalized * thick * 0.5f; Vector3[] top = { a + n, b + n, c + n }, bot = { a - n, b - n, c - n };
        var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
        void Face(params Vector3[] q) { int i = v.Count; v.AddRange(q); foreach (var p in q) uv.Add(new Vector2(p.x + p.z, p.y + p.z) / 0.3f); for (int k = 1; k + 1 < q.Length; k++) { t.Add(i); t.Add(i + k); t.Add(i + k + 1); } }
        Face(top[0], top[1], top[2]); Face(bot[0], bot[2], bot[1]);
        for (int k = 0; k < 3; k++) { int j = (k + 1) % 3; Face(top[k], bot[k], bot[j], top[j]); }
        var mesh = new Mesh { name = name }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.TwoSided; return g;
    }
    static void Quad3(string name, Transform parent, Vector3 a, Vector3 b, Vector3 c, Vector3 d, float thick, Material mat) { Tri3(name, parent, a, b, c, thick, mat); Tri3(name, parent, a, c, d, thick, mat); }

    /// The origami bird, about 210 long and 240 across the wings: a keel of doubled paper (beak, breast, tail), two wings hinged along
    /// the back so that they can beat. It looks along +z. Children "WingL" and "WingR" turn about their own z axis.
    static Transform Bird(Transform parent, string name, Material red, Material redDark)
    {
        var g = new GameObject(name).transform; g.SetParent(parent, false);
        Vector3 beak = new Vector3(0, 0.006f, 0.108f), head = new Vector3(0, 0.022f, 0.082f), neck = new Vector3(0, 0.020f, 0.050f), back = new Vector3(0, 0.014f, -0.020f), tail = new Vector3(0, 0.046f, -0.112f), belly = new Vector3(0, -0.020f, -0.030f), breast = new Vector3(0, -0.026f, 0.030f), throat = new Vector3(0, 0.002f, 0.070f);
        foreach (float s in new[] { -1f, 1f })                                                                                // the keel is two leaves of paper 3 mm apart at the breast, meeting along the back
        {
            Vector3 o = new Vector3(s * 0.004f, 0, 0), ob = new Vector3(s * 0.0012f, 0, 0);
            Tri3("keel", g, beak + ob, head + ob, throat + o, 0.0012f, red); Quad3("keel", g, head + ob, neck + ob, breast + o, throat + o, 0.0012f, red);
            Quad3("keel", g, neck + ob, back + ob, belly + o, breast + o, 0.0012f, redDark); Tri3("keel", g, back + ob, tail + ob, belly + o, 0.0012f, red);
        }
        foreach (float s in new[] { -1f, 1f })
        {
            var w = new GameObject(s < 0 ? "WingL" : "WingR").transform; w.SetParent(g, false); w.localPosition = new Vector3(s * 0.002f, 0.016f, 0.01f);
            Vector3 a = new Vector3(0, 0, 0.040f), b = new Vector3(0, 0, -0.050f), tip = new Vector3(s * 0.120f, 0.012f, -0.030f), mid = new Vector3(s * 0.070f, 0.004f, 0.030f);
            Tri3("wing", w, a, mid, b, 0.0012f, red); Tri3("wing", w, mid, tip, b, 0.0012f, redDark);                          // one crease along the wing
        }
        return g;
    }

    public static readonly Vector3 PAPER_AT = new Vector3(0.62f, 0.0086f, 1.52f);

    static void Props(Transform root)
    {
        var g = new GameObject("Props").transform; g.SetParent(root, false);
        var red = CardM("red_paper", new Color(0.74f, 0.09f, 0.08f), 0.12f, 1); var redD = CardM("red_paper_dark", new Color(0.56f, 0.06f, 0.06f), 0.12f, 1); var grey = CardM("grey_paper", new Color(0.62f, 0.62f, 0.60f), 0.10f, 1);
        var yellow = CardM("tape_yellow", new Color(0.86f, 0.70f, 0.16f), 0.14f); var black = CardM("hand_black", new Color(0.035f, 0.035f, 0.04f), 0.10f, 1); var white = CardM("snow_white", new Color(0.86f, 0.86f, 0.84f), 0.08f); var rainM = CardM("rain_blue", new Color(0.56f, 0.66f, 0.80f), 0.12f); var str = CardM("string", new Color(0.75f, 0.72f, 0.64f));
        // ---- the red paper: 210 x 148, one corner that can be lifted (child "Corner", hinged along its diagonal), two old creases
        foreach (var (nm, m) in new[] { ("RedPaper", red), ("GreyPaper", grey) })
        {
            var p = new GameObject(nm).transform; p.SetParent(g, false); p.localPosition = PAPER_AT; p.localEulerAngles = new Vector3(0, 14, 0);
            const float W = 0.105f, D = 0.074f, C = 0.045f, T = 0.0006f;
            Quad3("sheet", p, new Vector3(-W, 0, -D), new Vector3(-W, 0, D), new Vector3(W - C, 0, D), new Vector3(W, 0, D - C), T, m); Tri3("sheet", p, new Vector3(-W, 0, -D), new Vector3(W, 0, D - C), new Vector3(W, 0, -D), T, m);
            var cn = new GameObject("Corner").transform; cn.SetParent(p, false); cn.localPosition = new Vector3(W - C * 0.5f, 0, D - C * 0.5f); cn.localEulerAngles = new Vector3(0, -45, 0);
            float h = C * 0.7071f; Tri3("corner", cn, new Vector3(-h, 0, 0), new Vector3(0, 0, h), new Vector3(h, 0, 0), T, m);
            if (nm == "RedPaper") { Box("crease", p, new Vector3(0, 0.0005f, 0), new Vector3(0.0012f, 0.0004f, D * 2f), redD, default, 0f); Box("crease", p, new Vector3(0, 0.0005f, 0), new Vector3(W * 2f, 0.0004f, 0.0012f), redD, default, 0f); }
            p.gameObject.SetActive(false);
        }
        // ---- the folding of the bird, one fold a beat (stop motion: six still shapes of the same sheet, then the bird)
        var fold = new GameObject("Folds").transform; fold.SetParent(g, false); fold.localPosition = PAPER_AT;
        Transform F(int k) { var f = new GameObject("Fold" + k).transform; f.SetParent(fold, false); f.gameObject.SetActive(false); return f; }
        const float A = 0.105f, Th = 0.0008f;
        var f0 = F(0); Quad3("s", f0, new Vector3(-A, 0, -A), new Vector3(-A, 0, A), new Vector3(A, 0, A), new Vector3(A, 0, -A), Th, red);                                     // the sheet, trimmed square, lying flat
        var f1 = F(1); Tri3("s", f1, new Vector3(-A, 0, -A), new Vector3(-A, 0, A), new Vector3(A, 0, -A), Th, red); Tri3("s", f1, new Vector3(-A, 0.002f, -A), new Vector3(-0.02f, 0.05f, 0.02f), new Vector3(A, 0.002f, -A), Th, redD);   // folded corner to corner, the top leaf not yet flat
        var f2 = F(2); Tri3("s", f2, new Vector3(-A, 0, -A), new Vector3(-A, 0, A), new Vector3(A, 0, -A), Th * 2, red); Tri3("s", f2, new Vector3(-A, 0.002f, A), new Vector3(-A * 0.2f, 0.05f, 0), new Vector3(-A, 0.002f, -A), Th, redD);  // a second fold coming over
        var f3 = F(3); Tri3("s", f3, new Vector3(0, 0.022f, -A), new Vector3(-A * 0.55f, 0, 0), new Vector3(0, 0.022f, A), Th * 2, red); Tri3("s", f3, new Vector3(0, 0.022f, -A), new Vector3(0, 0.022f, A), new Vector3(A * 0.55f, 0, 0), Th * 2, redD);   // a kite, its spine already lifting
        var f4 = F(4); foreach (float s in new[] { -1f, 1f }) Tri3("s", f4, new Vector3(0, 0, -A), new Vector3(0, 0, A), new Vector3(s * A * 0.42f, 0.055f, 0), Th * 2, s < 0 ? red : redD);   // the kite folded along its spine: it stands on its keel
        var f5 = F(5); foreach (float s in new[] { -1f, 1f }) { Tri3("s", f5, new Vector3(0, 0, -A * 0.9f), new Vector3(0, 0, A * 0.6f), new Vector3(s * 0.012f, 0.05f, 0), Th * 2, red); Tri3("s", f5, new Vector3(0, 0.05f, 0.03f), new Vector3(0, 0.05f, -0.04f), new Vector3(s * 0.10f, 0.085f, -0.02f), Th, redD); }
        Tri3("s", f5, new Vector3(0, 0, A * 0.6f), new Vector3(0, 0.07f, A * 0.75f), new Vector3(0, 0.03f, A * 0.45f), Th * 2, red);                                               // wings out, the neck turned up
        var bRed = new Material(red); bRed.EnableKeyword("_EMISSION"); bRed.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; bRed.SetColor("_EmissionColor", new Color(0.002f, 0f, 0f)); bRed = Save(bRed, "bird_red"); var bRedD = new Material(redD); bRedD.EnableKeyword("_EMISSION"); bRedD.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; bRedD.SetColor("_EmissionColor", new Color(0.002f, 0f, 0f)); bRedD = Save(bRedD, "bird_red_dark");      /* thin paper lets the light through: the clock gives the bird a glow of its own in the dark */
        var bird = Bird(g, "Bird", bRed, bRedD); bird.localPosition = PAPER_AT + new Vector3(0, 0.03f, 0); bird.gameObject.SetActive(false);
        // ---- yellow paper tape: one across the street in front of the crowd, two crossed in the proscenium
        Transform Tape(string nm, float len) { var t = new GameObject(nm).transform; t.SetParent(g, false); Box("tape", t, Vector3.zero, new Vector3(len, 0.11f, 0.002f), yellow, default, 0f); if (nm == "Tape0") { Rod("tape_line", t, new Vector3(len * 0.5f, 0, 0), new Vector3(len * 0.5f + 7.5f, 0, 0), 0.0015f, str); Box("tape_tab", t, new Vector3(len * 0.5f - 0.01f, 0, -0.0015f), new Vector3(0.03f, 0.12f, 0.003f), black, default, 0f); }      /* the line it is drawn across by */ for (float x = -len * 0.5f + 0.2f; x < len * 0.5f; x += 0.42f) Box("tape_mark", t, new Vector3(x, 0, -0.0012f), new Vector3(0.10f, 0.03f, 0.001f), black, new Vector3(0, 0, 38), 0f); t.gameObject.SetActive(false); return t; }
        Tape("Tape0", 6.6f); Tape("Tape1", 5.4f); Tape("Tape2", 5.4f);
        // ---- the stagehand's hand: black card, seen flat from the front. The arm comes in level from the left wing (it is long enough to
        // reach from beyond the wing to the paper), the hand hangs from the wrist with the finger tips down, the thumb and the first finger
        // closed on each other: it pinches. Pivot at the pinch.
        var hand = new GameObject("StageHand").transform; hand.SetParent(g, false); var cuffM = CardM("hand_cuff", new Color(0.16f, 0.16f, 0.18f), 0.1f, 1);
        Box("arm", hand, new Vector3(-2.26f, 0.262f, 0.001f), new Vector3(4.20f, 0.085f, 0.004f), black, default, 0f); Box("cuff", hand, new Vector3(-0.150f, 0.262f, -0.003f), new Vector3(0.03f, 0.105f, 0.004f), cuffM, default, 0f);
        Box("wrist", hand, new Vector3(-0.085f, 0.235f, 0), new Vector3(0.11f, 0.10f, 0.004f), black, new Vector3(0, 0, -28f), 0f); Box("palm", hand, new Vector3(-0.030f, 0.158f, 0), new Vector3(0.115f, 0.11f, 0.004f), black, new Vector3(0, 0, -8f), 0f);
        var idx = new GameObject("HandIndex").transform; idx.SetParent(hand, false); idx.localPosition = new Vector3(0.012f, 0.105f, -0.001f); idx.localEulerAngles = new Vector3(0, 0, 6f); Box("finger", idx, new Vector3(0, -0.054f, 0), new Vector3(0.024f, 0.108f, 0.004f), black, default, 0f);                        // the first finger, down to the pinch
        var thb = new GameObject("HandThumb").transform; thb.SetParent(hand, false); thb.localPosition = new Vector3(-0.057f, 0.072f, -0.002f); thb.localEulerAngles = new Vector3(0, 0, -40f); Box("thumb", thb, new Vector3(0, -0.042f, 0), new Vector3(0.026f, 0.085f, 0.004f), black, default, 0f);      /* the clock closes thumb and finger on the paper */                     // the thumb, meeting it
        for (int k = 0; k < 3; k++) Box("finger", hand, new Vector3(0.034f + 0.026f * k, 0.078f + 0.012f * k, 0.001f), new Vector3(0.023f, 0.070f - 0.010f * k, 0.004f), black, new Vector3(0, 0, 12f + 6f * k), 0f);   // the other three, half curled
        hand.gameObject.SetActive(false);
        // ---- paper rain: twelve lines from the second batten, each with nine drops of card
        var rain = new GameObject("Rain").transform; rain.SetParent(g, false);
        for (int i = 0; i < 12; i++)
        {
            float x = -1.9f + 3.8f * i / 11f + 0.08f * Mathf.Sin(i * 2.3f), z = 1.80f + 0.10f * Mathf.Sin(i * 1.7f);
            Rod("rain_line", rain, new Vector3(x, 0.55f, z), new Vector3(x, BATTEN, z), 0.0015f, str);
            for (int k = 0; k < 9; k++) { float y = 0.55f + 0.30f * k + 0.11f * Mathf.Sin(i * 3.1f + k); var d = new GameObject("drop").transform; d.SetParent(rain, false); d.localPosition = new Vector3(x, y, z); d.localEulerAngles = new Vector3(0, 30f * Mathf.Sin(i + k * 1.3f), 0); Quad3("drop", d, new Vector3(0, 0.05f, 0), new Vector3(-0.018f, -0.012f, 0), new Vector3(0, -0.032f, 0), new Vector3(0.018f, -0.012f, 0), 0.0015f, rainM); }
        }
        rain.gameObject.SetActive(false);
        // ---- paper snow in front of the closed curtain: ninety scraps the clock lets fall
        var snow = new GameObject("Snow").transform; snow.SetParent(g, false);
        for (int i = 0; i < 90; i++) { float s = 0.018f + 0.016f * ((i * 37) % 10) / 10f; Box("snow_" + i, snow, Vector3.zero, new Vector3(s, s * 0.8f, 0.0008f), white, default, 0f); }
        snow.gameObject.SetActive(false);
    }
