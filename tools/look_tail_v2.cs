    // ---------------------------------------------------------------- the seated pose
    static string lastSeatNote = "";
    static void Aim(Transform bone, Vector3 childPos, Vector3 dir)
    {
        Vector3 cur = (childPos - bone.position).normalized; bone.rotation = Quaternion.FromToRotation(cur, dir.normalized) * bone.rotation;
    }

    /// The gesture rig poses her standing; this sits her down afterwards: pelvis onto the chair, thighs forward, shins down, feet flat,
    /// the trunk leaning in a little, and the head turned to what she is watching.
    static void Seat(SingDriver d, Vector3 look, float headBlend = 0.7f)
    {
        var a = d.animator; var root = d.transform; Vector3 fwd = root.forward, up = Vector3.up, right = root.right;
        var hips = a.GetBoneTransform(HumanBodyBones.Hips);
        var uL = a.GetBoneTransform(HumanBodyBones.LeftUpperLeg); var lL = a.GetBoneTransform(HumanBodyBones.LeftLowerLeg); var fL = a.GetBoneTransform(HumanBodyBones.LeftFoot);
        float L1 = Vector3.Distance(uL.position, lL.position), L2 = Vector3.Distance(lL.position, fL.position);
        const float ANKLE = 0.085f, SLOPE = 5f, SHIN = 9f;
        float hipJoint = ANKLE + L2 * Mathf.Cos(SHIN * Mathf.Deg2Rad) + L1 * Mathf.Sin(SLOPE * Mathf.Deg2Rad);
        float hipToJoint = hips.position.y - uL.position.y;                                        // pelvis bone sits a little above the hip joints
        hips.position = new Vector3(root.position.x, root.position.y + hipJoint + hipToJoint, root.position.z) - fwd * 0.02f;
        hips.rotation = Quaternion.AngleAxis(7f, right) * hips.rotation;                           // leaning toward the screen
        for (int side = 0; side < 2; side++)
        {
            bool r = side == 0; float sg = r ? 1f : -1f;
            var u = a.GetBoneTransform(r ? HumanBodyBones.RightUpperLeg : HumanBodyBones.LeftUpperLeg); var l = a.GetBoneTransform(r ? HumanBodyBones.RightLowerLeg : HumanBodyBones.LeftLowerLeg);
            var f = a.GetBoneTransform(r ? HumanBodyBones.RightFoot : HumanBodyBones.LeftFoot); var toe = a.GetBoneTransform(r ? HumanBodyBones.RightToes : HumanBodyBones.LeftToes);
            Aim(u, l.position, fwd * Mathf.Cos(SLOPE * Mathf.Deg2Rad) - up * Mathf.Sin(SLOPE * Mathf.Deg2Rad) + right * sg * 0.05f);
            Aim(l, f.position, -up * Mathf.Cos(SHIN * Mathf.Deg2Rad) + fwd * Mathf.Sin(SHIN * Mathf.Deg2Rad) * (r ? 1f : 0.4f) - right * sg * 0.03f);   // one foot a little ahead of the other
            if (toe != null) Aim(f, toe.position, fwd - up * 0.04f + right * sg * 0.10f);
        }
        var head = a.GetBoneTransform(HumanBodyBones.Head);
        if (head != null) { Vector3 to = look - head.position; head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(to, up), headBlend); }
        lastSeatNote = $"thigh {L1:F3} shin {L2:F3} hip joint height {hipJoint:F3} (seat top should be about {hipJoint - 0.075f:F3}) head {head.position:F2}";
    }

    static Vector3 ScreenPos() { var s = GameObject.Find("Screen"); return s != null ? s.transform.position : new Vector3(-1.42f, 0.80f, 0.63f); }

    /// pose her for film time t (seconds): gestures, breathing, blinks and gaze from the rig, then seated
    public static void PoseAt(float t)
    {
        var d = Object.FindFirstObjectByType<SingDriver>(); if (d == null) return;
        d.Apply(t); Seat(d, ScreenPos());
    }

    // ---------------------------------------------------------------- capture
    static void RenderTo(Camera cam, int w, int h, string path)
    {
        // one persistent target per size, kept as an asset and never destroyed (destroying a target a camera has just used crashed the Editor;
        // pooled temporaries handed back another camera's picture)
        EnsureDir(GEN); string ap = GEN + $"/capture_{w}x{h}.renderTexture";
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(ap);
        if (rt == null) { rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 }; AssetDatabase.CreateAsset(rt, ap); }
        var keep = cam.targetTexture; cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); RenderTexture.active = null;
        cam.targetTexture = keep;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, path.EndsWith(".png") ? tex.EncodeToPNG() : tex.EncodeToJPG(95));
        Object.DestroyImmediate(tex);
    }

    struct View { public Vector3 pos, look; public float fov; }
    static Vector3 HerRight => Quaternion.Euler(0, SEAT_YAW, 0) * Vector3.right;
    static Vector3 HeadPos() { var d = Object.FindFirstObjectByType<SingDriver>(); return d != null ? d.animator.GetBoneTransform(HumanBodyBones.Head).position + Vector3.up * 0.06f : SEAT + Vector3.up * 1.1f; }
    /// over her shoulder toward the screen (u 0 → 1 pushes in)
    static View Ots(float u) { Vector3 s = ScreenPos(); return new View { pos = Vector3.Lerp(SEAT + TVF * 0.95f + HerRight * 0.52f + Vector3.up * 1.20f, SEAT + TVF * 0.50f + HerRight * 0.40f + Vector3.up * 1.12f, u), look = s + Vector3.up * 0.02f, fov = Mathf.Lerp(40f, 29f, u) }; }
    /// from beside the television back at her face (u 0 → 1 drifts closer)
    static View Face(float u) { Vector3 s = ScreenPos(); return new View { pos = Vector3.Lerp(s + TVF * 0.10f - HerRight * 0.62f + Vector3.up * 0.26f, s + TVF * 0.30f - HerRight * 0.44f + Vector3.up * 0.30f, u), look = HeadPos() - Vector3.up * 0.10f, fov = Mathf.Lerp(37f, 31f, u) }; }
    static void Put(Camera c, View v, DepthOfField dof, Vector3 focusOn) { c.transform.position = v.pos; c.transform.rotation = Quaternion.LookRotation(v.look - v.pos); c.fieldOfView = v.fov; if (dof != null) dof.focusDistance.value = Vector3.Distance(v.pos, focusOn); }

    /// stills: 0 = all four, 1 = room views only, 2 = street only, 3 = + television close-up
    public static string Still(int which)
    {
        var tc = GameObject.Find("TheatreCam").GetComponent<Camera>(); var rc = GameObject.Find("RoomCam").GetComponent<Camera>();
        PoseAt(3.4f);                                                 // mid-reaction: the hand is at her chest
        tc.Render();
        DepthOfField dofR = null; AssetDatabase.LoadAssetAtPath<VolumeProfile>(GEN + "/post_room.asset").TryGet(out dofR); float f0 = dofR != null ? dofR.focusDistance.value : 0;
        Vector3 p0 = rc.transform.position; Quaternion r0 = rc.transform.rotation; float fov0 = rc.fieldOfView;
        if (which != 2)
        {
            if (dofR != null) dofR.focusDistance.value = Vector3.Distance(p0, HeadPos());
            RenderTo(rc, 1920, 1080, "Capture/still_wide.jpg");
            Put(rc, Ots(0.3f), dofR, ScreenPos()); RenderTo(rc, 1920, 1080, "Capture/still_ots.jpg");
            Put(rc, Face(0.5f), dofR, HeadPos()); RenderTo(rc, 1920, 1080, "Capture/still_face.jpg");
        }
        if (which == 3) { var s = GameObject.Find("Screen").transform; rc.transform.position = s.position - s.forward * 0.9f + Vector3.up * 0.08f; rc.transform.LookAt(s.position); rc.fieldOfView = 34; RenderTo(rc, 1280, 720, "Capture/tv_close.jpg"); }
        rc.transform.position = p0; rc.transform.rotation = r0; rc.fieldOfView = fov0; if (dofR != null) dofR.focusDistance.value = f0;
        if (which != 1) RenderTo(tc, 1920, 1080, "Capture/still_street.jpg");
        return "stills written; " + lastSeatNote;
    }

    /// her picture on a transparent ground, standing, front on, to be cut out as a paper puppet (Capture/jinglin_cut.png)
    public static string Cutout()
    {
        var dr = Object.FindFirstObjectByType<SingDriver>(); if (dr == null) return "no character";
        var go = dr.gameObject; var gn = dr.gestureNames; var gs = dr.gestureStart; var gd = dr.gestureDur;
        dr.plantFeet = true; dr.gestureNames = new string[0]; dr.gestureStart = new float[0]; dr.gestureDur = new float[0]; dr.Apply(1.0f);          // standing, hands folded
        // she stays where she is (teleporting a skinned mesh and rendering at once culls it on stale bounds); only her layer is drawn
        var trs = go.GetComponentsInChildren<Transform>(true); var layers = trs.Select(t => t.gameObject.layer).ToArray(); foreach (var t in trs) t.gameObject.layer = 8;
        Vector3 c = go.transform.position + Vector3.up * 0.80f;
        var cg = new GameObject("CutCam"); var cam = cg.AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 0.86f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 10; cam.cullingMask = 1 << 8;
        cam.transform.position = c + go.transform.forward * 3f; cam.transform.LookAt(c);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
        var ad = cg.AddComponent<UniversalAdditionalCameraData>(); ad.renderPostProcessing = false; ad.renderShadows = false;
        var lg = new GameObject("CutLight"); var l = lg.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 0.80f; l.color = Color.white; l.shadows = LightShadows.None;
        l.transform.rotation = Quaternion.LookRotation(-go.transform.forward + go.transform.right * -0.25f + Vector3.down * 0.35f);
        var others = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(x => x != l && x.enabled).ToArray(); foreach (var o in others) o.enabled = false;
        var amb0 = RenderSettings.ambientLight; RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.34f);
        int w = 1100, h = 2064; var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 }; cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGBA32, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); RenderTexture.active = null;
        Directory.CreateDirectory("Capture"); File.WriteAllBytes("Capture/jinglin_cut.png", tex.EncodeToPNG());
        cam.targetTexture = null; Object.DestroyImmediate(tex); Object.DestroyImmediate(cg); Object.DestroyImmediate(lg); RenderSettings.ambientLight = amb0;
        foreach (var o in others) o.enabled = true; for (int i = 0; i < trs.Length; i++) trs[i].gameObject.layer = layers[i];
        dr.plantFeet = false; dr.gestureNames = gn; dr.gestureStart = gs; dr.gestureDur = gd; PoseAt(0f);
        return "cut-out source written";
    }

    // ---------------------------------------------------------------- moving shots
    static float Ease(float u) { u = Mathf.Clamp01(u); return u * u * (3 - 2 * u); }
    struct Sway { public Transform t; public Vector3 pos; public Quaternion rot; public float amp, speed, phase; }

    /// shot 1 = her face, lit by the screen, the camera drifting in (her hand comes up to her chest);
    /// shot 2 = over her shoulder, pushing in on the television; shot 3 = the paper street, the camera sliding sideways so the flats part.
    /// t0 = film time of the first frame. Frames go to Capture/shot<N>/f_00000.jpg at 30 fps. Paper moves on twos, the camera on ones.
    public static string Shot(int which, int frames, float t0)
    {
        var tc = GameObject.Find("TheatreCam").GetComponent<Camera>(); var rc = GameObject.Find("RoomCam").GetComponent<Camera>();
        var sways = new List<Sway>();
        void Add(string n, float amp, float speed, float phase) { var g = GameObject.Find(n); if (g != null) sways.Add(new Sway { t = g.transform, pos = g.transform.position, rot = g.transform.rotation, amp = amp, speed = speed, phase = phase }); }
        Add("crowd_A", 1.3f, 1.9f, 0f); Add("crowd_B", 1.1f, 2.3f, 1.7f); Add("puppet_jinglin", 1.6f, 1.3f, 0.6f); Add("tape", 0.35f, 2.9f, 2.2f);
        Vector3 rp0 = rc.transform.position, tp0 = tc.transform.position; Quaternion rr0 = rc.transform.rotation, tr0 = tc.transform.rotation; float rf0 = rc.fieldOfView, tf0 = tc.fieldOfView;
        DepthOfField dofR = null; AssetDatabase.LoadAssetAtPath<VolumeProfile>(GEN + "/post_room.asset").TryGet(out dofR); float fr0 = dofR != null ? dofR.focusDistance.value : 0;
        var glowGo = GameObject.Find("TvGlow"); Light glow = glowGo != null ? glowGo.GetComponent<Light>() : null; float g0 = glow != null ? glow.intensity : 0;
        string dir = "Capture/shot" + which; if (Directory.Exists(dir)) Directory.Delete(dir, true); Directory.CreateDirectory(dir);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int f = 0; f < frames; f++)
        {
            float t = t0 + f / 30f, u = Ease(f / (float)(frames - 1)), tq = Mathf.Floor(t * 12f) / 12f;
            foreach (var s in sways) { s.t.position = s.pos; s.t.rotation = s.rot; if (s.amp > 0) s.t.RotateAround(s.pos - s.t.up * s.t.lossyScale.y * 0.5f, s.t.forward, s.amp * Mathf.Sin(tq * s.speed + s.phase)); }
            if (glow != null) glow.intensity = g0 * (0.80f + 0.34f * Mathf.PerlinNoise(tq * 2.3f, 0.5f));          // the picture changes, so does its light
            PoseAt(t);
            if (which == 1) { Put(rc, Face(u), dofR, HeadPos()); tc.Render(); RenderTo(rc, 1920, 1080, $"{dir}/f_{f:00000}.jpg"); }
            else if (which == 2) { Put(rc, Ots(u), dofR, Vector3.Lerp(HeadPos(), ScreenPos(), Ease(u * 1.6f))); tc.Render(); RenderTo(rc, 1920, 1080, $"{dir}/f_{f:00000}.jpg"); }
            else
            {
                Vector3 a = T + new Vector3(-1.5f, 1.45f, -3.9f), b = T + new Vector3(1.35f, 1.62f, -3.3f), look = T + new Vector3(0.15f, 1.55f, 2.6f);
                tc.transform.position = Vector3.Lerp(a, b, u); tc.transform.rotation = Quaternion.LookRotation(look - tc.transform.position); tc.fieldOfView = Mathf.Lerp(46f, 41f, u);
                RenderTo(tc, 1920, 1080, $"{dir}/f_{f:00000}.jpg");
            }
        }
        foreach (var s in sways) { s.t.position = s.pos; s.t.rotation = s.rot; }
        rc.transform.position = rp0; rc.transform.rotation = rr0; rc.fieldOfView = rf0; tc.transform.position = tp0; tc.transform.rotation = tr0; tc.fieldOfView = tf0;
        if (dofR != null) dofR.focusDistance.value = fr0; if (glow != null) glow.intensity = g0;
        PoseAt(0f);
        return $"shot {which}: {frames} frames in {sw.Elapsed.TotalSeconds:F1} s ({frames / sw.Elapsed.TotalSeconds:F1} fps)";
    }
}
