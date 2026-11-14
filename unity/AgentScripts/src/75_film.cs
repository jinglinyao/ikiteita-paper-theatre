    // ---------------------------------------------------------------- the film clock (stage scene)
    /// put the stage at film time t (scenery, lights, windows, bulbs, curtain, sky, singer); returns the state it is in
    public static string Film(float t)
    {
        var film = Object.FindFirstObjectByType<FilmClock>(); if (film == null) return "no film clock";
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck != null && ck.singer != null) ck.singer.Apply(t);
        State("night"); film.Bind(); film.Apply(t); string from; float mix; string to = film.StateAt(t, out from, out mix);
        return $"t {t:F2}: state {from} -> {to} ({mix:F2}), singer {(film.SingerOn(t) ? "on" : "off")}";
    }

    public static string FilmDebug(float t)
    {
        string r = Film(t) + " | "; var stage = GameObject.Find("Stage");
        foreach (var l in stage.GetComponentsInChildren<Light>(true)) if (l.enabled && l.gameObject.activeInHierarchy && l.intensity > 0.01f && !l.name.StartsWith("PaneLight")) r += $"  {l.name} {l.type} I {l.intensity:F2} pos {l.transform.position} fwd {l.transform.forward} angle {l.spotAngle:F0} range {l.range:F1} shadows {l.shadows} | ";
        var ck = Object.FindFirstObjectByType<StageClock>();
        if (ck != null && ck.singer != null) foreach (var rr in ck.singer.GetComponentsInChildren<Renderer>(true)) foreach (var m in rr.sharedMaterials) if (m != null) r += $"  lin {rr.name} / {m.name} / {m.shader.name} emis {(m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor").ToString() : "-")} kw {m.IsKeywordEnabled("_EMISSION")} | ";
        return r;
    }
    /// a still at film time t from the television camera (4 : 3) or from any camera position (16 : 9)
    public static string FilmTv(float t, string name)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null) return "no stage"; string s = Film(t);
        foreach (var rp in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) rp.RenderProbe();
        var cam = ck.cam; var keepS = cam.sensorSize; cam.sensorSize = new Vector2(36f, 27f); cam.transform.position = new Vector3(0, 1.5f, -4.444f); cam.transform.rotation = Quaternion.identity; cam.focalLength = 40f;
        DepthOfField dof; if (ck.volume.profile.TryGet(out dof)) { dof.focusDistance.value = 7.4f; dof.aperture.value = 8f; dof.focalLength.value = 40f; }
        RenderTo(cam, 960, 720, "Capture/" + name + ".jpg"); cam.sensorSize = keepS; return s + " -> Capture/" + name + ".jpg";
    }
    public static string FilmShot(float t, float px, float py, float pz, float lx, float ly, float lz, float focal, float fstop, string name)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null) return "no stage"; string s = Film(t);
        foreach (var rp in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) rp.RenderProbe();
        var cam = ck.cam; Vector3 p = new Vector3(px, py, pz), c = new Vector3(lx, ly, lz); cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(c - p); cam.focalLength = focal;
        DepthOfField dof; if (ck.volume.profile.TryGet(out dof)) { dof.focusDistance.value = Vector3.Distance(p, c); dof.aperture.value = fstop; dof.focalLength.value = focal; }
        RenderTo(cam, 1920, 1080, "Capture/" + name + ".jpg"); return s + " -> Capture/" + name + ".jpg";
    }

    /// n stills between film times t0 and t1 from one camera position, 960 x 540, as Capture/<dir>/<name>_NN.jpg (the clock only; the singer is posed by her own clock)
    public static string FilmSeq(float t0, float t1, int n, float px, float py, float pz, float lx, float ly, float lz, float focal, float fstop, string dir, string name)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); var film = Object.FindFirstObjectByType<FilmClock>(); if (ck == null || film == null) return "no stage"; Film(t0);
        foreach (var rp in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) rp.RenderProbe();
        var cam = ck.cam; Vector3 p = new Vector3(px, py, pz), c = new Vector3(lx, ly, lz); cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(c - p); cam.focalLength = focal;
        DepthOfField dof; if (ck.volume.profile.TryGet(out dof)) { dof.focusDistance.value = Vector3.Distance(p, c); dof.aperture.value = fstop; dof.focalLength.value = focal; }
        Directory.CreateDirectory("Capture/" + dir);
        for (int i = 0; i < n; i++) { float t = n > 1 ? Mathf.Lerp(t0, t1, i / (float)(n - 1)) : t0; if (ck.singer != null) ck.singer.Apply(t); film.Apply(t); RenderTo(cam, 960, 540, $"Capture/{dir}/{name}_{i:00}.jpg"); }
        return $"{name}: {n} stills {t0:F2}..{t1:F2}";
    }

    /// where the things a camera has to know about are (after Film(t))
    public static string Measure(float t)
    {
        Film(t); var stage = GameObject.Find("Stage"); var sb = new System.Text.StringBuilder();
        string[] names = { "HouseR", "HouseL", "FarRow", "LampPost", "globe", "Street", "Cyc", "PaperSun", "Cloud0", "Cloud1", "Cloud2", "RedPaper", "BareBulbProp", "bulb_glow", "Proscenium", "Person0", "Person3", "Tape0", "Rain", "StageHand", "WingR0", "WingL0", "GhostLamp", "Bird", "Folds", "GreyPaper", "Corner", "GhostBulb", "ghost_glow", "GhostKey" };
        foreach (var n in names) { Transform x = null; foreach (var tr in stage.GetComponentsInChildren<Transform>(true)) if (tr.name == n) { x = tr; break; } if (x == null) { sb.Append(n + ": none; "); continue; } var rs = x.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) { sb.Append($"{n}: pos {x.position}; "); continue; } var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); sb.Append($"{n}: min {b.min} max {b.max}; "); }
        var ck = Object.FindFirstObjectByType<StageClock>(); for (int i = 0; i < ck.panes.Length; i++) sb.Append($"pane{i} {ck.panes[i].transform.position}; ");
        var hd = ck.singer.animator.GetBoneTransform(HumanBodyBones.Head); sb.Append($"lin head {hd.position} root {ck.singer.transform.position} rhand {ck.singer.animator.GetBoneTransform(HumanBodyBones.RightHand).position}; ");
        int nb = 0; Vector3 bmin = Vector3.one * 99, bmax = -Vector3.one * 99; foreach (var r in stage.GetComponentsInChildren<MeshRenderer>(true)) { if (r.name == "bulb") { nb++; bmin = Vector3.Min(bmin, r.transform.position); bmax = Vector3.Max(bmax, r.transform.position); } }
        sb.Append($"bulbs {nb} min {bmin} max {bmax}; "); int nf = 0; Vector3 fmin = Vector3.one * 99, fmax = -Vector3.one * 99; foreach (var r in stage.GetComponentsInChildren<MeshRenderer>(true)) if (r.name == "FarPane") { nf++; fmin = Vector3.Min(fmin, r.transform.position); fmax = Vector3.Max(fmax, r.transform.position); }
        sb.Append($"farpanes {nf} min {fmin} max {fmax}"); return sb.ToString();
    }

    /// ready the play-mode pass from the television camera over film frames f0..f1 (every `step`-th frame)
    public static string SetupFilmBaker(int f0, int f1, int step, int w, int h, string dir)
    {
        foreach (var old in Object.FindObjectsByType<SliceBaker>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
        foreach (var old in Object.FindObjectsByType<FilmBaker>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
        foreach (var old in Object.FindObjectsByType<ShotBaker>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);      /* or it would run too and move the camera */
        var film = Object.FindFirstObjectByType<FilmClock>(); var ck = Object.FindFirstObjectByType<StageClock>(); if (film == null || ck == null) return "no stage";
        State("night"); film.Bind(); film.Apply(f0 / 24f);
        var g = new GameObject("FilmBaker"); var fb = g.AddComponent<FilmBaker>(); fb.film = film; fb.singer = ck.singer; fb.cam = ck.cam; fb.f0 = f0; fb.f1 = f1; fb.step = step; fb.width = w; fb.height = h; fb.dir = "Capture/" + dir;
        if (Directory.Exists("Capture/" + dir)) Directory.Delete("Capture/" + dir, true);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return "film baker ready: " + f0 + ".." + f1 + " step " + step;
    }
    public static string FilmProgress() { return $"playing {EditorApplication.isPlaying} written {FilmBaker.Written} done {FilmBaker.Done} {FilmBaker.Note}"; }

    /// show only the named properties (comma separated; "Fold3", "Bird", "RedPaper" ...) and take a still of them
    public static string PropStill(string names, float px, float py, float pz, float lx, float ly, float lz, float focal, float fstop, string file)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); var props = GameObject.Find("Props"); if (ck == null || props == null) return "no props";
        var want = new HashSet<string>(names.Split(','));
        foreach (Transform t in props.transform) t.gameObject.SetActive(want.Contains(t.name));
        var folds = props.transform.Find("Folds"); if (folds != null) { bool any = false; foreach (Transform f in folds) { bool on = want.Contains(f.name); f.gameObject.SetActive(on); any |= on; } folds.gameObject.SetActive(any); }
        var cam = ck.cam; Vector3 p = new Vector3(px, py, pz), c = new Vector3(lx, ly, lz); cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(c - p); cam.focalLength = focal;
        DepthOfField dof; if (ck.volume.profile.TryGet(out dof)) { dof.focusDistance.value = Vector3.Distance(p, c); dof.aperture.value = fstop; dof.focalLength.value = focal; }
        RenderTo(cam, 1280, 720, "Capture/" + file + ".jpg"); return "wrote Capture/" + file + ".jpg";
    }

    /// switch a named object of the stage on or off (inactive ones are found too)
    public static string Show(string name, bool on)
    {
        var stage = GameObject.Find("Stage"); if (stage == null) return "no stage"; int n = 0;
        foreach (var t in stage.GetComponentsInChildren<Transform>(true)) if (t.name == name) { t.gameObject.SetActive(on); n++; }
        return name + (on ? " on" : " off") + " x" + n;
    }

    /// try a head on a person with a given turn (degrees about y, then about x): for finding which way a generated head looks
    public static string HeadTurn(string person, float yaw, float pitch, string file)
    {
        var stage = GameObject.Find("Stage"); Transform pr = null; foreach (var t in stage.GetComponentsInChildren<Transform>(true)) if (t.name == person) pr = t; if (pr == null) return "no " + person;
        Transform hd = null, mask = null; foreach (var t in pr.GetComponentsInChildren<Transform>(true)) { if (t.name == "Head") hd = t; if (t.name == "mask") mask = t; }
        mask.localPosition = Vector3.zero; mask.localEulerAngles = new Vector3(pitch, yaw, 0); var b = BoundsOf(mask.gameObject); mask.position += hd.position - new Vector3(b.center.x, b.min.y, b.center.z);
        var ck = Object.FindFirstObjectByType<StageClock>(); var cam = ck.cam; Vector3 c = hd.position + Vector3.up * 0.16f, p = c + new Vector3(0.25f, 0.05f, -1.1f); cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(c - p); cam.focalLength = 70f;
        DepthOfField dof; if (ck.volume.profile.TryGet(out dof)) { dof.focusDistance.value = Vector3.Distance(p, c); dof.aperture.value = 8f; dof.focalLength.value = 70f; }
        RenderTo(cam, 640, 360, "Capture/" + file + ".jpg"); return "wrote " + file;
    }

    /// lin in one of her poses (0..7), from the front and from her right side
    public static string PoseStill(int id, string file)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null || ck.singer == null) return "no singer"; var sp = ck.singer;
        foreach (var r in sp.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        sp.forcePose = id; var cam = ck.cam; DepthOfField dof; ck.volume.profile.TryGet(out dof);
        Vector3 c = sp.transform.position + Vector3.up * 1.12f; string outp = "";
        foreach (var (tag, off, focal) in new[] { ("a", sp.transform.TransformVector(new Vector3(0.25f, 0.10f, 2.3f)), 50f), ("b", sp.transform.TransformVector(new Vector3(2.1f, 0.15f, 0.9f)), 50f) })
        {
            cam.transform.position = c + off; cam.transform.rotation = Quaternion.LookRotation(-off); cam.focalLength = focal; if (dof != null) { dof.focusDistance.value = off.magnitude; dof.aperture.value = 8f; dof.focalLength.value = focal; }
            sp.Apply(150f); sp.Apply(150f); RenderTo(cam, 640, 720, "Capture/" + file + "_" + tag + ".jpg"); outp += tag;
        }
        sp.forcePose = -1; return "pose " + id + " " + outp;
    }
