    // ---------------------------------------------------------------- checks from the cameras the shot list uses
    struct CamDef { public string name, state, shots; public Vector3 p, l; public float f; public CamDef(string n, string st, string sh, float px, float py, float pz, float lx, float ly, float lz, float f) { name = n; state = st; shots = sh; p = new Vector3(px, py, pz); l = new Vector3(lx, ly, lz); this.f = f; } }

    /// The kinds of camera position the shot list (v5) asks for, one entry per kind: where, looking at what, focal length, in which state.
    static readonly CamDef[] STAGE_CAMS =
    {
        new CamDef("front_1500", "night", "25 65 57", 0f, 1.5f, -1.5f, 0f, 1.5f, 3f, 50),
        new CamDef("front_2200", "night", "9 18 35 78", 0f, 1.4f, -2.2f, 0f, 1.6f, 3f, 24),
        new CamDef("front_2800", "night2", "39 45 52 77", 0f, 1.6f, -2.8f, 0f, 1.7f, 3f, 24),
        new CamDef("theatre", "night2", "77", 0f, 2.0f, -4.6f, 0f, 2.0f, 0f, 24),
        new CamDef("tv_camera", "night", "screen", 0f, 1.5f, -4.444f, 0f, 1.5f, 0f, 30),
        new CamDef("front_800", "night", "22", 0.3f, 1.2f, -0.8f, 0.6f, 1.6f, 3f, 35),
        new CamDef("inside_low", "night", "14 30", -1.5f, 1.1f, 0.9f, 1.0f, 1.0f, 2.6f, 35),
        new CamDef("groove_macro", "night", "15", 0.2f, 0.12f, 0.45f, 0.0f, 0.0f, 0.80f, 70),
        new CamDef("lamp_low", "night", "11 12", -0.9f, 0.3f, 1.2f, 0.05f, 1.2f, 2.32f, 50),
        new CamDef("paper_low", "spot", "13 21 33 37", 0.25f, 0.15f, 0.9f, 0.25f, 0.02f, 1.5f, 70),
        new CamDef("paper_top", "spot", "31 38", 0.25f, 2.2f, 1.44f, 0.25f, 0f, 1.5f, 50),
        new CamDef("crowd_back", "night", "30 81", 0.25f, 1.3f, 0.2f, 0.25f, 0.6f, 1.6f, 28),
        new CamDef("lin_left", "night", "26", -2.0f, 1.5f, -0.46f, -0.5f, 1.45f, 1.3f, 70),
        new CamDef("lin_right", "night", "75", 0.9f, 1.5f, -0.3f, -0.5f, 1.45f, 1.3f, 70),
        new CamDef("lin_waist", "night2", "49 69", -0.2f, 1.3f, -1.0f, -0.5f, 1.2f, 1.3f, 50),
        new CamDef("window_2", "night", "27", 0.77f, 1.5f, 1.62f, 1.10f, 1.72f, 3f, 50),
        new CamDef("window_4", "night", "28", 0.88f, 2.15f, 1.42f, 1.10f, 2.34f, 3f, 60),
        new CamDef("window_5", "night", "28.2", 1.82f, 1.98f, 1.44f, 1.94f, 2.29f, 3f, 50),
        new CamDef("street_wide", "night", "29 52", 0.95f, 1.45f, -2.3f, 0.55f, 1.55f, 3f, 24),
        new CamDef("alley_far", "night2", "44 50 51", 0.0f, 1.0f, 2.6f, 0.0f, 0.9f, 4.2f, 50),
        new CamDef("wing_gap", "spot", "34", -0.6f, 1.0f, 1.5f, -2.5f, 0.8f, 1.5f, 50),
        new CamDef("bulbs_up", "night", "74", 0.5f, 0.8f, -1.2f, 0.2f, 3.0f, 0f, 35),
        new CamDef("from_behind", "night", "24", 0.3f, 1.6f, 4.70f, 0f, 1.5f, 0f, 35),
        new CamDef("look_out", "night2", "76", 0.4f, 1.6f, 1.5f, 0.2f, 2.9f, 0f, 35),
        new CamDef("roofs", "night2", "43", 0.3f, 1.4f, -0.2f, 0.6f, 2.6f, 4.2f, 35),
        new CamDef("cloud_close", "cloud", "62 63", -0.3f, 1.75f, 1.35f, -0.6f, 1.85f, 2.63f, 35),
        new CamDef("curtain_lines", "build", "8", -1.2f, 1.6f, -1.0f, -2.1f, 1.6f, 0.12f, 50),
        new CamDef("build_wide", "build", "9 10", 0f, 1.4f, -2.2f, 0f, 1.8f, 3f, 24),
        new CamDef("dusk_low", "dusk", "46.2", -1.2f, 0.25f, 1.0f, 1.2f, 0.4f, 2.9f, 35),
        new CamDef("dusk_wide", "dusk", "45", 0f, 1.4f, -2.2f, 0f, 1.6f, 3f, 24),
        new CamDef("empty_low", "empty", "58", 0.4f, 0.3f, -1.8f, 0f, 2.6f, 5.2f, 24),
        new CamDef("dusk_sun", "dusk", "45", 0.0f, 1.2f, 1.6f, 0.0f, 1.5f, 4.7f, 50),
        new CamDef("empty_wide", "empty", "57 58", 0f, 1.5f, -1.8f, -0.3f, 1.6f, 5.2f, 28),
        new CamDef("empty_bulb", "empty", "59.2", -0.2f, 2.2f, 1.5f, -0.5f, 2.3f, 1.9f, 85),
        new CamDef("empty_paper", "empty", "60.2", 0.25f, 0.3f, 1.05f, 0.25f, 0.02f, 1.5f, 70),
        new CamDef("dark_front", "dark", "83 84 85", 0f, 1.4f, -1.8f, 0f, 1.6f, 3f, 35),
    };
    static readonly CamDef[] ROOM_CAMS =
    {
        new CamDef("wide", "night", "4 47.2 86.2", 0f, 1.25f, -1.085f, 0f, 1.08f, 1.365f, 28),
        new CamDef("tv_push", "night", "8 79", -0.35f, 0.82f, -0.10f, -0.35f, 0.80f, 0.90f, 35),
        new CamDef("tv_glass", "night", "2 86", -0.22f, 0.86f, 0.60f, -0.35f, 0.80f, 0.90f, 70),
        new CamDef("tv_body", "night", "3", -0.16f, 0.72f, 0.56f, -0.30f, 0.66f, 0.90f, 70),
        new CamDef("desk_macro", "night", "5.2 54.2", 0.72f, 1.22f, 0.66f, 0.92f, 0.90f, 1.02f, 70),
        new CamDef("leaf_shadow", "night", "7", -0.3f, 1.2f, -0.2f, -1.1f, 1.1f, 1.365f, 50),
        new CamDef("tv_top", "dawn", "86", 0.1f, 1.15f, 0.35f, -0.35f, 0.98f, 1.05f, 50),
        new CamDef("wide_dawn", "dawn", "86.2", 0f, 1.25f, -1.085f, 0f, 1.08f, 1.365f, 28),
        new CamDef("to_window", "night", "-", 1.2f, 1.4f, 0.2f, -1.82f, 1.4f, 0.0f, 28),
        new CamDef("to_door", "night", "-", 0.4f, 1.4f, 0.9f, -1.0f, 1.1f, -1.365f, 28),
    };

    /// For every camera of the open scene: (1) void with the camera also moved 300 mm each way and the view widened by a quarter;
    /// (2) the frame as the plan lights it; (3) what only the shadow-casting lights reach (ambient and shadowless lights off);
    /// (4) a white model under one head light, for where the geometry has edges. A block of the 16 x 9 grid is dead when it has
    /// neither an edge (4) nor any change of brightness (2).
    public static string CamCheck(string which)
    {
        bool room = which == "room"; var cams = room ? ROOM_CAMS : STAGE_CAMS; const int W = 480, H = 270, BL = 30;
        string dir = "Capture/cams/" + which; if (Directory.Exists(dir)) Directory.Delete(dir, true); Directory.CreateDirectory(dir);
        EnsureDir(GEN); string ap = GEN + "/camcheck.renderTexture"; var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(ap); if (rt == null) { rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 }; AssetDatabase.CreateAsset(rt, ap); }
        var go = new GameObject("CheckCam"); var cam = go.AddComponent<Camera>(); cam.nearClipPlane = 0.02f; cam.farClipPlane = 80; cam.clearFlags = CameraClearFlags.SolidColor; cam.allowHDR = true; cam.enabled = false; cam.aspect = W / (float)H;
        var ad = go.AddComponent<UniversalAdditionalCameraData>(); ad.renderPostProcessing = false; ad.renderShadows = true;
        var hg = new GameObject("CheckHead"); hg.transform.SetParent(go.transform, false); hg.transform.localEulerAngles = new Vector3(24, -32, 0); var head = hg.AddComponent<Light>(); head.type = LightType.Directional; head.intensity = 1.0f; head.shadows = LightShadows.None; head.enabled = false;
        var whiteM = new Material(Shader.Find("Universal Render Pipeline/Lit")); whiteM.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.7f)); whiteM.SetFloat("_Smoothness", 0f);
        Color32[] Grab(Color bg) { cam.backgroundColor = bg; cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null; cam.targetTexture = null; var px = tex.GetPixels32(); Object.DestroyImmediate(tex); return px; }
        void SaveJpg(Color32[] px, string path) { var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.SetPixels32(px); tex.Apply(); File.WriteAllBytes(path, tex.EncodeToJPG(92)); Object.DestroyImmediate(tex); }
        void Aim(CamDef c, Vector3 off, float widen) { go.transform.position = c.p + off; go.transform.rotation = Quaternion.LookRotation(c.l - c.p); cam.fieldOfView = 2f * Mathf.Atan(10.125f / (c.f / widen)) * Mathf.Rad2Deg; }
        var csv = new StringBuilder("camera,state,shots,void_px,ambient_only_pct,dead_pct,bare_pct\n"); var report = new StringBuilder();
        foreach (var st in cams.Select(c => c.state).Distinct().ToList())
        {
            report.AppendLine(room ? RoomState(st) : State(st));
            foreach (var rp in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) rp.RenderProbe();
            var set = cams.Where(c => c.state == st).ToList(); var lit = new Color32[set.Count][]; var voids = new long[set.Count]; var amb = new float[set.Count];
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l != head && l.enabled).ToArray(); Color ambient = RenderSettings.ambientLight;
            for (int i = 0; i < set.Count; i++)
            {
                var c = set[i]; Vector3 fw = (c.l - c.p).normalized, rg = Vector3.Cross(Vector3.up, fw).normalized;
                foreach (var off in new[] { Vector3.zero, rg * 0.3f, -rg * 0.3f, fw * 0.3f, -fw * 0.3f }) { Aim(c, off, 1.25f); foreach (var q in Grab(new Color(1, 0, 1))) if (q.r > 200 && q.b > 200 && q.g < 70) voids[i]++; }
                Aim(c, Vector3.zero, 1f); lit[i] = Grab(Color.black); SaveJpg(lit[i], $"{dir}/{c.name}_lit.jpg");
                RenderSettings.ambientLight = Color.black; foreach (var l in lights) if (l.shadows == LightShadows.None) l.enabled = false;
                var sp = Grab(Color.black); int blk = 0; foreach (var q in sp) if (q.r < 3 && q.g < 3 && q.b < 3) blk++; amb[i] = 100f * blk / sp.Length; SaveJpg(sp, $"{dir}/{c.name}_shadowlights.jpg");
                RenderSettings.ambientLight = ambient; foreach (var l in lights) l.enabled = true;
            }
            // the white model: every renderer gets one plain material, the plan's lights go off, a head light comes on
            var rends = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray(); var keep = rends.Select(r => r.sharedMaterials).ToArray();
            foreach (var r in rends) r.sharedMaterials = Enumerable.Repeat(whiteM, r.sharedMaterials.Length).ToArray();
            foreach (var l in lights) l.enabled = false; head.enabled = true; RenderSettings.ambientLight = new Color(0.28f, 0.28f, 0.28f);
            for (int i = 0; i < set.Count; i++)
            {
                var c = set[i]; Aim(c, Vector3.zero, 1f); var wm = Grab(Color.black); SaveJpg(wm, $"{dir}/{c.name}_white.jpg");
                int dead = 0, bare = 0, blocks = 0;
                for (int by = 0; by < H; by += BL) for (int bx = 0; bx < W; bx += BL)
                {
                    int edges = 0, lo = 255, hi = 0;
                    for (int y = by; y < by + BL - 2; y++) for (int x = bx; x < bx + BL - 2; x++)
                    {
                        int a = wm[y * W + x].g; if (Mathf.Abs(wm[y * W + x + 2].g - a) + Mathf.Abs(wm[(y + 2) * W + x].g - a) > 12) edges++;
                        var q = lit[i][y * W + x]; int lum = (q.r * 2 + q.g * 5 + q.b) / 8; if (lum < lo) lo = lum; if (lum > hi) hi = lum;
                    }
                    blocks++; if (edges < 6) { bare++; if (hi - lo < 5) dead++; }
                }
                csv.Append(c.name).Append(',').Append(c.state).Append(',').Append(c.shots).Append(',').Append(voids[i]).Append(',').Append(amb[i].ToString("F1")).Append(',').Append((100f * dead / blocks).ToString("F1")).Append(',').Append((100f * bare / blocks).ToString("F1")).Append('\n');
            }
            for (int k = 0; k < rends.Length; k++) rends[k].sharedMaterials = keep[k];
            foreach (var l in lights) l.enabled = true; head.enabled = false; RenderSettings.ambientLight = ambient;
        }
        Object.DestroyImmediate(go); Object.DestroyImmediate(whiteM);
        File.WriteAllText(dir + "/cams.csv", csv.ToString());
        report.AppendLine(room ? RoomState("night") : State("night"));
        return report.ToString() + csv.ToString();
    }

    // ---------------------------------------------------------------- automatic cross-checks of the plan's geometry
    /// (1) groups of scenery whose bounding boxes run into each other; (2) lights that sit inside a part; (3) for every spot light,
    /// the first part its axis meets and how far away; (4) cameras with something closer than 100 mm.
    public static string Audit(string which)
    {
        bool room = which == "room"; var sb = new StringBuilder(); var root = GameObject.Find(room ? "Room" : "Stage"); if (root == null) return "no scene";
        var groups = new List<(string, Bounds)>();
        string[] skip = { "StageHouse", "Street", "Lights", "Cyc", "Tube", "Proscenium", "Shell", "Outside", "Furniture" };
        foreach (Transform t in root.transform) { var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0 || skip.Contains(t.name)) continue; var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); groups.Add((t.name, b)); }
        if (room) { var fu = root.transform.Find("Furniture"); if (fu != null) foreach (Transform t in fu) { var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue; var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); groups.Add((t.name, b)); } }
        sb.AppendLine("== groups whose boxes overlap (name, name, overlap in mm)");
        for (int i = 0; i < groups.Count; i++) for (int j = i + 1; j < groups.Count; j++)
        {
            var a = groups[i].Item2; var b = groups[j].Item2; Vector3 lo = Vector3.Max(a.min, b.min), hi = Vector3.Min(a.max, b.max); Vector3 d = hi - lo;
            if (d.x > 0.005f && d.y > 0.005f && d.z > 0.005f) sb.AppendLine($"  {groups[i].Item1} x {groups[j].Item1}: {d.x * 1000:F0} x {d.y * 1000:F0} x {d.z * 1000:F0}");
        }
        float lim = room ? 1.9f : 3.02f; sb.AppendLine("== groups reaching outside the side walls");
        foreach (var (n, b) in groups) if (!room && (b.min.x < -lim || b.max.x > lim) || room && n != "Outside" && (b.max.x > lim || b.min.z < -1.42f || b.max.z > 1.42f)) sb.AppendLine($"  {n}: x {b.min.x * 1000:F0} .. {b.max.x * 1000:F0}, z {b.min.z * 1000:F0} .. {b.max.z * 1000:F0}");
        var parts = root.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled && !(r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Surface") && r.sharedMaterial.GetFloat("_Surface") > 0.5f)).Select(r => (r, r.bounds)).ToArray();
        sb.AppendLine("== lights");
        foreach (var l in root.GetComponentsInChildren<Light>(true))
        {
            if (l.name.StartsWith("PaneLight")) continue; Vector3 p = l.transform.position; string inside = "";
            foreach (var (r, b) in parts) if (b.size.x < 1.5f && b.size.y < 1.5f && b.size.z < 1.5f && b.Contains(p)) { inside = r.name + " (" + r.transform.parent.name + ")"; break; }
            string line = $"  {l.name,-14} {(l.enabled ? "on " : "off")} {l.type,-5} shadows {(l.shadows != LightShadows.None ? "yes" : "no "),-3} at {p * 1000f:F0}";
            if (inside != "") line += "  INSIDE " + inside;
            if (l.type == LightType.Spot)
            {
                var ray = new Ray(p, l.transform.forward); float best = 1e9f; string hit = "nothing";
                foreach (var (r, b) in parts) if (b.IntersectRay(ray, out float d) && d > 0.05f && d < best) { best = d; hit = r.name + " (" + r.transform.parent.name + ")"; }
                line += $"  axis meets {hit} after {best * 1000:F0}";
            }
            sb.AppendLine(line);
        }
        sb.AppendLine("== cameras with a part within 100 mm");
        foreach (var c in room ? ROOM_CAMS : STAGE_CAMS) foreach (var (r, b) in parts) if (b.size.magnitude < 3f && b.SqrDistance(c.p) < 0.01f) { sb.AppendLine($"  {c.name}: {r.name} ({r.transform.parent.name})"); break; }
        sb.AppendLine("== sheets without thickness (quads)"); var quads = root.GetComponentsInChildren<MeshFilter>().Where(m => m.sharedMesh != null && m.sharedMesh.name == "Quad").GroupBy(m => System.Text.RegularExpressions.Regex.Replace(m.name, @"\d+$", "")).Select(g2 => g2.Key + " x" + g2.Count());
        sb.AppendLine("  " + string.Join(", ", quads));
        sb.AppendLine("== textures used"); var texs = new SortedSet<string>();
        foreach (var r in root.GetComponentsInChildren<Renderer>()) foreach (var m in r.sharedMaterials) if (m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) texs.Add(AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")));
        foreach (var t in texs) sb.AppendLine("  " + t);
        File.WriteAllText("Capture/audit_" + which + ".txt", sb.ToString()); return sb.ToString();
    }
