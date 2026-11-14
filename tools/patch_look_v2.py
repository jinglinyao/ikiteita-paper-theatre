p = r'E:/sucai/ikiteita/unity/AgentScripts/Look.cs'; s = open(p, encoding='utf-8').read()
def rep(a, b):
    global s
    assert a in s, a[:90]
    s = s.replace(a, b, 1)

# ---- A. a real window: the wall has a hole, the paper city stands outside it
a = s.index('        Quad("wall_back", root, new Vector3(0, 1.3f, 1.9f), new Vector2(4.6f, 2.6f), wall);')
b = s.index('        Quad("wall_left", root')
s = s[:a] + r'''        // back wall in four pieces round the window opening (x 0.55 +- 0.75, y 0.95 .. 2.0); each piece keeps the plaster at the same scale
        const float WX = 0.55f, WW = 1.5f, WY0 = 0.95f, WY1 = 2.0f, WZ = 1.9f;
        void WallPiece(string n, float x0, float x1, float y0, float y1)
        {
            var m = new Material(wall); m.SetTextureScale("_BaseMap", new Vector2((x1 - x0) / 2.3f, (y1 - y0) / 2.0f)); m.SetTextureOffset("_BaseMap", new Vector2(x0 / 2.3f, y0 / 2.0f));
            Quad(n, root, new Vector3((x0 + x1) / 2, (y0 + y1) / 2, WZ), new Vector2(x1 - x0, y1 - y0), Save(m, "wall_" + n));
        }
        WallPiece("back_l", -2.3f, WX - WW / 2, 0, 2.6f); WallPiece("back_r", WX + WW / 2, 2.3f, 0, 2.6f);
        WallPiece("back_below", WX - WW / 2, WX + WW / 2, 0, WY0); WallPiece("back_above", WX - WW / 2, WX + WW / 2, WY1, 2.6f);
''' + s[b:]
a = s.index('        // window in the back wall: night outside is a paper skyline too')
b = s.index('        string R = "Assets/Props/";')
s = s[:a] + r'''        // the window: reveals (the wall's thickness), a wooden frame set into it, a sill; outside, the night is cut paper standing well back from the glass
        string P = "Assets/Paper/street/";
        var win = new GameObject("window").transform; win.SetParent(root, false); win.localPosition = new Vector3(WX, (WY0 + WY1) / 2, WZ);
        float wh = WY1 - WY0; var plaster = Lit("reveal", null, null, null, Vector2.one, new Color(.62f, .58f, .52f), 0.05f);
        Box("reveal_top", win, new Vector3(0, wh / 2 + .01f, .08f), new Vector3(WW + .04f, .02f, .16f), plaster); Box("reveal_bot", win, new Vector3(0, -wh / 2 - .01f, .08f), new Vector3(WW + .04f, .02f, .16f), plaster);
        Box("reveal_l", win, new Vector3(-WW / 2 - .01f, 0, .08f), new Vector3(.02f, wh, .16f), plaster); Box("reveal_r", win, new Vector3(WW / 2 + .01f, 0, .08f), new Vector3(.02f, wh, .16f), plaster);
        foreach (var (n, p2, s2) in new[] { ("top", new Vector3(0, wh / 2 - .025f, .09f), new Vector3(WW, .05f, .05f)), ("bot", new Vector3(0, -wh / 2 + .025f, .09f), new Vector3(WW, .05f, .05f)),
                                           ("l", new Vector3(-WW / 2 + .025f, 0, .09f), new Vector3(.05f, wh, .05f)), ("r", new Vector3(WW / 2 - .025f, 0, .09f), new Vector3(.05f, wh, .05f)),
                                           ("mid", new Vector3(0, 0, .09f), new Vector3(.04f, wh, .04f)), ("sill", new Vector3(0, -wh / 2 - .015f, -.045f), new Vector3(WW + .14f, .03f, .13f)) })
            Box("frame_" + n, win, p2, s2, dark);
        var outside = new GameObject("outside").transform; outside.SetParent(root, false);
        Quad("out_sky", outside, new Vector3(WX, 1.9f, WZ + 2.3f), new Vector2(7.0f, 4.2f), Paper("paper_sky_win", Tex(P + "sky.png", alpha: true, repeat: false), Tex(P + "sky.png", alpha: true, repeat: false), 0.9f, null), default, false);
        Quad("out_moon", outside, new Vector3(WX + 0.55f, 2.45f, WZ + 2.2f), new Vector2(.42f, .42f), Paper("paper_moon_win", Tex(P + "moon.png", alpha: true, repeat: false), Tex(P + "moon_emit.png", repeat: false), 2.0f, null), default, false);
        Quad("out_far", outside, new Vector3(WX + 0.2f, 1.42f, WZ + 1.7f), new Vector2(4.6f, 2.05f), Paper("paper_far_win", Tex(P + "far_buildings.png", alpha: true, repeat: false), Tex(P + "far_buildings_emit.png", repeat: false), 1.5f, null), default, false);
        Quad("out_near", outside, new Vector3(WX - 0.75f, 0.62f, WZ + 0.9f), new Vector2(1.65f, 2.3f), Paper("paper_near_win", Tex(P + "block_R.png", alpha: true, repeat: false), Tex(P + "block_R_emit.png", repeat: false), 1.1f, null), default, false);

''' + s[b:]

# ---- B. furniture: her chair faces the television, 1.25 m in front of the screen
rep('Prop(R + "schoolchair_01/SchoolChair_01_2k.gltf", "Chair", root, new Vector3(1.25f, 0, 0.85f), 160);',
    'Prop(R + "schoolchair_01/SchoolChair_01_2k.gltf", "SeatChair", root, SEAT, SEAT_YAW + CHAIR_YAW_OFFSET, CHAIR_SCALE);')
rep('Prop(R + "sf_zabuton/scene.gltf", "Zabuton", root, new Vector3(-0.55f, 0, 0.15f), 20, 0.22f);',
    'Prop(R + "sf_zabuton/scene.gltf", "Zabuton", root, new Vector3(-0.95f, 0, -0.55f), 35, 0.22f);')
rep('    static Transform tvScreen;', r'''    static Transform tvScreen;
    // where she sits: on the chair, facing the screen (the screen is at about (-1.42, 0.80, 0.63) and faces (0.74, 0, -0.67))
    static readonly Vector3 TVF = new Vector3(0.74f, 0, -0.67f).normalized;
    static readonly Vector3 SEAT = new Vector3(-1.42f, 0, 0.63f) + new Vector3(0.74f, 0, -0.67f).normalized * 1.30f;
    static readonly float SEAT_YAW = Mathf.Atan2(-0.74f, 0.67f) * Mathf.Rad2Deg;      // her yaw when she faces the screen
    const float CHAIR_YAW_OFFSET = 180f, CHAIR_SCALE = 0.92f, SEAT_BACK = 0.10f;''')
rep('AddLight("RoomFill", root, LightType.Point, new Vector3(0.0f, 2.2f, 0.2f), Vector3.zero, new Color(0.80f, 0.66f, 0.56f), 0.42f, 6f, 0, false);',
    'AddLight("RoomFill", root, LightType.Point, new Vector3(-0.2f, 2.2f, 0.0f), Vector3.zero, new Color(0.78f, 0.66f, 0.58f), 0.50f, 6f, 0, false);')
rep('AddLight("WindowNight", root, LightType.Spot, new Vector3(0.55f, 1.55f, 1.78f), new Vector3(0.0f, 0.2f, -0.4f), new Color(0.40f, 0.54f, 0.98f), 5.5f, 6f, 112f);',
    '// moonlight: a lamp far outside, so the window frame draws its cross on the floor\n        AddLight("Moonlight", root, LightType.Spot, new Vector3(2.35f, 3.55f, 4.6f), new Vector3(-0.15f, 0f, 0.35f), new Color(0.46f, 0.60f, 1f), 520f, 14f, 30f);')

# ---- C. the singer, seated
a = s.index('            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = "Jinglin";')
b = s.index('        } else log.AppendLine("MISSING jinglin prefab");')
s = s[:a] + r'''            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = "Jinglin"; go.transform.position = SEAT - TVF * SEAT_BACK;
            go.transform.rotation = Quaternion.Euler(0, SEAT_YAW, 0);
            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            var anim = go.GetComponent<Animator>(); var face = smrs.OrderByDescending(x => x.sharedMesh.blendShapeCount).First();
            var d = go.AddComponent<SingDriver>(); d.animator = anim; d.face = face; d.audioSource = go.AddComponent<AudioSource>();
            d.lipJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/MV/Data/lipsync_24_47.json"); d.mouth = 0f;
            d.stageOrigin = go.transform.position; d.CaptureRest(); d.plantFeet = false; d.bodyClips = new AnimationClip[0]; d.bodyStart = new float[0];
            // hands folded in her lap; a little after two seconds one hand comes up to her chest, later to her throat
            d.restPose = "folded"; d.gestureNames = new[] { "chest", "throat" }; d.gestureStart = new[] { 2.3f, 6.2f }; d.gestureDur = new[] { 3.4f, 3.0f };
            try { PoseAt(0f); log.AppendLine("jinglin seated; " + lastSeatNote); } catch (System.Exception e) { log.AppendLine("pose failed: " + e.Message + " | " + e.StackTrace.Split('\n')[0]); }
''' + s[b:]

# ---- street tweaks
rep('new Color(0.62f, 0.72f, 1f), 58f, 16f, 70f);', 'new Color(0.86f, 0.86f, 0.92f), 36f, 16f, 70f);')
rep('Quad("ground", root, new Vector3(0, 0, 4.0f), new Vector2(12f, 10f), PM("ground"), new Vector3(90, 0, 0), false);',
    'var gm = Paper("paper_ground", Tex(P + "ground.png", alpha: true, repeat: false), null, 0, Tex("Assets/Textures/tex_asphalt_02/asphalt_02_nor_gl_2k.jpg", normal: true)); gm.SetFloat("_DetailNormalMapScale", 1.1f);\n        Quad("ground", root, new Vector3(0, 0, 4.0f), new Vector2(12f, 10f), gm, new Vector3(90, 0, 0), false);')
rep('Quad("puppet_jinglin", root, new Vector3(0.95f, ph / 2 + 0.01f, 2.05f), new Vector2(pw, ph), pm, new Vector3(0, 12, 0));',
    'Quad("puppet_jinglin", root, new Vector3(0.80f, ph / 2 + 0.01f, 1.95f), new Vector2(pw, ph), pm, new Vector3(0, 10, 0));')
rep('Box("puppet_rod", root, new Vector3(0.97f, 0.55f, 2.09f), new Vector3(.018f, 1.1f, .018f), rod);', 'Box("puppet_rod", root, new Vector3(0.82f, 0.55f, 2.0f), new Vector3(.018f, 1.1f, .018f), rod);')
rep('float ph = 1.62f, pw', 'float ph = 1.72f, pw')

# ---- room camera default = wide establishing view
rep('var rc = Cam("RoomCam", new Vector3(0.95f, 1.22f, -1.95f), new Vector3(-0.12f, 0.95f, 0.9f), 42f, pRoom, 1 << 6);',
    'var rc = Cam("RoomCam", new Vector3(1.55f, 1.35f, -1.85f), new Vector3(-0.35f, 0.92f, 0.75f), 44f, pRoom, 1 << 6);')

# ---- D/E. everything from the capture section on is rewritten
a = s.index('    // ---------------------------------------------------------------- capture')
s = s[:a] + open(r'E:/sucai/ikiteita/tools/look_tail_v2.cs', encoding='utf-8').read()
open(p, 'w', encoding='utf-8').write(s); print('patched', len(s))
