    // ---------------------------------------------------------------- stills (edit mode)
    static void RenderTo(Camera cam, int w, int h, string path)
    {
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

    /// pose the stage at a film time and write a still; shot 16 or 17, or 0 for the shot that time belongs to
    public static string Still(float t, int shot, string name)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null) return "no stage in the open scene";
        foreach (var rp in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) rp.RenderProbe();
        if (ck.singer != null) ck.singer.Apply(t);
        ck.Apply(t, shot);
        if (ck.singer != null) ck.singer.Apply(t);                               // the gaze target rides on the camera, which has just moved
        RenderTo(ck.cam, 1920, 1080, "Capture/" + name + ".jpg");
        return "wrote Capture/" + name + ".jpg at t " + t;
    }

    /// free camera still for checking a pose from the side
    public static string View(float t, float yaw, float dist, float height, float focal, string name)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null || ck.singer == null) return "no stage";
        ck.singer.Apply(t); ck.Apply(t, 21); ck.singer.Apply(t);
        var cam = ck.cam; Vector3 c = ck.singer.transform.position + Vector3.up * height;
        Vector3 p = c + Quaternion.Euler(0, yaw, 0) * Vector3.back * dist;
        cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(c - p); cam.focalLength = focal;
        DepthOfField dof; if (ck.volume.profile.TryGet(out dof)) { dof.focusDistance.value = dist; dof.aperture.value = 11f; dof.focalLength.value = focal; }
        RenderTo(cam, 1920, 1080, "Capture/" + name + ".jpg");
        return "wrote Capture/" + name + ".jpg";
    }

    /// real-mesh clearance: the nearest any vertex of the right hand and forearm comes to the torso, head and skirt at film time t (metres)
    public static string Clearance(float t)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null || ck.singer == null) return "no stage";
        var sp = ck.singer; sp.Apply(t);
        var an = sp.animator; var armBones = new HashSet<Transform>();
        foreach (var b in new[] { HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand }) foreach (var tr in an.GetBoneTransform(b).GetComponentsInChildren<Transform>()) armBones.Add(tr);
        var upperArms = new HashSet<Transform>();
        foreach (var b in new[] { HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm }) { var x = an.GetBoneTransform(b); if (x != null) foreach (var tr in x.GetComponentsInChildren<Transform>()) upperArms.Add(tr); }
        var arm = new List<Vector3>(); var body = new List<Vector3>();
        foreach (var smr in sp.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var m = new Mesh(); smr.BakeMesh(m, true); var v = m.vertices; var bw = smr.sharedMesh.boneWeights; var bones = smr.bones; var l2w = smr.transform.localToWorldMatrix;
            for (int i = 0; i < v.Length; i++)
            {
                var tb = bones[bw[i].boneIndex0]; Vector3 wpos = l2w.MultiplyPoint3x4(v[i]);
                if (armBones.Contains(tb)) arm.Add(wpos); else if (!upperArms.Contains(tb)) body.Add(wpos);
            }
            Object.DestroyImmediate(m);
        }
        float best = 1e9f; Vector3 pa = Vector3.zero, pb = Vector3.zero;
        foreach (var a in arm) foreach (var b in body) { float d = (a - b).sqrMagnitude; if (d < best) { best = d; pa = a; pb = b; } }
        return $"t {t}: right hand and forearm vertices {arm.Count}, body vertices {body.Count}, nearest distance {Mathf.Sqrt(best) * 100f:F1} cm (arm point {pa:F3}, body point {pb:F3})";
    }

    /// Is anything in frame that was never built? Renders shot `shot` at film time t against a magenta void with the finish switched off
    /// and counts the magenta pixels (nothing there at all) and the near-black ones (there, but no light reaches it).
    public static string Cover(float t, int shot, string name)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null) return "no stage";
        if (shot >= 0) { if (ck.singer != null) ck.singer.Apply(t); ck.Apply(t, shot); }          // shot -1: the camera as Free() left it
        var cam = ck.cam; var ad = cam.GetComponent<UniversalAdditionalCameraData>(); var keepC = cam.backgroundColor; bool keepP = ad.renderPostProcessing;
        cam.backgroundColor = new Color(1, 0, 1); ad.renderPostProcessing = false;
        string path = "Capture/cover/" + name + ".png"; RenderTo(cam, 480, 270, path);
        cam.backgroundColor = keepC; ad.renderPostProcessing = keepP;
        var tex = new Texture2D(2, 2); tex.LoadImage(File.ReadAllBytes(path)); var px = tex.GetPixels32(); int voidN = 0, dark = 0;
        foreach (var c in px) { if (c.r > 200 && c.b > 200 && c.g < 70) voidN++; else if (c.r < 6 && c.g < 6 && c.b < 8) dark++; }
        Object.DestroyImmediate(tex);
        Vector3 p = cam.transform.position, f = cam.transform.forward;
        bool houseSide = p.z < 2.2f && p.y < 2.9f && Mathf.Abs(p.x) < 2.0f && f.z > 0.80f;
        return $"shot {shot} t {t:F2}: void {100f * voidN / px.Length:F1}%  unlit {100f * dark / px.Length:F1}%  camera {p:F2} forward {f:F2}  house side {houseSide}";
    }

    // ---------------------------------------------------------------- the play-mode pass
    public static string SetupBaker(int warmup)
    {
        foreach (var old in Object.FindObjectsByType<SliceBaker>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null || ck.singer == null) return "no stage";
        var g = new GameObject("SliceBaker"); var sb = g.AddComponent<SliceBaker>(); sb.singer = ck.singer; sb.clock = ck; sb.cam = ck.cam; sb.warmup = warmup;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return "baker ready";
    }

    public static string Progress() { return $"playing {EditorApplication.isPlaying} written {SliceBaker.Written} done {SliceBaker.Done} {SliceBaker.Note}"; }
