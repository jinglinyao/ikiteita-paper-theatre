    // ---------------------------------------------------------------- the shots of the film (stage side)
    /// "8-23,30,47-51" -> the shot numbers
    static List<int> ParseShots(string spec)
    {
        var l = new List<int>();
        foreach (var part in spec.Split(',')) { var q = part.Trim().Split('-'); if (q[0].Length == 0) continue; int a = int.Parse(q[0]), b = q.Length > 1 ? int.Parse(q[1]) : a; for (int n = a; n <= b; n++) l.Add(n); }
        return l;
    }
    static bool StageShot(int n) { char c = FilmShots.TYPE[n - 1]; return c == 'T' || c == 'A' || c == 'L'; }

    /// one still of every stage shot in the list, at the fraction u of its length (edit mode), as Capture/<dir>/Snn.jpg
    public static string ShotStills(string spec, float u, int w, int h, string dir)
    {
        var film = Object.FindFirstObjectByType<FilmClock>(); var ck = Object.FindFirstObjectByType<StageClock>(); if (film == null || ck == null) return "no stage";
        State("night"); film.Bind(); Directory.CreateDirectory("Capture/" + dir); var cam = ck.cam; var keep = cam.sensorSize; cam.sensorSize = new Vector2(36f, 20.25f); int n0 = 0; var miss = new StringBuilder();
        foreach (var rp in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) rp.RenderProbe();
        foreach (int n in ParseShots(spec))
        {
            if (n < 1 || n > FilmShots.N || !StageShot(n)) continue; float t = Mathf.Lerp(FilmShots.F0[n - 1], FilmShots.F1[n - 1], u) / 24f;
            if (ck.singer != null) ck.singer.Apply(t); film.Apply(t); if (!film.ShotCamera(n, t, cam)) { miss.Append(" S" + n); continue; }
            if (ck.singer != null) ck.singer.Apply(t); film.Apply(t); film.ShotCamera(n, t, cam);
            RenderTo(cam, w, h, $"Capture/{dir}/S{n:00}.jpg"); n0++;
        }
        cam.sensorSize = keep; return $"{n0} stills at u {u:F2} -> Capture/{dir}" + (miss.Length > 0 ? " | no camera:" + miss : "");
    }

    /// ready the play-mode pass over the stage shots of the list
    public static string SetupShotBaker(string spec, int handle, int step, int w, int h, string dir)
    {
        foreach (var old in Object.FindObjectsByType<SliceBaker>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
        foreach (var old in Object.FindObjectsByType<FilmBaker>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
        foreach (var old in Object.FindObjectsByType<ShotBaker>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
        var film = Object.FindFirstObjectByType<FilmClock>(); var ck = Object.FindFirstObjectByType<StageClock>(); if (film == null || ck == null) return "no stage";
        var list = ParseShots(spec).FindAll(StageShot); if (list.Count == 0) return "no stage shot in " + spec;
        State("night"); film.Bind(); film.Apply(FilmShots.F0[list[0] - 1] / 24f);
        var g = new GameObject("ShotBaker"); var sb = g.AddComponent<ShotBaker>(); sb.film = film; sb.singer = ck.singer; sb.cam = ck.cam; sb.shots = list.ToArray(); sb.handle = handle; sb.step = step; sb.width = w; sb.height = h; sb.dir = "Capture/" + dir;
        int frames = 0; foreach (int n in list) frames += (FilmShots.F1[n - 1] - FilmShots.F0[n - 1] + 2 * handle) / step;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return $"shot baker ready: {list.Count} shots, {frames} frames -> Capture/{dir}";
    }
    public static string ShotProgress() { return $"playing {EditorApplication.isPlaying} written {ShotBaker.Written} done {ShotBaker.Done} {ShotBaker.Note}"; }
