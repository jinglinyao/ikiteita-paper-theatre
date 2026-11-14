    // ---------------------------------------------------------------- the shots of the film (room side)
    // The room is real things at rest; what changes is the television (its picture is the stage seen by the television camera, frame
    // for frame), the glow it throws, and at the very end the daylight. Rendered in edit mode, one still a frame.
    static bool RoomShotType(int n) { char c = FilmShots.TYPE[n - 1]; return c == 'R' || c == 'X'; }

    /// how much of the picture tube is lit at film time t: x and y extent of the raster (0..1) and the brightness of what is left
    static void TubePower(float t, out float sx, out float sy, out float gain, out float white)
    {
        const float ON = 0.60f, OFF = 186.50f; sx = sy = 1f; gain = 1f; white = 0f;
        if (t < ON) { sx = sy = 0f; gain = 0f; return; }
        if (t < ON + 0.45f)
        {   // a line drawn across the glass, then pulled open
            float a = Mathf.Clamp01((t - ON) / 0.14f), b = Mathf.Clamp01((t - ON - 0.14f) / 0.31f); b = b * b * (3 - 2 * b);
            sx = Mathf.Lerp(0.02f, 1f, a); sy = Mathf.Lerp(0.010f, 1f, b); gain = Mathf.Lerp(3.0f, 1f, b); white = sy < 0.2f ? 1f : 0f; return;
        }
        if (t >= OFF)
        {   // the picture falls to a line (two frames), the line to a point, the point dies
            float a = Mathf.Clamp01((t - OFF) / 0.085f), b = Mathf.Clamp01((t - OFF - 0.085f) / 0.26f), c = Mathf.Clamp01((t - OFF - 0.345f) / 0.85f);
            sy = Mathf.Lerp(1f, 0.010f, a); sx = Mathf.Lerp(1f, 0.014f, b * b); if (b >= 1f) sy = 0.014f; white = sy < 0.2f ? 1f : 0f; gain = Mathf.Lerp(1f, 3.4f, a) * (1f - c) * (1f - c); if (c >= 1f) { sx = sy = 0f; gain = 0f; }
        }
    }

    static bool RoomCam(int n, float t, Vector3 sc, out Vector3 p, out Vector3 look, out float focal, out float fstop, out Vector3 focus)
    {
        float t0 = FilmShots.F0[n - 1] / 24f, t1 = FilmShots.F1[n - 1] / 24f, u = Mathf.Clamp01((t - t0) / (t1 - t0)), e = u * u * (3 - 2 * u);
        Vector3 wide0 = new Vector3(0.95f, 1.20f, -1.00f), wideLook = new Vector3(-0.55f, 0.82f, 1.15f), wdir = (wideLook - wide0).normalized, wide1 = wide0 + wdir * 0.30f + new Vector3(0, -0.10f, 0);
        p = wide0; look = wideLook; focal = 24f; fstop = 4f; focus = sc;
        switch (n)
        {
            case 1: p = sc + new Vector3(0, 0, -0.70f + 0.05f * e); look = sc; focal = 50f; fstop = 2.8f; return true;
            case 2: p = sc + new Vector3(Mathf.Lerp(0.22f, 0.28f, e), 0.05f, -0.30f); look = sc + new Vector3(0.07f, 0, 0); focal = 50f; fstop = 2.8f; return true;
            case 3: p = new Vector3(-0.16f + 0.02f * e, 0.72f, 0.56f); look = new Vector3(-0.30f, 0.66f, 0.90f); focal = 70f; fstop = 2.8f; focus = look; return true;
            case 4: { float uu = Mathf.Clamp01((t - t0) / (9.82f - t0)); p = Vector3.Lerp(wide0, wide1, uu * uu * (3 - 2 * uu)); return true; }      /* the push settles on bar 5 */
            case 5: p = new Vector3(Mathf.Lerp(0.66f, 0.74f, e), 1.30f, 0.38f); look = new Vector3(0.95f, 1.04f, 1.12f); focal = 60f; fstop = 2.8f; focus = Vector3.Lerp(new Vector3(0.80f, 0.78f, 1.035f), new Vector3(1.04f, 0.80f, 1.235f), e); return true;
            case 6: p = new Vector3(-0.3f, 1.2f, -0.2f); look = new Vector3(-1.1f, 1.1f, 1.365f); focal = 50f; fstop = 2.8f; focus = look; return true;
            case 7: { float a = u * u * u * (u * (6f * u - 15f) + 10f); p = Vector3.Lerp(sc + new Vector3(0.10f, 0.05f, -1.00f), sc + new Vector3(0, 0, -0.30f), a); look = Vector3.Lerp(sc + new Vector3(0.02f, 0, 0), sc, a); focal = 35f; fstop = 4f; return true; }
            case 46: p = wide1 + wdir * 0.15f * e; return true;
            case 52: p = new Vector3(Mathf.Lerp(0.74f, 0.78f, e), 1.30f, 0.38f); look = new Vector3(0.95f, 1.04f, 1.12f); focal = 60f; fstop = 2.8f; focus = new Vector3(1.04f, 0.80f, 1.235f); return true;
            case 74: p = Vector3.Lerp(sc + new Vector3(0, 0, -0.30f), sc + new Vector3(0.08f, -0.15f, -1.20f), e); look = Vector3.Lerp(sc, sc + new Vector3(0.02f, -0.03f, 0), e); focal = 35f; fstop = 4f; return true;
            case 81: p = sc + new Vector3(0, 0, -0.70f); look = sc; focal = 50f; fstop = 2.8f; return true;
            case 82: p = wide1 - wdir * 0.20f * e; return true;
        }
        return false;
    }

    /// Frames of the room shots in the list (with `handle` extra frames each side) into Capture/<dir>/Snn/f_#####.jpg. `tvDir` holds the
    /// television camera's frames of the whole film (f_#####.jpg). step > 1 renders every step-th frame (for a quick look).
    public static string RoomFilm(string spec, int handle, int step, int w, int h, string dir, string tvDir)
    {
        var room = GameObject.Find("Room"); var camGo = GameObject.Find("ShotCam"); var scr = GameObject.Find("Screen"); if (room == null || camGo == null || scr == null) return "no room";
        var cam = camGo.GetComponent<Camera>(); var vol = Object.FindFirstObjectByType<Volume>(); DepthOfField dof = null; if (vol != null) vol.profile.TryGet(out dof);
        var scrR = scr.GetComponent<Renderer>(); var keepMat = scrR.sharedMaterial; var mat = new Material(keepMat); scrR.sharedMaterial = mat; Vector3 sc = scr.transform.position, scale0 = scr.transform.localScale;
        Light glow = null, sun = null, desk = null; foreach (var l in room.GetComponentsInChildren<Light>(true)) { if (l.name == "TvGlow") glow = l; else if (l.name == "Sun") sun = l; else if (l.name == "DeskLampLight") desk = l; } float desk0 = desk != null ? desk.intensity : 0f;
        Transform plant = null, tvT = null, bird = null; foreach (var x in room.GetComponentsInChildren<Transform>(true)) { if (x.name == "PlantBig") plant = x; else if (x.name == "TV") tvT = x; else if (x.name == "RoomBird") bird = x; }
        if (bird == null && tvT != null)
        {   // the red paper bird that is found on the television in the morning
            var red = CardM("room_bird_red", new Color(0.74f, 0.09f, 0.08f), 0.12f, 1); var redD = CardM("room_bird_red_dark", new Color(0.56f, 0.06f, 0.06f), 0.12f, 1);
            bird = Bird(room.transform, "RoomBird", red, redD); var tb = BoundsOf(tvT.gameObject); bird.position = new Vector3(tb.center.x + 0.07f, tb.max.y + 0.001f, tb.center.z - 0.02f); bird.localEulerAngles = new Vector3(0, 205f, 0);
            foreach (var wn in bird.GetComponentsInChildren<Transform>()) { if (wn.name == "WingL") wn.localEulerAngles = new Vector3(0, 0, -62f); if (wn.name == "WingR") wn.localEulerAngles = new Vector3(0, 0, 62f); }
        }
        if (bird != null) foreach (var r in bird.GetComponentsInChildren<Renderer>(true)) { var m = r.sharedMaterial; if (m == null) continue; m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; m.SetColor("_EmissionColor", new Color(0.50f, 0.045f, 0.035f)); }      /* thin paper with the sun behind it glows */
        Quaternion plantRot = plant != null ? plant.localRotation : Quaternion.identity; Vector3 sunAim = new Vector3(0f, 0.9f, 1.365f);
        var tex = new Texture2D(2, 2, TextureFormat.RGB24, false); var white = Texture2D.whiteTexture; int n0 = 0; var note = new StringBuilder(); string state = "";
        foreach (int n in ParseShots(spec))
        {
            if (n < 1 || n > FilmShots.N || !RoomShotType(n)) continue; bool dawn = n == 82; string want = dawn ? "dawn" : "night"; if (want != state) { RoomState(want); state = want; }
            if (bird != null) bird.gameObject.SetActive(dawn); if (desk != null) desk.intensity = desk0 * (n == 5 || n == 52 ? 0.20f : 1f);      /* the close shots of the desk are exposed for the lamp */
            int a = Mathf.Max(0, FilmShots.F0[n - 1] - handle), b = Mathf.Min(FilmShots.F1[FilmShots.N - 1], FilmShots.F1[n - 1] + handle);
            for (int f = a; f < b; f += step)
            {
                float t = f / 24f; Vector3 p, look, focus; float focal, fstop; if (!RoomCam(n, t, sc, out p, out look, out focal, out fstop, out focus)) { note.Append(" S" + n + " no camera"); break; }
                // the television: its picture, how much of the tube is lit, the light it throws into the room
                float sx, sy, gain, wh; TubePower(t, out sx, out sy, out gain, out wh); Color mean = new Color(0.10f, 0.12f, 0.18f);
                string tvp = $"Capture/{tvDir}/f_{Mathf.Clamp(f, 0, 4633):00000}.jpg";
                if (File.Exists(tvp)) { tex.LoadImage(File.ReadAllBytes(tvp)); var px = tex.GetPixels32(); long r = 0, g = 0, bl = 0; int cnt = 0; for (int i = 0; i < px.Length; i += 37) { r += px[i].r; g += px[i].g; bl += px[i].b; cnt++; } mean = new Color(r / (255f * cnt), g / (255f * cnt), bl / (255f * cnt)); }
                bool on = !dawn && gain > 0.001f && sx > 0f; scrR.enabled = on;
                if (on)
                {
                    float su = Mathf.Clamp01((t - FilmShots.F0[n - 1] / 24f) / ((FilmShots.F1[n - 1] - FilmShots.F0[n - 1]) / 24f)), near = n == 7 ? Mathf.SmoothStep(0f, 1f, (su - 0.55f) / 0.40f) : n == 74 ? 1f - Mathf.SmoothStep(0f, 1f, (su - 0.05f) / 0.40f) : 0f, scr0 = Mathf.Lerp(2.2f, n == 7 ? 0.30f : 0.64f, near);
                    mat.SetTexture("_BaseMap", wh > 0.5f ? white : tex); Color bc = Color.Lerp(new Color(scr0, scr0, scr0, 1), new Color(2.6f, 2.9f, 3.4f, 1), wh) * gain; bc.a = 1; mat.SetColor("_BaseColor", bc);
                    scr.transform.localScale = new Vector3(scale0.x * sx, scale0.y * sy, scale0.z);
                }
                if (glow != null)
                {
                    float luma = 0.3f * mean.r + 0.59f * mean.g + 0.11f * mean.b; Color gc = luma > 0.004f ? Color.Lerp(mean / Mathf.Max(0.02f, mean.maxColorComponent), Color.white, 0.25f) : new Color(0.56f, 0.74f, 1f);
                    float area = sx * sy, lv = Mathf.Lerp(Mathf.Clamp(luma / 0.11f, 0.06f, 2.4f) * area, 0.5f * gain * Mathf.Sqrt(Mathf.Max(0, area)), wh);
                    glow.enabled = on && lv > 0.003f; glow.color = Color.Lerp(gc, new Color(0.80f, 0.88f, 1f), wh); glow.intensity = 13f * lv * (n == 46 || n == 74 ? 2.6f : 1f);      /* where the shot is about the light the television throws on the room */
                }
                if (plant != null) plant.localRotation = plantRot * Quaternion.Euler(0.5f * Mathf.Sin(t * 1.1f + 0.4f), 1.4f * Mathf.Sin(t * 0.83f) + 0.6f * Mathf.Sin(t * 2.3f), 0.4f * Mathf.Sin(t * 1.7f));   // the leaves are never quite still
                if (dawn && sun != null) { float uu = Mathf.Clamp01((t - FilmShots.F0[n - 1] / 24f) / ((FilmShots.F1[n - 1] - FilmShots.F0[n - 1]) / 24f)); Vector3 aim = sunAim + new Vector3(Mathf.Lerp(-1.3f, 0.1f, uu), 0, 0); sun.transform.rotation = Quaternion.LookRotation(aim - sun.transform.position); }
                cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(look - p); cam.focalLength = focal;
                if (dof != null) { dof.focusDistance.value = Mathf.Max(0.1f, Vector3.Dot(focus - p, cam.transform.forward)); dof.aperture.value = fstop; dof.focalLength.value = focal; }
                foreach (var rp in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) rp.RenderProbe();
                RenderTo(cam, w, h, $"Capture/{dir}/S{n:00}/f_{f:00000}.jpg"); n0++;
            }
        }
        scrR.sharedMaterial = keepMat; scr.transform.localScale = scale0; scrR.enabled = true; if (plant != null) plant.localRotation = plantRot; if (sun != null) sun.transform.rotation = Quaternion.LookRotation(sunAim - sun.transform.position);
        if (bird != null) bird.gameObject.SetActive(false); if (desk != null) desk.intensity = desk0; RoomState("night"); Object.DestroyImmediate(mat); Object.DestroyImmediate(tex);
        return $"room: {n0} frames -> Capture/{dir}" + note;
    }

    /// the clock on the desk turned to face the room, and a soft contact shadow under it and under the lamp's clamp (they stood on the desk without one and read as floating)
    public static string DeskTouches(float yaw)
    {
        var clock = GameObject.Find("Clock"); var lamp = GameObject.Find("DeskLamp"); if (clock == null) return "no clock";
        clock.transform.eulerAngles = new Vector3(0, yaw, 0);
        foreach (var old in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (old != null && old.name == "ContactShadow") Object.DestroyImmediate(old.gameObject);
        const int N = 64; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false); tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { float r = new Vector2((x + 0.5f) / N - 0.5f, (y + 0.5f) / N - 0.5f).magnitude * 2f, a = Mathf.Clamp01(1 - r); a = a * a * (3 - 2 * a); tex.SetPixel(x, y, new Color(0.03f, 0.02f, 0.015f, a * 0.9f)); }
        tex.Apply(); File.WriteAllBytes("Assets/Gen/contact_shadow.png", tex.EncodeToPNG()); AssetDatabase.ImportAsset("Assets/Gen/contact_shadow.png"); var ti = (TextureImporter)AssetImporter.GetAtPath("Assets/Gen/contact_shadow.png"); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport(); var texA = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Gen/contact_shadow.png");
        Material Clear(string name, Color col, Texture2D map, float smooth)
        {
            var mm = new Material(Shader.Find("Universal Render Pipeline/" + (map != null ? "Unlit" : "Lit"))); mm.SetFloat("_Surface", 1); mm.SetFloat("_Blend", 0); mm.SetFloat("_SrcBlend", 5); mm.SetFloat("_DstBlend", 10); mm.SetFloat("_ZWrite", 0); mm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mm.SetOverrideTag("RenderType", "Transparent"); mm.renderQueue = 3000;
            mm.SetColor("_BaseColor", col); if (map != null) mm.SetTexture("_BaseMap", map); else mm.SetFloat("_Smoothness", smooth); return Save(mm, name);
        }
        var m = Clear("contact_shadow", Color.white, texA, 0);
        foreach (var r0 in clock.GetComponentsInChildren<Renderer>()) { var ms = r0.sharedMaterials; bool ch = false; for (int k = 0; k < ms.Length; k++) if (ms[k] != null && ms[k].name.Contains("Glass")) { ms[k] = Clear("clock_glass_clear", new Color(0.9f, 0.95f, 1f, 0.06f), null, 0.9f); ch = true; } if (ch) r0.sharedMaterials = ms; }
        var sb = new StringBuilder();
        void Blob(Vector3 c, float sx, float sz, float yawB)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(q.GetComponent<Collider>()); q.name = "ContactShadow"; q.transform.SetParent(clock.transform.parent, true);
            q.transform.position = c; q.transform.eulerAngles = new Vector3(90, yawB, 0); q.transform.localScale = new Vector3(sx, sz, 1); var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; sb.Append($" blob {c:F3}");
        }
        var cb = BoundsOf(clock); Blob(new Vector3(cb.center.x, cb.min.y + 0.0015f, cb.center.z), 0.26f, 0.20f, yaw);
        if (lamp != null)
        {   // the clamp: the lowest tenth of the lamp
            var mf = lamp.GetComponentInChildren<MeshFilter>(); Vector3 lo = Vector3.zero; int cnt = 0; float y0 = BoundsOf(lamp).min.y;
            foreach (var v in mf.sharedMesh.vertices) { var w = mf.transform.TransformPoint(v); if (w.y < y0 + 0.03f) { lo += w; cnt++; } }
            if (cnt > 0) { lo /= cnt; Blob(new Vector3(lo.x, y0 + 0.0012f, lo.z), 0.16f, 0.16f, 0); }
        }
        EditorSceneManager.MarkSceneDirty(clock.scene); EditorSceneManager.SaveScene(clock.scene); return "clock yaw " + yaw + sb;
    }
