    // ---------------------------------------------------------------- what the tube shows of the room, and the bare bulb
    /// (room scene) the room as the television sees it: from the middle of the screen looking out, wide, 4 : 3
    public static string RoomView()
    {
        var camGo = GameObject.Find("ShotCam"); if (camGo == null) return "no room"; var cam = camGo.GetComponent<Camera>(); var vol = Object.FindFirstObjectByType<Volume>();
        var scr = GameObject.Find("Screen"); Vector3 p = (scr != null ? scr.transform.position : new Vector3(-0.35f, 0.80f, 0.90f)) + new Vector3(0, 0, -0.06f);
        var keepS = cam.sensorSize; Vector3 kp = cam.transform.position; Quaternion kr = cam.transform.rotation; float kf = cam.focalLength;
        cam.sensorSize = new Vector2(36f, 27f); cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(new Vector3(0.15f, 0.12f, -1f)); cam.focalLength = 18f;
        DepthOfField dof; if (vol.profile.TryGet(out dof)) { dof.focusDistance.value = 0.35f; dof.aperture.value = 1.4f; dof.focalLength.value = 50f; }                 // out of focus on purpose: the room is only a blur beyond the glass
        if (scr != null) scr.GetComponent<Renderer>().enabled = false; var glass = GameObject.Find("ScreenGlass"); if (glass != null) glass.GetComponent<Renderer>().enabled = false;
        RenderTo(cam, 1440, 1080, GEN + "/room_view.png");
        if (scr != null) scr.GetComponent<Renderer>().enabled = true; if (glass != null) glass.GetComponent<Renderer>().enabled = true;
        cam.sensorSize = keepS; cam.transform.position = kp; cam.transform.rotation = kr; cam.focalLength = kf; AssetDatabase.ImportAsset(GEN + "/room_view.png");
        return "wrote " + GEN + "/room_view.png";
    }

    /// (stage scene) put that picture behind the tube's glass; it glows a little, mirrored the way one sees a room from inside a screen
    static void TubeViewFill(Transform stage)
    {
        var pic = AssetDatabase.LoadAssetAtPath<Texture2D>(GEN + "/room_view.png"); if (pic == null) return;
        foreach (var r in stage.GetComponentsInChildren<MeshRenderer>(true)) if (r.name == "TubeView")
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetTexture("_BaseMap", pic); m.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.6f, 1)); r.sharedMaterial = Save(m, "tube_view_room");
        }
    }

    /// the bare bulb of the struck stage: a real glass bulb in a pull-chain socket (Poly Haven, CC0) on a cord from the second batten
    static void BareBulbProp(Transform root)
    {
        var g = new GameObject("BareBulbProp").transform; g.SetParent(root, false); Vector3 at = new Vector3(-0.5f, 2.3f, 1.9f);
        Rod("bulb_cord", g, at + Vector3.up * 0.10f, new Vector3(at.x, BATTEN, 1.8f), 0.004f, CardM("gap", new Color(0.03f, 0.03f, 0.04f)));
        var so = Prop("pull_chain_light_socket", "pull_chain_light_socket_2k.gltf", "BulbSocket", g, at + Vector3.up * 0.02f, 0);
        var bu = Prop("lightbulb_01", "lightbulb_01_2k.gltf", "Bulb", g, at + Vector3.down * 0.10f, 0);
        foreach (var o in new[] { so, bu }) if (o != null) foreach (var r in o.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        Ball("bulb_glow", g, at + Vector3.down * 0.05f, 0.016f, Emit("stage_bulb_glow", new Color(1f, 0.80f, 0.55f) * 6f)).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        g.gameObject.SetActive(false);
    }
