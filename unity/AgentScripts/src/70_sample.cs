    // ---------------------------------------------------------------- the ten-second sample: bar 25 in the room, bars 26-29 on the stage
    /// the picture on the television during bar 25: the stage with only the pool of light, nobody in it
    public static string TvStill(float t, string state, bool hideSinger)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null) return "no stage";
        string s = State(state); if (hideSinger && ck.singer != null) foreach (var r in ck.singer.GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var rp in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) rp.RenderProbe();
        string w = TvFrame(t);
        if (ck.singer != null) foreach (var r in ck.singer.GetComponentsInChildren<Renderer>()) r.enabled = true;
        State("night"); return s + "\n" + w;
    }

    /// Frames f0..f1-1 of the room shot (film frames, 24 a second): from the wide, a slow push toward the television that drops a
    /// little and turns onto the screen; it settles on the bar line at frame 1326. Edit mode: nothing in the room moves but the camera.
    public static string RoomSeq(int f0, int f1, string dir)
    {
        var camGo = GameObject.Find("ShotCam"); if (camGo == null) return "no room"; var cam = camGo.GetComponent<Camera>(); var vol = Object.FindFirstObjectByType<Volume>();
        var scr = GameObject.Find("Screen"); Vector3 screen = scr != null ? scr.transform.position : new Vector3(-0.35f, 0.80f, 0.90f);
        var pic = AssetDatabase.LoadAssetAtPath<Texture2D>(GEN + "/tv_frame.png"); if (scr != null && pic != null) scr.GetComponent<Renderer>().sharedMaterial.SetTexture("_BaseMap", pic);
        Directory.CreateDirectory("Capture/" + dir); int n = 0; DepthOfField dof; vol.profile.TryGet(out dof);
        for (int f = f0; f < f1; f++)
        {
            float u = Mathf.Clamp01((f - 1272f) / (1326f - 1272f)); u = u * u * (3 - 2 * u);
            // from the corner by the bed, across the room: a piece of the window on the left, the plant, the television, the desk and its lamp;
            // then in on the television until it is a third of the frame
            Vector3 p = Vector3.Lerp(new Vector3(0.95f, 1.20f, -1.00f), new Vector3(0.12f, 0.93f, -0.34f), u), look = Vector3.Lerp(new Vector3(-0.55f, 0.82f, 1.15f), new Vector3(-0.37f, 0.79f, 0.90f), u);
            cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(look - p); cam.focalLength = Mathf.Lerp(24f, 40f, u);
            if (dof != null) { dof.focusDistance.value = Vector3.Distance(p, screen); dof.aperture.value = 2.8f; dof.focalLength.value = cam.focalLength; }
            foreach (var rp in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) rp.RenderProbe();
            RenderTo(cam, 2560, 1440, $"Capture/{dir}/f_{f:00000}.jpg"); n++;
        }
        return $"wrote {n} frames to Capture/{dir}";
    }

    /// the stage, in its night state, ready for the play-mode pass over bars 26-29
    public static string PrepSlice(int warmup)
    {
        string s = State("night"); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return s + "\n" + SetupBaker(warmup);
    }
