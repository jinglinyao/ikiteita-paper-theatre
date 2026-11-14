    public static float LIN_TONE = 0.74f;
    public static string Build()
    {
        log = new StringBuilder(); rnd = new System.Random(20261113); kitM.Clear();
        AssetDatabase.Refresh();
        var silAspect = new Dictionary<string, float>(); var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var ln in File.ReadAllLines(ART + "stage_art.txt")) { var q = ln.Split(' '); if (q[0] == "sil") silAspect[q[1]] = float.Parse(q[2], inv); }
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MV/Characters/lin.vrm") == null) { AssetDatabase.ImportAsset("Assets/MV/Characters/lin.vrm", ImportAssetOptions.ForceUpdate); }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Pipeline(); Ssao(1.6f, 0.22f);
        RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.020f, 0.028f, 0.055f);
        RenderSettings.fog = false; RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        var root = new GameObject("Stage").transform;

        StageBox(root);
        var bulbs = new List<Renderer>(); Proscenium(root, bulbs);
        // sky cloth: a crisp sheet with pin-prick stars, lit from behind; cut-paper clouds hang in front of it on strings
        var skyT = Tex(ART + "sky_crisp.png", false, false, false, false, 8192);
        var skyR = Cyc(root, skyT);
        var farPanes = new List<Renderer>(); CloudsBuilt(root); FarRowBuilt(root, 4.2f, farPanes); SunBuilt(root); PaperCurtain(root); StreetProps(root); Lanterns(root);
        float[] wz = { 0.35f, 1.20f, 1.80f };
        for (int k = 0; k < 3; k++) { WingFlat(root, "WingR" + k, new Vector3(2.10f, 0, wz[k] + 0.15f), 20f, k); WingFlat(root, "WingL" + k, new Vector3(-2.93f, 0, wz[k] - 0.15f), -20f, k + 1); }
        Border(root, 0, 0.45f); Border(root, 1, 1.50f); Border(root, 2, 2.45f);
        var panes = new List<Renderer>(); var plights = new List<Light>(); var dummyP = new List<Renderer>(); var dummyL = new List<Light>();
        Transform front, frontL;
        House(root, "HouseR", new Vector3(0.50f, 0.04f, 3.0f), 0, panes, plights, silAspect, out front);
        House(root, "HouseL", new Vector3(-2.47f, 0.04f, 3.15f), 1, dummyP, dummyL, silAspect, out frontL);
        LampPost(root, new Vector3(0.05f, 0, 2.32f));
        Street(root);

        StageLights(root); Tube(root); TubeViewFill(root); BareBulbProp(root); Props(root); Crowd(root);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MV/Characters/lin.vrm");
        StagePerformer sp = null;
        if (prefab == null) log.AppendLine("lin.vrm did not import as a model");
        else
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = "Lin"; go.transform.position = MARK; go.transform.rotation = Quaternion.Euler(0, 202, 0);
            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var r in smrs) { r.forceMatrixRecalculationPerRender = true; r.updateWhenOffscreen = true; }
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var ms = r.sharedMaterials;
                for (int k = 0; k < ms.Length; k++)
                {
                    if (ms[k] == null) continue; var m2 = new Material(ms[k]);
                    foreach (var pn in new[] { "_Color", "_BaseColor", "_ShadeColor" }) if (m2.HasProperty(pn)) { Color c = m2.GetColor(pn); m2.SetColor(pn, new Color(c.r * LIN_TONE, c.g * LIN_TONE * 0.97f, c.b * LIN_TONE * 0.95f, c.a)); }
                    if (m2.HasProperty("_EmissionColor")) m2.SetColor("_EmissionColor", m2.GetColor("_EmissionColor") * 0.5f);
                    ms[k] = Save(m2, "lin_" + k + "_" + ms[k].name.Replace(" ", "_").Replace("(", "").Replace(")", ""));
                }
                r.sharedMaterials = ms;
            }
            sp = go.AddComponent<StagePerformer>();
            sp.animator = go.GetComponent<Animator>();
            sp.face = smrs.FirstOrDefault(x => x.sharedMesh.GetBlendShapeIndex("Fcl_MTH_A") >= 0);
            sp.lipJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/MV/Data/lipsync_song2_vroid.json");
            sp.CaptureRest(); sp.pose = 1; sp.poseStart = 55.24f;
        }

        var prof = Post("post_stage");
        var cg = new GameObject("ShotCam"); var cam = cg.AddComponent<Camera>();
        cam.usePhysicalProperties = true; cam.sensorSize = new Vector2(36f, 20.25f); cam.gateFit = Camera.GateFitMode.Horizontal; cam.focalLength = 70f;
        cam.nearClipPlane = 0.05f; cam.farClipPlane = 60; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.004f, 0.005f, 0.010f); cam.allowHDR = true; cam.allowMSAA = true;
        var ad = cg.AddComponent<UniversalAdditionalCameraData>(); ad.renderPostProcessing = true; ad.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; ad.renderShadows = true; ad.stopNaN = true;
        var vg = new GameObject("PostVolume"); var vol = vg.AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = prof;
        var gz = new GameObject("GazeTarget"); gz.transform.SetParent(cg.transform, false);
        if (sp != null) sp.gazeTarget = gz.transform;

        var rpg = new GameObject("StageReflection"); rpg.transform.position = new Vector3(0, 1.2f, 1.6f);
        var rp = rpg.AddComponent<ReflectionProbe>(); rp.mode = ReflectionProbeMode.Realtime; rp.refreshMode = ReflectionProbeRefreshMode.EveryFrame; rp.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        rp.resolution = 256; rp.size = new Vector3(7f, 6f, 10f); rp.boxProjection = true; rp.hdr = true; rp.clearFlags = ReflectionProbeClearFlags.SolidColor; rp.backgroundColor = Color.black; rp.intensity = 1f;

        var ck = new GameObject("StageClock").AddComponent<StageClock>();
        ck.panes = panes.ToArray(); ck.paneLights = plights.ToArray(); ck.paneOn = PANE_ON; ck.cam = cam; ck.volume = vol; ck.singer = sp; ck.nearR = front; ck.closePane = 4;
        var film = ck.gameObject.AddComponent<FilmClock>(); film.singer = sp; film.mark = MARK;
        if (sp != null) sp.Apply(57.0f);
        ck.Apply(57.0f);
        int parts = root.GetComponentsInChildren<MeshRenderer>().Length;
        log.AppendLine($"parts {parts}, lit windows {panes.Count}");
        EnsureDir("Assets/Scenes"); EditorSceneManager.SaveScene(scene, "Assets/Scenes/Stage.unity"); AssetDatabase.SaveAssets();
        log.AppendLine("scene saved: Assets/Scenes/Stage.unity");
        return log.ToString();
    }

