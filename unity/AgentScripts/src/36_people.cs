    // ---------------------------------------------------------------- the animal-headed paper people
    /// One paper person, a jointed card puppet about 1.48 m high: a base that rides in the floor groove, two legs, a coat in layers
    /// (back sheet, two fronts, lapels, a turned-down collar, buttons, pockets), arms in two pieces on brass paper fasteners, the animal
    /// head (a generated model reduced to a few thousand flat facets) on a card neck. In its right hand a slip with a bright face; in
    /// its left a small window lamp carried by a wire handle; on its back a bracket with a stick and a blank speech card.
    /// The film clock moves the named children:  Body (leans), Head, ArmR / ForeR, ArmL / ForeL, Slip, Speech, Lamp, EyeL / EyeR (Lid)
    /// v2 after the independent review: narrower (0.47 m over the arms, so that six stand 0.56 m apart without overlapping), eyes behind
    /// the eye holes, shoes on the plate, the slip held under the thumb, the speech stick in a sleeve, a handle on the lamp, a marked back.
    static Transform PaperPerson(Transform parent, string name, string head, Color coat, Color trouser, float headYaw, int number)
    {
        var g = new GameObject(name).transform; g.SetParent(parent, false);
        var coatM = CardM("coat_" + name, coat, 0.10f, 1); var coatD = CardM("coatd_" + name, coat * 0.72f, 0.10f, 1); var legM = CardM("leg_" + name, trouser, 0.10f, 1); var shoe = CardM("shoe", new Color(0.07f, 0.06f, 0.06f), 0.25f, 1);
        var edge = CardM("coat_edge_" + name, Color.Lerp(coat, new Color(0.80f, 0.76f, 0.68f), 0.45f), 0.10f, 1);                                      // the cut edge of the card: a lighter line that keeps the layers apart in the dark
        var skin = CardM("glove", new Color(0.78f, 0.74f, 0.66f), 0.10f); var brass = CardM("brass", new Color(0.72f, 0.55f, 0.24f), 0.45f); var white = CardM("slip_white", new Color(0.90f, 0.89f, 0.85f), 0.08f); var stick = Stud;
        var pencil = CardM("pencil_grey", new Color(0.36f, 0.36f, 0.38f), 0.10f); var tapeM = CardM("masking_tape", new Color(0.82f, 0.76f, 0.60f), 0.12f);
        const float T = 0.012f;
        // base: a strip of card standing in the groove, a foot plate just above the boards
        Box("base_fin", g, new Vector3(0, -0.02f, 0), new Vector3(0.28f, 0.06f, 0.008f), stick, default, 0f); Box("base_plate", g, new Vector3(0, 0.014f, 0), new Vector3(0.30f, 0.010f, 0.12f), stick);
        var body = new GameObject("Body").transform; body.SetParent(g, false); body.localPosition = new Vector3(0, 0.02f, 0);
        foreach (float s in new[] { -1f, 1f })
        {
            Box("leg", body, new Vector3(s * 0.070f, 0.33f, 0), new Vector3(0.100f, 0.62f, T), legM); Box("leg_crease", body, new Vector3(s * 0.070f, 0.33f, -T * 0.5f - 0.001f), new Vector3(0.004f, 0.58f, 0.002f), CardM("legd_" + name, trouser * 0.7f, 0.1f, 1), default, 0f);
            Box("shoe", body, new Vector3(s * 0.073f, 0.024f, -0.03f), new Vector3(0.11f, 0.05f, 0.09f), shoe);                                          // standing on the plate (its top is 19 mm above the boards)
        }
        // coat: back sheet, two fronts that overlap, lapels, a turned-down collar of two points, three buttons, two pocket flaps, a belt
        Box("coat_back", body, new Vector3(0, 0.86f, 0.006f), new Vector3(0.36f, 0.66f, T), coatD);
        foreach (float s in new[] { -1f, 1f })
        {
            Box("coat_front", body, new Vector3(s * 0.085f + 0.010f, 0.85f, -0.008f), new Vector3(0.195f, 0.66f, T), coatM, new Vector3(0, 0, s * 1.2f)); Box("pocket", body, new Vector3(s * 0.100f, 0.70f, -0.017f), new Vector3(0.09f, 0.032f, 0.006f), coatD, new Vector3(0, 0, s * 3f));
            Box("coat_edge", body, new Vector3(s * 0.180f, 0.85f, -0.0085f), new Vector3(0.003f, 0.64f, T + 0.001f), edge, default, 0f);
        }
        Box("coat_edge", body, new Vector3(0.012f, 0.85f, -0.0150f), new Vector3(0.003f, 0.62f, 0.002f), edge, default, 0f);                              // the front edge where one leaf laps the other
        Tri3("lapel", body, new Vector3(-0.004f, 1.17f, -0.016f), new Vector3(-0.094f, 1.16f, -0.016f), new Vector3(-0.025f, 0.96f, -0.016f), 0.006f, coatD); Tri3("lapel", body, new Vector3(0.025f, 1.17f, -0.018f), new Vector3(0.041f, 0.96f, -0.018f), new Vector3(0.110f, 1.16f, -0.018f), 0.006f, coatD);
        Tri3("collar", body, new Vector3(-0.012f, 1.215f, -0.022f), new Vector3(-0.105f, 1.205f, -0.022f), new Vector3(-0.075f, 1.135f, -0.022f), 0.006f, coatM); Tri3("collar", body, new Vector3(0.012f, 1.215f, -0.022f), new Vector3(0.075f, 1.135f, -0.022f), new Vector3(0.105f, 1.205f, -0.022f), 0.006f, coatM);
        Box("collar_stand", body, new Vector3(0, 1.200f, 0.004f), new Vector3(0.15f, 0.045f, T), coatD); Box("belt", body, new Vector3(0.005f, 0.80f, -0.016f), new Vector3(0.37f, 0.032f, 0.006f), coatD);
        for (int k = 0; k < 3; k++) Cyl("button", body, new Vector3(0.026f, 0.92f - 0.11f * k, -0.017f), 0.012f, 0.006f, brass, new Vector3(90, 0, 0));
        Box("shoulder", body, new Vector3(0, 1.165f, 0.004f), new Vector3(0.41f, 0.06f, T), coatD);
        // the back, as the back of every piece of this set: a pencilled number, two tabs of masking tape, a diagonal brace of stripwood
        Box("back_brace", body, new Vector3(0, 0.84f, 0.016f), new Vector3(0.022f, 0.62f, 0.008f), stick, new Vector3(0, 0, 24f), 0f);
        Box("back_tape", body, new Vector3(-0.10f, 1.06f, 0.0135f), new Vector3(0.07f, 0.035f, 0.001f), tapeM, new Vector3(0, 0, 8f), 0f); Box("back_tape", body, new Vector3(0.09f, 0.62f, 0.0135f), new Vector3(0.07f, 0.035f, 0.001f), tapeM, new Vector3(0, 0, -6f), 0f);
        for (int k = 0; k < number + 1; k++) Box("back_number", body, new Vector3(0.10f - 0.018f * k, 1.02f, 0.0135f), new Vector3(0.004f, 0.05f, 0.001f), pencil, new Vector3(0, 0, 6f), 0f);   // tally strokes: which of the six it is
        // arms: upper arm and forearm of card, a brass fastener at shoulder and elbow, a mitten hand
        foreach (float s in new[] { -1f, 1f })
        {
            var arm = new GameObject(s > 0 ? "ArmR" : "ArmL").transform; arm.SetParent(body, false); arm.localPosition = new Vector3(s * 0.195f, 1.15f, -0.016f); arm.localEulerAngles = new Vector3(0, 0, s * 3f);
            Box("upper", arm, new Vector3(0, -0.15f, 0), new Vector3(0.080f, 0.34f, T), coatM); Cyl("fastener", arm, new Vector3(0, 0, -0.009f), 0.013f, 0.008f, brass, new Vector3(90, 0, 0));
            var fore = new GameObject(s > 0 ? "ForeR" : "ForeL").transform; fore.SetParent(arm, false); fore.localPosition = new Vector3(0, -0.30f, -0.014f); fore.localEulerAngles = new Vector3(0, 0, s * 3f);
            Box("fore", fore, new Vector3(0, -0.13f, 0), new Vector3(0.070f, 0.30f, T), coatM); Cyl("fastener", fore, new Vector3(0, 0, -0.009f), 0.011f, 0.008f, brass, new Vector3(90, 0, 0)); Box("cuff", fore, new Vector3(0, -0.265f, -0.002f), new Vector3(0.078f, 0.03f, T + 0.004f), coatD);
            Box("hand", fore, new Vector3(0, -0.315f, 0), new Vector3(0.062f, 0.08f, 0.010f), skin);
            if (s > 0)
            {   // the slip: a card with a bright face, held under the thumb. The side toward the audience is its plain back until "Slip" turns about its long axis.
                Box("thumb", fore, new Vector3(-0.030f, -0.315f, -0.020f), new Vector3(0.022f, 0.045f, 0.008f), skin, new Vector3(0, 0, -25f));
                var slip = new GameObject("Slip").transform; slip.SetParent(fore, false); slip.localPosition = new Vector3(0, -0.36f, -0.012f);
                Box("slip", slip, new Vector3(0, -0.02f, 0), new Vector3(0.10f, 0.14f, 0.0015f), CardM("slip_card", new Color(0.62f, 0.62f, 0.60f), 0.08f), default, 0f);
                var face = Emit("slip_face", new Color(0.002f, 0.002f, 0.002f)); face.SetColor("_BaseColor", new Color(0.86f, 0.86f, 0.84f)); face.SetFloat("_Smoothness", 0.10f);
                Box("slip_face", slip, new Vector3(0, -0.018f, 0.0010f), new Vector3(0.082f, 0.116f, 0.0006f), face, default, 0f);
            }
            else
            {   // the little window lamp: a card box with a paned window, carried by a wire handle; dark until the clock lights it
                Box("thumb", fore, new Vector3(0.030f, -0.300f, 0), new Vector3(0.022f, 0.045f, 0.010f), skin, new Vector3(0, 0, 25f));
                var lampPivot = new GameObject("Lamp").transform; lampPivot.SetParent(fore, false); lampPivot.localPosition = new Vector3(0, -0.317f, -0.02f);                 // the clock keeps this upright, so the lamp always hangs from its handle
                var lamp = new GameObject("LampBody").transform; lamp.SetParent(lampPivot, false); lamp.localPosition = new Vector3(0, -0.128f, 0);
                Box("lamp_box", lamp, Vector3.zero, new Vector3(0.11f, 0.13f, 0.05f), coatD); Box("lamp_roof", lamp, new Vector3(0, 0.072f, 0), new Vector3(0.13f, 0.014f, 0.07f), stick);
                foreach (float hx in new[] { -0.034f, 0.034f }) Rod("lamp_handle", lamp, new Vector3(hx, 0.079f, 0), new Vector3(hx * 0.75f, 0.128f, 0), 0.0025f, Iron); Rod("lamp_handle", lamp, new Vector3(-0.0255f, 0.128f, 0), new Vector3(0.0255f, 0.128f, 0), 0.0025f, Iron);
                Quad("LampPane", lamp, new Vector3(0, 0, -0.0255f), new Vector2(0.075f, 0.095f), Emit("person_lamp", new Color(1f, 0.80f, 0.56f) * 2.0f), default, false); Box("lamp_bar", lamp, new Vector3(-0.012f, 0, -0.027f), new Vector3(0.005f, 0.095f, 0.003f), stick, default, 0f);                                             // a window: a frame and one upright set to one side (never a cross on a dark box)
                foreach (float fy in new[] { -0.0505f, 0.0505f }) Box("lamp_frame", lamp, new Vector3(0, fy, -0.027f), new Vector3(0.087f, 0.006f, 0.004f), stick, default, 0f); foreach (float fx in new[] { -0.0405f, 0.0405f }) Box("lamp_frame", lamp, new Vector3(fx, 0, -0.027f), new Vector3(0.006f, 0.107f, 0.004f), stick, default, 0f);
                var pl = AddLight("LampGlow", lamp, LightType.Point, new Vector3(0, 0, -0.07f), Vector3.zero, new Color(1f, 0.72f, 0.42f), 0.35f, 1.1f, 0, false);
                pl.enabled = false;
            }
        }
        // neck and head
        Box("neck", body, new Vector3(0, 1.235f, 0.002f), new Vector3(0.045f, 0.07f, T), coatD);
        var hd = new GameObject("Head").transform; hd.SetParent(body, false); hd.localPosition = new Vector3(0, 1.235f, 0);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/heads/" + head + ".glb"); if (prefab == null) { AssetDatabase.ImportAsset("Assets/Props/heads/" + head + ".glb", ImportAssetOptions.ForceUpdate); prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/heads/" + head + ".glb"); }
        if (prefab != null)
        {
            var h = (GameObject)PrefabUtility.InstantiatePrefab(prefab); h.name = "mask"; h.transform.SetParent(hd, false); h.transform.localScale = Vector3.one * 0.36f; h.transform.localEulerAngles = new Vector3(0, headYaw, 0);
            var b = BoundsOf(h); float wide = Mathf.Max(b.size.x, b.size.z); if (wide > 0.40f) { h.transform.localScale *= 0.40f / wide; b = BoundsOf(h); }      /* a long snout is not allowed to make the head a giant */
            h.transform.position += hd.position + new Vector3(0, 0.0f, 0) - new Vector3(b.center.x, b.min.y, b.center.z);
            foreach (var r in h.GetComponentsInChildren<Renderer>()) foreach (var m in r.sharedMaterials) if (m != null && m.HasProperty("_Smoothness")) { m.SetFloat("_Smoothness", 0.12f); if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f); }
            log.AppendLine($"  {name}: head {head} {b.size.x * 1000:F0} x {b.size.y * 1000:F0} x {b.size.z * 1000:F0} mm" + Eyes(hd, h, name));
        }
        else { Box("mask_missing", hd, new Vector3(0, 0.16f, 0), new Vector3(0.26f, 0.32f, 0.26f), coatD); log.AppendLine("  MISSING head " + head); }
        // the speech card: blank, on a stick that slides in a sleeve on a bracket behind the shoulders; the clock pushes it up over the head
        Box("speech_bracket", body, new Vector3(0.05f, 1.10f, 0.105f), new Vector3(0.018f, 0.018f, 0.19f), stick, default, 0f); Box("speech_sleeve", body, new Vector3(0.05f, 1.10f, 0.19f), new Vector3(0.030f, 0.05f, 0.024f), stick, default, 0f);
        var sp = new GameObject("Speech").transform; sp.SetParent(body, false); sp.localPosition = new Vector3(0.05f, 0.42f, 0.19f);
        Box("speech_stick", sp, new Vector3(0, -0.035f, 0), new Vector3(0.012f, 0.75f, 0.006f), stick, default, 0f); Box("speech_card", sp, new Vector3(0, 0.44f, -0.004f), new Vector3(0.30f, 0.20f, 0.004f), white); Tri3("speech_tail", sp, new Vector3(-0.09f, 0.345f, -0.004f), new Vector3(-0.02f, 0.345f, -0.004f), new Vector3(-0.12f, 0.29f, -0.004f), 0.004f, white);
        sp.gameObject.SetActive(false);
        return g;
    }

    /// The crow's head is a half mask with open eye holes: a black card stands close behind them, so that one looks into darkness and not
    /// through the head at the scenery. (Finding the holes of every head by rays and fitting eyes and lids was tried and thrown out: it
    /// put cards where there were no eyes.)
    static string Eyes(Transform hd, GameObject mask, string name)
    {
        if (!name.EndsWith("2")) return "";                                                                       // Person2 wears the crow
        var b = BoundsOf(mask); var black = CardM("eye_liner", new Color(0.010f, 0.010f, 0.012f), 0.05f);
        var e = new GameObject("EyeLiner").transform; e.SetParent(hd, true); e.position = new Vector3(b.center.x, b.min.y + 0.545f * b.size.y, b.center.z + 0.035f); e.rotation = Quaternion.identity;
        Box("liner", e, Vector3.zero, new Vector3(0.44f * b.size.x, 0.23f * b.size.y, 0.002f), black, default, 0f);
        // two scraps of pale paper behind the holes: if they sit where the holes are they are its eyes and catch the light of the slip; if not, the mask hides them
        var em = Emit("eye_crow", new Color(0.02f, 0.02f, 0.02f)); em.SetColor("_BaseColor", new Color(0.62f, 0.60f, 0.54f));
        foreach (float sx in new[] { -1f, 1f }) Cyl("Eye", e, new Vector3(sx * 0.135f * b.size.x, 0.012f * b.size.y, -0.004f), 0.011f, 0.002f, em, new Vector3(90, 0, 0)).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        return ", eye holes lined";
    }

    public static readonly string[] HEADS = { "vulture", "hyena", "crow", "wolf", "crocodile", "boar" };
    public static readonly float[] CROWD_X = { -1.50f, -0.94f, -0.38f, 0.48f, 1.04f, 1.60f };
    public static readonly float[] CROWD_SCALE = { 1.00f, 0.95f, 1.03f, 0.97f, 1.02f, 0.94f };

    /// six of them in a row along the second groove, facing the audience
    static void Crowd(Transform root)
    {
        var g = new GameObject("Crowd").transform; g.SetParent(root, false);
        Color[] coats = { new Color(0.34f, 0.33f, 0.36f), new Color(0.22f, 0.30f, 0.46f), new Color(0.38f, 0.42f, 0.28f), new Color(0.46f, 0.32f, 0.22f), new Color(0.52f, 0.52f, 0.54f), new Color(0.42f, 0.26f, 0.36f) };
        Color[] legs = { new Color(0.18f, 0.18f, 0.20f), new Color(0.14f, 0.18f, 0.28f), new Color(0.22f, 0.24f, 0.18f), new Color(0.26f, 0.20f, 0.15f), new Color(0.30f, 0.30f, 0.32f), new Color(0.22f, 0.15f, 0.20f) };
        for (int i = 0; i < 6; i++) { var p = PaperPerson(g, "Person" + i, HEADS[i], coats[i], legs[i], HEAD_YAW[i], i); p.localPosition = new Vector3(CROWD_X[i], 0.0f, 2.22f); p.localScale = Vector3.one * CROWD_SCALE[i]; }
        g.gameObject.SetActive(false);
    }
    public static float[] HEAD_YAW = { 90f, 90f, 90f, 90f, 90f, 90f };       // the generated heads look along +x; turned so that they face the audience (-z)
