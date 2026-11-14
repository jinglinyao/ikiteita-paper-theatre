    // ---------------------------------------------------------------- C the room and D the box outside its window (Room.unity)
    static Bounds BoundsOf(GameObject g) { var rs = g.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }

    /// a downloaded model, standing on `at` (its footprint centred there), turned by yaw; longZ = true turns it so that its long side runs along z
    static GameObject Prop(string id, string file, string name, Transform parent, Vector3 at, float yaw, float scale = 1f, int longZ = -1)
    {
        string path = "Assets/Props/" + id + "/" + file; var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); }
        if (prefab == null) { log.AppendLine("MISSING model " + path); return null; }
        var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab); g.name = name; g.transform.SetParent(parent, false); g.transform.localScale = Vector3.one * scale; g.transform.localEulerAngles = new Vector3(0, yaw, 0); g.transform.position = Vector3.zero;
        var b = BoundsOf(g);
        if (longZ >= 0 && (b.size.x > b.size.z) == (longZ == 1)) { g.transform.localEulerAngles = new Vector3(0, yaw + 90, 0); b = BoundsOf(g); }
        g.transform.position = parent.TransformPoint(at) - new Vector3(b.center.x, b.min.y, b.center.z);
        log.AppendLine($"  {name}: {b.size.x * 1000:F0} x {b.size.y * 1000:F0} x {b.size.z * 1000:F0} mm at {at}");
        return g;
    }

    /// a sheet hanging in folds: width along x, the folds a sine of the given wavelength and depth, one segment every 10 mm
    static GameObject Wavy(string name, Transform parent, Vector3 at, float width, float height, float wave, float depth, Material mat)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = at;
        int n = Mathf.CeilToInt(width / 0.01f); var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i <= n; i++) { float x = width * i / n, z = Mathf.Sin(x / wave * Mathf.PI * 2f) * depth * 0.5f; v.Add(new Vector3(x, 0, z)); v.Add(new Vector3(x, -height, z * 1.15f)); uv.Add(new Vector2(x / 0.3f, 0)); uv.Add(new Vector2(x / 0.3f, height / 0.3f)); }
        for (int i = 0; i < n; i++) { int a = i * 2; t.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); }
        var mesh = new Mesh { name = name }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.TwoSided; return g;
    }

    const float RX = 1.82f, RZ = 1.365f, RH = 2.40f, WT = 0.16f;                                   // half width, half depth, height of the room; wall thickness
    const float WIN_Z0 = -0.835f, WIN_Z1 = 0.815f, WIN_Y0 = 0.85f, WIN_Y1 = 2.00f;                 // the window in the left wall
    const float DX0 = -5.40f, DX1 = -1.98f, DY0 = -1.5f, DY1 = 3.0f, DZ = 2.5f;                   // the box outside

    public static string BuildRoom()
    {
        log = new StringBuilder(); rnd = new System.Random(20261114); kitM.Clear(); AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Pipeline(); Ssao(1.2f, 0.25f);
        RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.050f, 0.044f, 0.078f);                 // the night in the room is a grey violet, not black RenderSettings.fog = false; RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        var root = new GameObject("Room").transform;
        var wall = Lit("room_wall", Tex("Assets/Textures/tex_beige_wall_001/beige_wall_001_diff_2k.jpg"), Tex("Assets/Textures/tex_beige_wall_001/beige_wall_001_nor_gl_2k.jpg", true), new Vector2(0.3f, 0.3f), new Color(0.86f, 0.82f, 0.76f), 0.10f);
        var floorM = Lit("room_floor", Tex("Assets/Textures/tex_laminate_floor/laminate_floor_diff_2k.jpg"), Tex("Assets/Textures/tex_laminate_floor/laminate_floor_nor_gl_2k.jpg", true), new Vector2(0.3f, 0.3f), new Color(0.80f, 0.72f, 0.64f), 0.55f);
        var white = Lit("room_white", null, null, Vector2.one, new Color(0.88f, 0.86f, 0.82f), 0.25f); var darkwood = Lit("room_darkwood", Tex("Assets/Textures/tex_plywood/plywood_diff_2k.jpg"), null, new Vector2(0.5f, 0.5f), new Color(0.30f, 0.20f, 0.14f), 0.35f);
        var brass = Lit("room_brass", null, null, Vector2.one, new Color(0.72f, 0.56f, 0.26f), 0.6f); var plastic = Lit("room_plastic", null, null, Vector2.one, new Color(0.90f, 0.88f, 0.82f), 0.45f); var black = Lit("room_black", null, null, Vector2.one, new Color(0.03f, 0.03f, 0.035f), 0.4f);

        // ---- the shell: floor, ceiling, four walls 160 thick; the left wall has the window, the front wall the door
        var sh = new GameObject("Shell").transform; sh.SetParent(root, false);
        Box("floor", sh, new Vector3(0, -0.05f, 0), new Vector3(2 * RX + 2 * WT, 0.10f, 2 * RZ + 2 * WT), floorM, default, 0f);
        Box("ceiling", sh, new Vector3(0, RH + 0.05f, 0), new Vector3(2 * RX + 2 * WT, 0.10f, 2 * RZ + 2 * WT), white, default, 0f);
        Box("wall_back", sh, new Vector3(0, RH * 0.5f, RZ + WT * 0.5f), new Vector3(2 * RX + 2 * WT, RH, WT), wall, default, 0f);
        Box("wall_right", sh, new Vector3(RX + WT * 0.5f, RH * 0.5f, 0), new Vector3(WT, RH, 2 * RZ), wall, default, 0f);
        var wl = WallFrame(sh, "WallLeft", new Vector3(-RX, 0, -RZ), -90); var winR = Rect.MinMaxRect(WIN_Z0 + RZ, WIN_Y0, WIN_Z1 + RZ, WIN_Y1);
        Wall(wl, 0, 0, 2 * RZ, RH, 0, WT, new List<Rect> { winR }, wall);
        var wf = WallFrame(sh, "WallFront", new Vector3(RX + WT, 0, -RZ), 180); var doorR = new Rect(RX + WT + 0.70f, 0f, 0.80f, 2.0f);                     // door: x -1500 .. -700
        Wall(wf, 0, 0, 2 * RX + 2 * WT, RH, 0, WT, new List<Rect> { doorR }, wall);
        // skirting 80 x 12 and cornice 50 x 50 on every wall
        Box("skirt_back", sh, new Vector3(0, 0.04f, RZ - 0.006f), new Vector3(2 * RX, 0.08f, 0.012f), white); Box("skirt_right", sh, new Vector3(RX - 0.006f, 0.04f, 0), new Vector3(0.012f, 0.08f, 2 * RZ), white);
        Box("skirt_left", sh, new Vector3(-RX + 0.006f, 0.04f, 0), new Vector3(0.012f, 0.08f, 2 * RZ), white); Box("skirt_front_a", sh, new Vector3((-0.70f + RX) * 0.5f, 0.04f, -RZ + 0.006f), new Vector3(RX + 0.70f, 0.08f, 0.012f), white); Box("skirt_front_b", sh, new Vector3((-RX - 1.50f) * 0.5f, 0.04f, -RZ + 0.006f), new Vector3(RX - 1.50f, 0.08f, 0.012f), white);
        foreach (float sz in new[] { -1f, 1f }) Box("cornice_z", sh, new Vector3(0, RH - 0.025f, sz * (RZ - 0.025f)), new Vector3(2 * RX, 0.05f, 0.05f), white); foreach (float sx in new[] { -1f, 1f }) Box("cornice_x", sh, new Vector3(sx * (RX - 0.025f), RH - 0.025f, 0), new Vector3(0.05f, 0.05f, 2 * RZ - 0.1f), white);

        // ---- the window: frame 50 x 50 in the middle of the wall's thickness, one upright and one cross bar 40 x 40, glass 4, a sill board
        float wxc = winR.center.x, wyc = winR.center.y, ww = winR.width, wh = winR.height;
        Box("win_top", wl, new Vector3(wxc, winR.yMax - 0.025f, 0.08f), new Vector3(ww, 0.05f, 0.05f), white); Box("win_bot", wl, new Vector3(wxc, winR.yMin + 0.025f, 0.08f), new Vector3(ww, 0.05f, 0.05f), white);
        Box("win_l", wl, new Vector3(winR.xMin + 0.025f, wyc, 0.08f), new Vector3(0.05f, wh - 0.1f, 0.05f), white); Box("win_r", wl, new Vector3(winR.xMax - 0.025f, wyc, 0.08f), new Vector3(0.05f, wh - 0.1f, 0.05f), white);
        Box("win_mullion", wl, new Vector3(wxc, wyc, 0.08f), new Vector3(0.04f, wh - 0.1f, 0.04f), white); Box("win_transom", wl, new Vector3(wxc, winR.yMin + wh * 0.62f, 0.08f), new Vector3(ww - 0.1f, 0.04f, 0.04f), white);
        Box("win_glass", wl, new Vector3(wxc, wyc, 0.08f), new Vector3(ww - 0.1f, wh - 0.1f, 0.004f), Glass("room_glass", new Color(0.03f, 0.04f, 0.05f), 0.80f, 0.10f)).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        { var gm = AssetDatabase.LoadAssetAtPath<Material>(GEN + "/room_glass.mat"); if (gm != null) { gm.SetFloat("_SpecularHighlights", 0f); gm.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); EditorUtility.SetDirty(gm); } }      // no hot spot of a lamp on the pane
        Box("win_sill", wl, new Vector3(wxc, winR.yMin - 0.015f, -0.055f), new Vector3(ww + 0.10f, 0.03f, 0.15f), white);
        // the sheer curtain, drawn and bunched at the end of the window nearer the television wall; its rod and two brackets
        var sheer = new Material(Shader.Find("Universal Render Pipeline/Lit")); sheer.SetColor("_BaseColor", new Color(0.86f, 0.84f, 0.78f)); sheer.SetFloat("_Smoothness", 0.1f); sheer.SetFloat("_Cull", 0); sheer = Save(sheer, "room_sheer");
        Wavy("curtain", wl, new Vector3(winR.xMin - 0.08f, 2.14f, -0.09f), 0.42f, 1.42f, 0.12f, 0.04f, sheer);                 // at the end of the window away from the television wall: the moon comes in past it and reaches the plant
        Rod("curtain_rod", wl, new Vector3(winR.xMin - 0.15f, 2.15f, -0.09f), new Vector3(winR.xMax + 0.15f, 2.15f, -0.09f), 0.0125f, brass); foreach (float bx in new[] { winR.xMin - 0.10f, winR.xMax + 0.10f }) Box("rod_bracket", wl, new Vector3(bx, 2.15f, -0.045f), new Vector3(0.02f, 0.03f, 0.09f), brass);
        for (int k = 0; k < 7; k++) Cyl("curtain_ring", wl, new Vector3(winR.xMin - 0.06f + 0.06f * k, 2.15f, -0.09f), 0.02f, 0.006f, brass, new Vector3(0, 0, 90));

        // ---- the door (closed): leaf 40 thick set in the opening, two raised panels, architrave 70 wide, a knob at 1000
        float dxc = doorR.center.x;
        Box("door_leaf", wf, new Vector3(dxc, 1.0f, 0.05f), new Vector3(0.80f, 2.0f, 0.04f), darkwood); Box("door_stop", wf, new Vector3(dxc, 1.0f, 0.12f), new Vector3(0.84f, 2.04f, 0.04f), black, default, 0f);
        foreach (float py in new[] { 0.55f, 1.45f }) Box("door_panel", wf, new Vector3(dxc, py, 0.026f), new Vector3(0.56f, 0.70f, 0.012f), darkwood);
        Box("arch_t", wf, new Vector3(dxc, 2.035f, -0.006f), new Vector3(0.94f, 0.07f, 0.012f), white); foreach (float sx in new[] { -1f, 1f }) Box("arch_s", wf, new Vector3(dxc + sx * 0.435f, 1.0f, -0.006f), new Vector3(0.07f, 2.0f, 0.012f), white);
        Ball("door_knob", wf, new Vector3(doorR.xMin + 0.07f, 1.0f, -0.01f), 0.028f, brass); Cyl("door_rose", wf, new Vector3(doorR.xMin + 0.07f, 1.0f, 0.02f), 0.03f, 0.012f, brass, new Vector3(90, 0, 0));
        Box("switch", wf, new Vector3(doorR.xMin - 0.14f, 1.2f, -0.0045f), new Vector3(0.07f, 0.115f, 0.009f), plastic); Box("switch_rocker", wf, new Vector3(doorR.xMin - 0.14f, 1.2f, -0.011f), new Vector3(0.03f, 0.05f, 0.006f), plastic);

        // ---- furniture (Poly Haven, CC0), placed by its measured footprint
        var fu = new GameObject("Furniture").transform; fu.SetParent(root, false);
        var stand = Prop("side_table_01", "side_table_01_2k.gltf", "TvStand", fu, new Vector3(-0.35f, 0, RZ - 0.29f), 0);
        float standTop = stand != null ? BoundsOf(stand).max.y : 0.55f;
        var tv = Prop("television_02", "television_02_4k.gltf", "TV", fu, new Vector3(-0.35f, standTop, RZ - 0.29f), 0);
        var desk = Prop("schooldesk_01", "SchoolDesk_01_2k.gltf", "Desk", fu, new Vector3(1.00f, 0, RZ - 0.30f), 180); float deskTop = desk != null ? BoundsOf(desk).max.y : 0.76f;
        var lamp = Prop("desk_lamp_arm_01", "desk_lamp_arm_01_2k.gltf", "DeskLamp", fu, new Vector3(1.20f, deskTop, RZ - 0.30f), 215, 0.70f);
        Prop("binder_notebook", "binder_notebook_4k.gltf", "Notebook", fu, new Vector3(0.80f, deskTop, RZ - 0.33f), 100, 0.8f);
        Prop("alarm_clock_01", "alarm_clock_01_4k.gltf", "Clock", fu, new Vector3(1.04f, deskTop, RZ - 0.13f), 350);      // face to the room; run DeskTouches(350) after a rebuild for the clear glass and the contact shadows
        Prop("office_notepads", "office_notepads_2k.gltf", "Notepads", fu, new Vector3(1.22f, deskTop, RZ - 0.44f), 20, 0.22f);
        var bed = Prop("vintage_day_bed", "vintage_day_bed_2k.gltf", "Bed", fu, new Vector3(RX - 0.46f, 0, -0.35f), 0, 1f, 1); float bedTop = bed != null ? BoundsOf(bed).center.y : 0.5f;
        Prop("throw_pillows_01", "throw_pillows_01_2k.gltf", "Pillows", fu, new Vector3(RX - 0.46f, bedTop + 0.02f, -0.95f), 30, 0.8f);
        var ns = Prop("painted_wooden_nightstand", "painted_wooden_nightstand_2k.gltf", "Nightstand", fu, new Vector3(0.65f, 0, -RZ + 0.29f), 180); float nsTop = ns != null ? BoundsOf(ns).max.y : 0.6f;
        Prop("book_encyclopedia_set_01", "book_encyclopedia_set_01_2k.gltf", "Books", fu, new Vector3(0.65f, nsTop, -RZ + 0.24f), 180, 0.6f);
        Prop("wicker_basket_01", "wicker_basket_01_2k.gltf", "Basket", fu, new Vector3(-0.35f, 0, -RZ + 0.30f), 0);
        // plant stands: so that the leaves reach the height of the window and throw their shadow on the television wall
        Prop("potted_plant_01", "potted_plant_01_2k.gltf", "PlantBig", fu, new Vector3(-1.15f, 0, 1.03f), 0);            // 1.35 m high, in the corner between the window and the television wall
        foreach (var (px, pz, hgt, id, file) in new[] { (-1.52f, -1.08f, 0.62f, "office_notepads", "office_notepads_2k.gltf") })
        {
            Box("plant_stand_top", fu, new Vector3(px, hgt - 0.012f, pz), new Vector3(0.30f, 0.024f, 0.30f), darkwood); foreach (float sx in new[] { -0.12f, 0.12f }) foreach (float sz in new[] { -0.12f, 0.12f }) Box("plant_stand_leg", fu, new Vector3(px + sx, (hgt - 0.024f) * 0.5f, pz + sz), new Vector3(0.03f, hgt - 0.024f, 0.03f), darkwood);
            Box("plant_stand_rail", fu, new Vector3(px, hgt * 0.35f, pz), new Vector3(0.27f, 0.02f, 0.27f), darkwood); Prop(id, file, "StandThing", fu, new Vector3(px, hgt, pz), 30, 0.2f);
        }
        Prop("hanging_picture_frame_01", "hanging_picture_frame_01_2k.gltf", "Frame1", fu, new Vector3(-0.35f, 1.22f, RZ - 0.02f), 0, 0.7f);
        Prop("hanging_picture_frame_02", "hanging_picture_frame_02_2k.gltf", "Frame2", fu, new Vector3(0.95f, 1.35f, -RZ + 0.02f), 0);
        Prop("wall_clock", "wall_clock_2k.gltf", "WallClock", fu, new Vector3(RX - 0.03f, 1.75f, 0.90f), -90);
        // a flush ceiling light (off): a base and a shallow glass dish
        Cyl("ceiling_base", fu, new Vector3(0, RH - 0.012f, -0.2f), 0.10f, 0.024f, white); Cyl("ceiling_dish", fu, new Vector3(0, RH - 0.05f, -0.2f), 0.18f, 0.055f, plastic); Cyl("ceiling_rim", fu, new Vector3(0, RH - 0.024f, -0.2f), 0.19f, 0.012f, brass);
        // a shelf over the desk with books and the small plant, as bare wall would otherwise fill the wide shot
        Box("shelf", fu, new Vector3(1.00f, 1.50f, RZ - 0.09f), new Vector3(0.80f, 0.024f, 0.18f), darkwood); foreach (float bx in new[] { 0.70f, 1.30f }) Box("shelf_bracket", fu, new Vector3(bx, 1.45f, RZ - 0.06f), new Vector3(0.02f, 0.10f, 0.12f), darkwood);
        Color[] spine = { new Color(0.70f, 0.52f, 0.20f), new Color(0.20f, 0.38f, 0.42f), new Color(0.82f, 0.78f, 0.66f), new Color(0.62f, 0.22f, 0.16f), new Color(0.24f, 0.30f, 0.50f), new Color(0.36f, 0.46f, 0.30f), new Color(0.80f, 0.62f, 0.40f), new Color(0.30f, 0.24f, 0.30f) };
        float bxp = 0.66f; for (int k = 0; k < 8; k++) { float bw = 0.028f + 0.012f * ((k * 5) % 3), bh = 0.19f + 0.03f * ((k * 3) % 4); var bk = Box("book", fu, new Vector3(bxp + bw * 0.5f, 1.512f + bh * 0.5f, RZ - 0.10f), new Vector3(bw, bh, 0.14f), Lit("room_book" + k, null, null, Vector2.one, spine[k], 0.25f)); if (k == 7) bk.transform.localEulerAngles = new Vector3(0, 0, -12); bxp += bw + 0.003f; }
        Prop("potted_plant_04", "potted_plant_04_2k.gltf", "ShelfPlant", fu, new Vector3(1.26f, 1.512f, RZ - 0.09f), 0, 0.8f);
        // the picture in the frame over the television, and a little warm light on the wall behind the desk lamp
        var posterT = AssetDatabase.LoadAssetAtPath<Texture2D>(GEN + "/room_poster.png"); if (posterT == null) { AssetDatabase.ImportAsset(GEN + "/room_poster.png"); posterT = AssetDatabase.LoadAssetAtPath<Texture2D>(GEN + "/room_poster.png"); }
        Quad("poster", fu, new Vector3(-0.35f, 1.515f, RZ - 0.034f), new Vector2(0.32f, 0.48f), Lit("room_poster", posterT, null, Vector2.one, Color.white, 0.15f), default, false);
        AddLight("LampWall", fu, LightType.Point, new Vector3(1.12f, 1.22f, RZ - 0.14f), Vector3.zero, new Color(1f, 0.70f, 0.40f), 0.45f, 0.9f, 0, false);
        // rug 1600 x 1100 x 8; socket (a tall Japanese plate) and the television's lead along the skirting
        Box("rug", fu, new Vector3(-0.30f, 0.004f, 0), new Vector3(1.60f, 0.008f, 1.10f), Lit("room_rug", Tex("Assets/Textures/paper006/Paper006_2K-JPG_Color.jpg"), Tex("Assets/Textures/paper006/Paper006_2K-JPG_NormalGL.jpg", true), new Vector2(4f, 4f), new Color(0.42f, 0.26f, 0.22f), 0.04f), default, 0f);
        Box("rug_border", fu, new Vector3(-0.30f, 0.0045f, 0), new Vector3(1.50f, 0.008f, 1.00f), Lit("room_rug2", Tex("Assets/Textures/paper006/Paper006_2K-JPG_Color.jpg"), Tex("Assets/Textures/paper006/Paper006_2K-JPG_NormalGL.jpg", true), new Vector2(4f, 4f), new Color(0.56f, 0.40f, 0.30f), 0.04f), default, 0f);
        Box("socket", fu, new Vector3(-0.85f, 0.25f, RZ - 0.0045f), new Vector3(0.07f, 0.115f, 0.009f), plastic); Rod("tv_lead_a", fu, new Vector3(-0.45f, 0.45f, RZ - 0.02f), new Vector3(-0.45f, 0.10f, RZ - 0.02f), 0.003f, black); Rod("tv_lead_b", fu, new Vector3(-0.45f, 0.10f, RZ - 0.02f), new Vector3(-0.85f, 0.10f, RZ - 0.02f), 0.003f, black); Rod("tv_lead_c", fu, new Vector3(-0.85f, 0.10f, RZ - 0.02f), new Vector3(-0.85f, 0.22f, RZ - 0.02f), 0.003f, black);

        // ---- the television's face: the picture (a frame shot from the stage), the bulged glass over it, the glow it throws
        var lights = new GameObject("Lights").transform; lights.SetParent(root, false);
        if (tv != null)
        {
            var tb = BoundsOf(tv); Vector3 sc = new Vector3(tb.center.x, tb.center.y + 0.046f, tb.min.z - 0.004f); const float SW = 0.305f, SH = 0.229f;
            var picT = AssetDatabase.LoadAssetAtPath<Texture2D>(GEN + "/tv_frame.png");
            var scr = new Material(Shader.Find("Universal Render Pipeline/Unlit")); if (picT != null) scr.SetTexture("_BaseMap", picT); scr.SetColor("_BaseColor", picT != null ? new Color(2.2f, 2.2f, 2.2f, 1) : new Color(0.5f, 0.7f, 1.4f, 1)); scr = Save(scr, "room_tv_screen");
            Quad("Screen", root, sc, new Vector2(SW, SH), scr, default, false);
            Cap("ScreenGlass", root, sc + new Vector3(0, 0, -0.002f), SW * 1.04f, SH * 1.04f, SW * 0.04f, Glass("room_tube_glass", new Color(0.02f, 0.025f, 0.03f), 0.97f, 0.16f), true);
            Spot("TvGlow", lights, sc + new Vector3(0, 0, -0.04f), sc + new Vector3(0, -0.25f, -2f), new Color(0.56f, 0.74f, 1f), 13f, 5.5f, 140f, true);
            log.AppendLine($"  screen centre {sc * 1000f} mm");
        }
        if (lamp != null) { var lb = BoundsOf(lamp); Vector3 head = new Vector3(lb.center.x - 0.14f, lb.max.y - 0.10f, lb.center.z - 0.10f); Spot("DeskLampLight", lights, head, head + new Vector3(-0.12f, -1f, -0.10f), new Color(1f, 0.72f, 0.40f), 1.7f, 3.0f, 76f, true); Ball("lamp_bulb", root, head + Vector3.up * 0.02f, 0.022f, Emit("room_bulb", new Color(1f, 0.74f, 0.42f) * 4f)); log.AppendLine($"  desk lamp {lb.size * 1000f} mm, light at {head * 1000f}"); }
        Spot("Moon", lights, new Vector3(-4.3f, 1.9f, -1.5f), new Vector3(0.30f, 1.35f, RZ), new Color(0.60f, 0.70f, 1f), 30f, 12f, 34f, true);       // the patch falls above and to the right of the television, which stays in the shade of the wall between
        foreach (float sz in new[] { -0.4f, 0.4f }) Spot("SkyFill", lights, new Vector3(-RX - 0.46f, 1.5f, sz), new Vector3(0.5f, 1.0f, sz * 2f), new Color(0.30f, 0.42f, 0.85f), 2.2f, 2.8f, 120f, false);
        Spot("OutsideMoon", lights, new Vector3(-2.4f, 2.7f, -1.9f), new Vector3(-4.55f, 0.4f, 1.3f), new Color(0.50f, 0.64f, 1f), 16f, 8f, 80f, true);       // on the house across the street, so that it is more than two lit windows in the dark
        Spot("Sun", lights, new Vector3(-4.7f, 1.38f, -1.5f), new Vector3(0f, 0.9f, RZ), new Color(1f, 0.80f, 0.56f), 420f, 12f, 30f, true, false);

        // ---- D: the box outside. Night-sky paper on five sides, a paper house across the street on the half toward the television wall
        var dbox = new GameObject("Outside").transform; dbox.SetParent(root, false);
        var skyT = Tex(ART + "sky_crisp.png", false, false, false, false, 8192);
        Material Sky(string n, float tx, float ty) { var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetTexture("_BaseMap", skyT); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.05f); m.SetFloat("_Cull", 2); m.SetTexture("_EmissionMap", skyT); m.SetColor("_EmissionColor", Color.white * 2.6f); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; foreach (var pn in new[] { "_BaseMap", "_EmissionMap" }) m.SetTextureScale(pn, new Vector2(tx, ty)); return Save(m, n); }
        float dxm = (DX0 + DX1) * 0.5f, dw = DX1 - DX0, dym = (DY0 + DY1) * 0.5f, dh = DY1 - DY0;
        Quad("SkyFar", dbox, new Vector3(DX0, dym, 0), new Vector2(2 * DZ, dh), Sky("room_sky_far", 0.8f, 0.9f), new Vector3(0, -90, 0), false);
        Quad("SkyZ1", dbox, new Vector3(dxm, dym, DZ), new Vector2(dw, dh), Sky("room_sky_side", 0.5f, 0.9f), new Vector3(0, 0, 0), false); Quad("SkyZ0", dbox, new Vector3(dxm, dym, -DZ), new Vector2(dw, dh), Sky("room_sky_side", 0.5f, 0.9f), new Vector3(0, 180, 0), false);
        Quad("SkyTop", dbox, new Vector3(dxm, DY1, 0), new Vector2(dw, 2 * DZ), Sky("room_sky_top", 0.5f, 0.8f), new Vector3(-90, 0, 0), false);
        var street = CardM("road", new Color(0.15f, 0.16f, 0.20f), 0.16f, 1); Box("out_street", dbox, new Vector3(dxm, DY0 - 0.02f, 0), new Vector3(dw + 0.2f, 0.04f, 2 * DZ + 0.2f), street, default, 0f);
        var silAspect = new Dictionary<string, float>(); var inv = System.Globalization.CultureInfo.InvariantCulture; foreach (var ln in File.ReadAllLines(ART + "stage_art.txt")) { var q = ln.Split(' '); if (q[0] == "sil") silAspect[q[1]] = float.Parse(q[2], inv); }
        var dp = new List<Renderer>(); var dl = new List<Light>(); Transform fc;
        var opp = House(dbox, "HouseOpp", new Vector3(-4.55f, -0.95f, 0.30f), 1, dp, dl, silAspect, out fc); opp.localEulerAngles = new Vector3(0, -90, 0);
        var parT = Tex(ART + "pane_washi.png", false, false, false); int lit = 0;
        foreach (var r in opp.GetComponentsInChildren<MeshRenderer>()) if (r.name.StartsWith("Pane") && (lit++ == 4 || lit == 7)) { r.sharedMaterial = Paper("room_parchment", parT, parT, Color.black, false); var mpb = new MaterialPropertyBlock(); mpb.SetColor("_EmissionColor", new Color(1f, 0.86f, 0.70f) * 1.1f); mpb.SetColor("_BaseColor", new Color(0.13f, 0.14f, 0.19f)); r.SetPropertyBlock(mpb); }
        Box("out_kerb", dbox, new Vector3(-4.2f, DY0 + 0.02f, 0), new Vector3(0.9f, 0.04f, 2 * DZ), CardM("kerb_stone", new Color(0.56f, 0.56f, 0.54f), 0.14f), default, 0f);

        // ---- camera, finish, reflections
        var prof = Post("post_room"); ColorAdjustments rca; if (prof.TryGet(out rca)) { rca.postExposure.Override(0.9f); EditorUtility.SetDirty(prof); }
        var cg = new GameObject("ShotCam"); var cam = cg.AddComponent<Camera>(); cam.usePhysicalProperties = true; cam.sensorSize = new Vector2(36f, 20.25f); cam.gateFit = Camera.GateFitMode.Horizontal; cam.focalLength = 28f;
        cam.nearClipPlane = 0.03f; cam.farClipPlane = 40; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.004f, 0.005f, 0.010f); cam.allowHDR = true; cam.allowMSAA = true;
        var ad = cg.AddComponent<UniversalAdditionalCameraData>(); ad.renderPostProcessing = true; ad.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; ad.renderShadows = true; ad.stopNaN = true;
        cg.transform.position = new Vector3(0, 1.25f, -1.085f); cg.transform.rotation = Quaternion.Euler(4, 0, 0);
        var vg = new GameObject("PostVolume"); var vol = vg.AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = prof;
        var rpg = new GameObject("RoomReflection"); rpg.transform.position = new Vector3(0, 1.2f, 0); var rp = rpg.AddComponent<ReflectionProbe>(); rp.mode = ReflectionProbeMode.Realtime; rp.refreshMode = ReflectionProbeRefreshMode.EveryFrame; rp.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        rp.resolution = 256; rp.size = new Vector3(2 * RX, RH, 2 * RZ); rp.boxProjection = true; rp.hdr = true; rp.clearFlags = ReflectionProbeClearFlags.SolidColor; rp.backgroundColor = Color.black;
        int parts = root.GetComponentsInChildren<MeshRenderer>().Length; log.AppendLine($"parts {parts}");
        EnsureDir("Assets/Scenes"); EditorSceneManager.SaveScene(scene, "Assets/Scenes/Room.unity"); AssetDatabase.SaveAssets(); log.AppendLine("scene saved: Assets/Scenes/Room.unity");
        return log.ToString();
    }

    static Material Emit(string name, Color e) { var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", new Color(1f, 0.9f, 0.7f)); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; m.SetColor("_EmissionColor", e); return Save(m, name); }

    /// night | dawn
    public static string RoomState(string name)
    {
        var room = GameObject.Find("Room"); if (room == null) return "no room";
        bool dawn = name == "dawn";
        foreach (var l in room.GetComponentsInChildren<Light>(true)) { if (l.name == "Sun") l.enabled = dawn; else if (l.name == "Moon" || l.name == "DeskLampLight") l.enabled = !dawn; else if (l.name == "SkyFill") { l.color = dawn ? new Color(0.9f, 0.72f, 0.56f) : new Color(0.30f, 0.42f, 0.85f); l.intensity = dawn ? 3.5f : 2.2f; } else if (l.name == "TvGlow") l.enabled = !dawn; }
        foreach (var r in room.GetComponentsInChildren<MeshRenderer>(true)) if (r.name.StartsWith("Sky")) { var mpb = new MaterialPropertyBlock(); Color c = dawn ? new Color(1.0f, 0.62f, 0.38f) * 1.6f : Color.white * 2.6f; mpb.SetColor("_EmissionColor", c); mpb.SetColor("_BaseColor", dawn ? new Color(1f, 0.7f, 0.5f) : Color.white); r.SetPropertyBlock(mpb); }
        var scr = GameObject.Find("Screen"); if (scr != null) scr.GetComponent<Renderer>().enabled = !dawn;
        var bulb = GameObject.Find("lamp_bulb"); if (bulb != null) bulb.GetComponent<Renderer>().enabled = !dawn;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return "room state " + name;
    }

    /// a still from the room camera (pos, look, focal), as Free() does for the stage
    public static string RoomShot(float px, float py, float pz, float lx, float ly, float lz, float focal, float fstop, string name)
    {
        var cam = GameObject.Find("ShotCam").GetComponent<Camera>(); var vol = Object.FindFirstObjectByType<Volume>();
        foreach (var rp in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) rp.RenderProbe();
        Vector3 p = new Vector3(px, py, pz), c = new Vector3(lx, ly, lz); cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(c - p); cam.focalLength = focal;
        DepthOfField dof; if (vol.profile.TryGet(out dof)) { dof.focusDistance.value = Vector3.Distance(p, c); dof.aperture.value = fstop; dof.focalLength.value = focal; }
        RenderTo(cam, 1920, 1080, "Capture/" + name + ".jpg"); return "wrote Capture/" + name + ".jpg";
    }

    /// open one of the two scenes
    public static string Open(string which)
    {
        EditorSceneManager.OpenScene(which == "room" ? "Assets/Scenes/Room.unity" : "Assets/Scenes/Stage.unity", OpenSceneMode.Single); return "opened " + which;
    }

    /// the frame the television shows: the stage through its proscenium, from the plan's television camera (40 mm on 36 x 27)
    public static string TvFrame(float t)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null) return "no stage";
        if (ck.singer != null) ck.singer.Apply(t); ck.Apply(t, 26);
        var cam = ck.cam; var keepS = cam.sensorSize; cam.sensorSize = new Vector2(36f, 27f); cam.transform.position = new Vector3(0, 1.5f, -4.444f); cam.transform.rotation = Quaternion.identity; cam.focalLength = 40f;
        DepthOfField dof; if (ck.volume.profile.TryGet(out dof)) { dof.focusDistance.value = 7.4f; dof.aperture.value = 8f; dof.focalLength.value = 40f; }
        RenderTo(cam, 1440, 1080, GEN + "/tv_frame.png"); cam.sensorSize = keepS; AssetDatabase.ImportAsset(GEN + "/tv_frame.png");
        return "wrote " + GEN + "/tv_frame.png";
    }
