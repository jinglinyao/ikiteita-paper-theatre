    // ---------------------------------------------------------------- the plan's lights, and the states of the stage
    static Light Spot(string name, Transform root, Vector3 pos, Vector3 look, Color col, float intensity, float range, float angle, bool shadows, bool on = true)
    {
        var l = AddLight(name, root, LightType.Spot, pos, look, col, intensity, range, angle, shadows); l.enabled = on; return l;
    }

    // base intensities of the lights a state may dim
    const float I_MOON = 42f, I_POOL = 30f, I_LAMP = 14f, I_FOOT = 9f, I_SIDE = 7f, I_HOUSE = 22f;

    /// Every light of section 5 of the plan, by name. Nothing else lights the stage but the flat blue ambient.
    static void StageLights(Transform root)
    {
        var g = new GameObject("Lights").transform; g.SetParent(root, false);
        Color warm = new Color(1f, 0.74f, 0.42f), cool = new Color(0.50f, 0.66f, 1f), night = new Color(0.44f, 0.52f, 0.88f), white = new Color(1f, 0.93f, 0.82f);
        Spot("FollowSpot", g, new Vector3(-2.45f, 1.40f, -1.60f), MARK + Vector3.up * 1.38f, white, SPOT, 9f, 15f, true);                     // level, through the proscenium: her face and shoulders
        Spot("Pool", g, new Vector3(-1.60f, 6.2f, 0.95f), new Vector3(-0.30f, 0f, 1.40f), white, I_POOL, 9f, 18f, true);                          // straight down between the first two borders: the circle on the floor
        Spot("WindowKick", g, new Vector3(1.5f, 1.9f, 2.55f), MARK + Vector3.up * 1.3f, warm, 3.5f, 6f, 60f, false);                                         // what the lit windows throw on her: a warm edge on the side toward the house
        Spot("BackCool", g, new Vector3(0.2f, 3.75f, 3.6f), MARK + Vector3.up * 1.35f, cool, RIM, 8f, 26f, false);
        Spot("SceneryWash", g, new Vector3(-0.6f, 3.40f, 0.2f), new Vector3(0.2f, 1.2f, 3.4f), night, WASH, 10f, 96f, true);                   // under the first border
        Spot("Moon", g, new Vector3(-1.4f, 6.6f, 2.3f), new Vector3(1.0f, 0f, 3.4f), new Color(0.62f, 0.74f, 1f), I_MOON, 14f, 50f, true);     // behind and above the last border
        Spot("LampLight", g, new Vector3(0.39f, 2.74f, 2.32f), new Vector3(0.39f, 0f, 2.32f), warm, I_LAMP, 5f, 150f, true);
        Spot("GhostLight", g, new Vector3(2.4f, 1.52f, 5.0f), new Vector3(1.2f, 1.4f, 5.2f), warm, 0.9f, 6f, 150f, false);
        Spot("GhostKey", g, new Vector3(2.4f, 1.545f, 4.93f), new Vector3(-0.5f, 1.0f, 1.0f), warm, 40f, 9f, 165f, true, false);      // the same lamp as the one light of the struck stage (the clock switches it)
        Spot("LoftWork", g, new Vector3(0f, 6.3f, 2.6f), new Vector3(0f, 8.2f, 2.6f), new Color(0.46f, 0.58f, 0.95f), 3.0f, 6f, 160f, true);
        Spot("BackWork", g, new Vector3(0f, 3.2f, 4.75f), new Vector3(0f, 0.8f, 3.9f), new Color(0.46f, 0.58f, 0.95f), 2.5f, 5f, 150f, true);                     // from above the back passage down the backs of the flats
        Spot("SideCoolR", g, new Vector3(2.75f, 1.4f, 0.85f), new Vector3(-2.4f, 0.5f, 1.6f), cool, I_SIDE, 8f, 70f, true);                                  // out of the gap between two wings, across the stage
        Spot("SideCoolL", g, new Vector3(-2.75f, 1.4f, 0.85f), new Vector3(2.4f, 0.5f, 1.6f), cool, I_SIDE, 8f, 70f, true);
        Spot("HouseLight", g, new Vector3(0.3f, 3.8f, -4.6f), new Vector3(0f, 2.2f, -0.3f), new Color(1f, 0.82f, 0.60f), I_HOUSE, 9f, 100f, true);            // in the tube: gives the front of the proscenium its relief
        for (int k = 0; k < 3; k++) Spot("Foot" + k, g, new Vector3(-1.2f + 1.2f * k, 0.10f, -0.30f), new Vector3(-1.2f + 1.2f * k, 1.7f, 3.0f), warm, I_FOOT, 9f, 84f, true, false);
        Spot("ProsRim0", g, new Vector3(-1.85f, 2.85f, 0.30f), new Vector3(1.2f, 1.2f, 0.12f), warm, 2.2f, 5f, 110f, true); Spot("ProsRim1", g, new Vector3(1.85f, 2.85f, 0.30f), new Vector3(-1.2f, 1.2f, 0.12f), warm, 2.2f, 5f, 110f, true);     // graze the inside of the proscenium wall, the curtain, the first border
        Spot("LastBulb", g, new Vector3(0.9f, 2.92f, 0.06f), new Vector3(-0.3f, 0f, 1.5f), warm, 8f, 7f, 120f, true, false);                                   // the one bulb of the proscenium still burning at the end
        var bulb = AddLight("BareBulb", g, LightType.Point, new Vector3(-0.5f, 2.3f, 1.9f), Vector3.zero, new Color(1f, 0.86f, 0.66f), 7f, 7f, 0, true); bulb.enabled = false;
        Spot("SunLight", g, new Vector3(2.75f, 0.45f, 2.05f), new Vector3(-1.5f, 0.3f, 2.6f), new Color(1f, 0.56f, 0.24f), 26f, 10f, 80f, true, false);   // low, out of the gap between two wings on the right
    }

    static readonly string[] HOUSES = { "HouseR", "HouseL" };
    public static readonly string[] STATES = { "build", "night", "night2", "spot", "dusk", "empty", "cloud", "dark" };

    /// Put the stage into one of the plan's states (pieces flown or struck, lights on or off, the sky's colour). Positions only: the
    /// moves between states belong to the film.
    public static string State(string name)
    {
        var stage = GameObject.Find("Stage"); if (stage == null) return "no stage";
        Transform T(string n) { foreach (var t in stage.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
        var lights = new Dictionary<string, Light>(); foreach (var l in stage.GetComponentsInChildren<Light>(true)) if (l.name != "LampGlow") lights[l.name] = l;
        void On(params string[] names) { foreach (var kv in lights) if (!kv.Key.StartsWith("PaneLight")) kv.Value.enabled = names.Contains(kv.Key) || names.Any(n => n.EndsWith("*") && kv.Key.StartsWith(n.TrimEnd('*'))); }
        bool empty = name == "empty", build = name == "build";
        const float FLY = 4.2f;                                                                                      // a piece 3.6 high under a batten at 7.9: its foot at 4.2 or 4.3, above the borders' lower edge
        foreach (var n in HOUSES) { var t = T(n); if (t == null) continue; var p = t.localPosition; p.y = empty ? FLY : build && n == "HouseR" ? 1.9f : 0.04f; t.localPosition = p; }
        var far = T("FarRow"); if (far != null) { var p = far.localPosition; p.y = empty ? 6.4f : 0f; far.localPosition = p; }                 // only 1.24 high: it goes right up, out of the low sight lines under the last border
        var bulbProp = T("BareBulbProp"); if (bulbProp != null) bulbProp.gameObject.SetActive(empty);
        var cyc = T("Cyc"); if (cyc != null) foreach (var r in cyc.GetComponentsInChildren<MeshRenderer>(true)) if (r.name.StartsWith("Sky")) r.enabled = !empty;                 // rolled up on its roller
        foreach (var n in new[] { "StreetProps", "Street" }) { var t = T(n); if (t != null) t.gameObject.SetActive(!empty); }                                                   // the street is a floor cloth: struck as one
        var lamp = T("LampPost"); if (lamp != null) { bool down = build; lamp.gameObject.SetActive(!empty); lamp.localEulerAngles = new Vector3(0, 0, down ? 90 : 0); var p = lamp.localPosition; p.y = down ? 0.12f : 0f; lamp.localPosition = p; }
        var clouds = T("Clouds"); if (clouds != null) clouds.localPosition = new Vector3(0, name == "cloud" ? -1.4f : empty ? 3.6f : 0f, 0);
        var sun = T("PaperSun"); if (sun != null) { var p = sun.localPosition; p.y = name == "dusk" ? 1.55f : 0.30f; sun.localPosition = p; /* by night it has set: down behind the far row */ sun.gameObject.SetActive(!empty); }
        var rod = T("SunRod"); if (rod != null) rod.gameObject.SetActive(!empty);
        if (cyc != null) foreach (var r in cyc.GetComponentsInChildren<MeshRenderer>(true)) if (r.name.StartsWith("Sky")) { var m = AssetDatabase.LoadAssetAtPath<Material>(GEN + "/" + (name == "dusk" ? "stage_cyc_dusk" : "stage_cyc" + r.name.Substring(3)) + ".mat"); if (m != null) r.sharedMaterial = m; r.SetPropertyBlock(null); }
        float wash = 1f, side = 1f;
        switch (name)
        {
            case "spot": On("Pool", "FollowSpot", "BackCool", "SceneryWash", "SideCool*", "GhostLight", "LoftWork"); wash = 0.15f; side = 0.5f; break;
            case "dusk": On("SceneryWash", "SunLight", "GhostLight", "LoftWork", "BackWork", "ProsRim*", "HouseLight"); wash = 0.30f; break;
            case "empty": On("Foot1", "BareBulb", "FollowSpot", "BackCool", "GhostLight", "LoftWork", "SideCool*"); side = 0.5f; break;
            case "dark": On("LastBulb", "SceneryWash", "GhostLight"); wash = 0.15f; break;
            case "build": On("SceneryWash", "Moon", "GhostLight", "LoftWork", "BackWork", "ProsRim*", "SideCool*", "HouseLight"); break;
            default: On("Pool", "FollowSpot", "WindowKick", "BackCool", "SceneryWash", "Moon", "LampLight", "GhostLight", "LoftWork", "BackWork", "ProsRim*", "SideCool*", "HouseLight"); break;
        }
        if (lights.ContainsKey("SceneryWash")) lights["SceneryWash"].intensity = WASH * wash;
        foreach (var n in new[] { "SideCoolR", "SideCoolL" }) if (lights.ContainsKey(n)) lights[n].intensity = I_SIDE * side;
        if (lights.ContainsKey("Foot1")) lights["Foot1"].intensity = empty ? 14f : I_FOOT;
        var ck = Object.FindFirstObjectByType<StageClock>();
        bool windows = name == "night" || name == "night2" || name == "cloud";
        if (ck != null) { if (ck.singer != null) ck.singer.Apply(63f); ck.Apply(windows ? 63f : 10f, 26); }
        bool farLit = name == "night2" || name == "cloud";
        foreach (var r in stage.GetComponentsInChildren<MeshRenderer>(true)) if (r.name == "FarPane") { var mpb = new MaterialPropertyBlock(); mpb.SetColor("_EmissionColor", farLit ? new Color(1f, 0.56f, 0.26f) * 1.2f : Color.black); mpb.SetColor("_BaseColor", new Color(0.10f, 0.11f, 0.15f)); r.SetPropertyBlock(mpb); }
        var globe = T("globe"); if (globe != null) { var mpb = new MaterialPropertyBlock(); mpb.SetColor("_EmissionColor", lights.ContainsKey("LampLight") && lights["LampLight"].enabled ? new Color(1f, 0.70f, 0.36f) * 1.5f : Color.black); globe.GetComponent<Renderer>().SetPropertyBlock(mpb); }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return "state " + name + ": lights on = " + string.Join(", ", lights.Where(kv => kv.Value.enabled && !kv.Key.StartsWith("PaneLight")).Select(kv => kv.Key));
    }
