    /// Version 3 of the film: the girl the song is about as a paper doll (on the roof, falling, on the crossing), beams of light in the air
    /// under the stage lanterns and the street lamp, a soft key and bounce for the singer. Safe to run again: it replaces what it made.
    public static string V3Build()
    {
        var stage = GameObject.Find("Stage"); if (stage == null) return "no stage"; var props = stage.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "Props"); if (props == null) return "no props";
        foreach (var n in new[] { "PaperGirl", "GirlBlood" }) { var o = props.Find(n); if (o != null) Object.DestroyImmediate(o.gameObject); }
        var skin = CardM("girl_skin", new Color(0.93f, 0.86f, 0.78f)); var white = CardM("girl_blouse", new Color(0.94f, 0.94f, 0.92f)); var navy = CardM("girl_skirt", new Color(0.10f, 0.12f, 0.20f)); var hair = CardM("girl_hair", new Color(0.09f, 0.07f, 0.06f));
        var blue = CardM("girl_lanyard", new Color(0.16f, 0.30f, 0.62f)); var black = CardM("girl_shoe", new Color(0.04f, 0.04f, 0.05f)); var redM = CardM("girl_red", new Color(0.62f, 0.05f, 0.05f), 0.25f);
        var g = new GameObject("PaperGirl").transform; g.SetParent(props, false); const float T = 0.003f;
        for (int s = -1; s <= 1; s += 2)
        {
            Box("leg", g, new Vector3(0.05f * s, 0.33f, 0), new Vector3(0.046f, 0.60f, T), skin, default, 0f); Box("shoe", g, new Vector3(0.052f * s, 0.022f, -0.001f), new Vector3(0.062f, 0.044f, T), black, default, 0f);
            Box("arm", g, new Vector3(0.150f * s, 1.00f, 0.001f), new Vector3(0.040f, 0.50f, T), skin, new Vector3(0, 0, 7f * s), 0f); Box("sleeve", g, new Vector3(0.135f * s, 1.17f, 0f), new Vector3(0.060f, 0.13f, T), white, new Vector3(0, 0, 7f * s), 0f);
        }
        Box("skirt", g, new Vector3(0, 0.76f, -0.002f), new Vector3(0.27f, 0.34f, T), navy, default, 0f); Box("skirt_hem", g, new Vector3(0, 0.61f, -0.0025f), new Vector3(0.31f, 0.06f, T), navy, default, 0f);
        Box("blouse", g, new Vector3(0, 1.08f, -0.003f), new Vector3(0.215f, 0.33f, T), white, default, 0f); Box("neck", g, new Vector3(0, 1.26f, -0.002f), new Vector3(0.05f, 0.06f, T), skin, default, 0f);
        Cyl("hair_back", g, new Vector3(0, 1.365f, 0.002f), 0.098f, T, hair, new Vector3(90, 0, 0)); Box("hair_bob", g, new Vector3(0, 1.32f, 0.0025f), new Vector3(0.20f, 0.13f, T), hair, default, 0f);
        Cyl("head", g, new Vector3(0, 1.36f, -0.003f), 0.082f, T, skin, new Vector3(90, 0, 0)); Box("fringe", g, new Vector3(0, 1.41f, -0.0055f), new Vector3(0.15f, 0.045f, T), hair, default, 0f);
        Box("lanyard", g, new Vector3(-0.035f, 1.13f, -0.006f), new Vector3(0.008f, 0.22f, 0.001f), blue, new Vector3(0, 0, -17f), 0f); Box("lanyard", g, new Vector3(0.035f, 1.13f, -0.006f), new Vector3(0.008f, 0.22f, 0.001f), blue, new Vector3(0, 0, 17f), 0f);
        Box("badge", g, new Vector3(0, 0.995f, -0.007f), new Vector3(0.055f, 0.075f, 0.001f), white, default, 0f); Box("badge_photo", g, new Vector3(0, 1.005f, -0.0078f), new Vector3(0.028f, 0.034f, 0.0005f), navy, default, 0f);
        foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
        // the red: torn scraps of red paper that come out from under her head, one on each beat
        var bl = new GameObject("GirlBlood").transform; bl.SetParent(props, false);
        float[] sz = { 0.20f, 0.15f, 0.24f, 0.12f, 0.17f }; for (int k = 0; k < sz.Length; k++) Box("scrap" + k, bl, Vector3.zero, new Vector3(sz[k], 0.0012f, sz[k] * (0.62f + 0.1f * k)), redM, new Vector3(0, 23f * k + 11f, 0), 0f);
        g.gameObject.SetActive(false); bl.gameObject.SetActive(false);

        // ---- beams
        var cone = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Gen/beam_cone.asset");
        if (cone == null)
        {
            const int N = 48; var v = new List<Vector3>(); var nr = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int i = 0; i <= N; i++) { float a = i * Mathf.PI * 2f / N; Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0); Vector3 nn = (d - Vector3.forward * 0.6f).normalized; v.Add(Vector3.zero); v.Add(d + Vector3.forward); nr.Add(nn); nr.Add(nn); uv.Add(new Vector2(i / (float)N, 0)); uv.Add(new Vector2(i / (float)N, 1)); }
            for (int i = 0; i < N; i++) { int a = i * 2; tri.Add(a); tri.Add(a + 1); tri.Add(a + 3); tri.Add(a); tri.Add(a + 3); tri.Add(a + 2); }
            cone = new Mesh { name = "beam_cone" }; cone.SetVertices(v); cone.SetNormals(nr); cone.SetUVs(0, uv); cone.SetTriangles(tri, 0); cone.bounds = new Bounds(new Vector3(0, 0, 0.5f), new Vector3(2, 2, 1)); AssetDatabase.CreateAsset(cone, "Assets/Gen/beam_cone.asset");
        }
        var sh = Shader.Find("Stage/Beam"); if (sh == null) return "no beam shader"; int nb = 0;
        foreach (var l in stage.GetComponentsInChildren<Light>(true))
        {
            var old = l.transform.Find("Beam"); if (old != null) Object.DestroyImmediate(old.gameObject);
            float len; if (l.name == "Pool") len = 6.4f; else if (l.name == "FollowSpot") len = 4.6f; else if (l.name == "LampLight") len = 2.7f; else if (l.name == "BackCool") len = 4.2f; else if (l.name == "GhostKey") len = 3.0f; else continue;
            var b = new GameObject("Beam"); b.transform.SetParent(l.transform, false); b.AddComponent<MeshFilter>().sharedMesh = cone; var mr = b.AddComponent<MeshRenderer>(); var m = new Material(sh); m.SetColor("_Color", l.color); m.SetFloat("_Gain", 0f);
            mr.sharedMaterial = Save(m, "beam_" + l.name); mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; float ang = l.name == "LampLight" ? 62f : l.spotAngle; float rad = len * Mathf.Tan(ang * 0.5f * Mathf.Deg2Rad); b.transform.localScale = new Vector3(rad, rad, len); nb++;
        }
        // ---- the singer's own light: a soft warm key from the front and the bounce of the pool off the floor
        var anyL = stage.GetComponentsInChildren<Light>(true).FirstOrDefault(x => x.name == "Pool"); var lg = anyL != null ? anyL.transform.parent : stage.transform;
        foreach (var n in new[] { "LinKey", "PoolBounce" }) { var o = stage.GetComponentsInChildren<Light>(true).FirstOrDefault(x => x.name == n); if (o != null) Object.DestroyImmediate(o.gameObject); }
        var key = new GameObject("LinKey").AddComponent<Light>(); key.transform.SetParent(lg, false); key.type = LightType.Spot; key.color = new Color(1f, 0.90f, 0.78f); key.intensity = 0; key.range = 7f; key.spotAngle = 22f; key.innerSpotAngle = 10f; key.shadows = LightShadows.Soft; key.transform.localPosition = new Vector3(1.9f, 2.6f, -1.9f);
        var bo = new GameObject("PoolBounce").AddComponent<Light>(); bo.transform.SetParent(lg, false); bo.type = LightType.Point; bo.color = new Color(1f, 0.86f, 0.70f); bo.intensity = 0; bo.range = 2.4f; bo.shadows = LightShadows.None;
        EditorSceneManager.MarkSceneDirty(stage.scene); EditorSceneManager.SaveScene(stage.scene); AssetDatabase.SaveAssets();
        return $"paper girl built, {nb} beams, key and bounce lights (lights root {lg.name})";
    }

    /// The generated pictures, each cut to the thing that carries it: six on the speech cards of the paper people (a quad 27 x 17 cm on a card of
    /// 30 x 20), four in the lit windows of the far row (10.5 x 14.5 cm inside panes of 12 x 16; pane 0 of the list is the one window that burns alone).
    public static string V3Art()
    {
        var stage = GameObject.Find("Stage"); if (stage == null) return "no stage"; AssetDatabase.Refresh(); int nc = 0, np = 0;
        foreach (var tr in stage.GetComponentsInChildren<Transform>(true)) if (tr.name == "CardArt" || tr.name == "PaneArt") Object.DestroyImmediate(tr.gameObject);
        Material Art(string file, bool lit)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Gen/art/" + file + ".png"); if (tex == null) return null;
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.05f);
            if (lit) { m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", tex); m.SetColor("_EmissionColor", Color.white); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
            return Save(m, "art_" + file);
        }
        GameObject Q(string name, Transform parent, Vector3 lp, Vector2 size, Material m, bool flip)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(q.GetComponent<Collider>()); q.name = name; q.transform.SetParent(parent, false); q.transform.localPosition = lp; q.transform.localRotation = Quaternion.identity;
            q.transform.localScale = new Vector3(flip ? -size.x : size.x, size.y, 1); var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; return q;
        }
        foreach (var r in stage.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name != "speech_card") continue; var person = r.transform.parent.parent.parent; int i = person.name.StartsWith("Person") ? int.Parse(person.name.Substring(6)) : nc;
            var m = Art("card" + (i % 6), false); if (m == null) continue; var c = r.transform.localPosition; Q("CardArt", r.transform.parent, new Vector3(c.x, c.y, c.z - 0.0032f), new Vector2(0.27f, 0.17f), m, false); nc++;
        }
        var film = Object.FindFirstObjectByType<FilmClock>(); film.Bind(); int solo = film.SoloFar; var panes = film.FarPanes;
        for (int j = 0; panes != null && j < panes.Length; j++)
        {
            int k = j == solo ? 0 : 1 + (j % 3); var m = Art("pane" + k, true); if (m == null) continue;
            var q = Q("PaneArt", panes[j].transform, new Vector3(0, 0, -0.004f), Vector2.one, m, (j / 3) % 2 == 1); var ls = panes[j].transform.lossyScale; q.transform.localScale = new Vector3((q.transform.localScale.x < 0 ? -1 : 1) * 0.105f / Mathf.Max(1e-4f, ls.x), 0.145f / Mathf.Max(1e-4f, ls.y), 1); np++;
        }
        EditorSceneManager.MarkSceneDirty(stage.scene); EditorSceneManager.SaveScene(stage.scene);
        return $"{nc} card pictures, {np} window pictures (solo pane {solo})";
    }
