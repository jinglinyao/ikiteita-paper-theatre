    // ---------------------------------------------------------------- indirect light, baked into probes
    /// Direct light stays real-time (sharp shadows, lights that change); what the floor, the flats and the sky cloth throw back is
    /// baked once into an adaptive probe volume. on = false switches the baked light off again for the comparison still.
    public static string BakeGI(float spacing)
    {
        log = new StringBuilder();
        var urp = Urp(); var so = new SerializedObject(urp);
        var lp = so.FindProperty("m_LightProbeSystem"); if (lp != null) { lp.intValue = 1; so.ApplyModifiedProperties(); EditorUtility.SetDirty(urp); log.AppendLine("light probe system -> adaptive probe volumes"); }
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            GameObjectUtility.SetStaticEditorFlags(mr.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
            mr.receiveGI = ReceiveGI.LightProbes;
        }
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) l.lightmapBakeType = l.name.StartsWith("PaneLight") ? LightmapBakeType.Realtime : LightmapBakeType.Mixed;
        var ls = new LightingSettings { bakedGI = true, realtimeGI = false, mixedBakeMode = MixedLightingMode.IndirectOnly, lightmapper = LightingSettings.Lightmapper.ProgressiveGPU, indirectSampleCount = 512, directSampleCount = 32, maxBounces = 4 };
        Lightmapping.lightingSettings = ls;
        var old = GameObject.Find("ProbeVolume"); if (old != null) Object.DestroyImmediate(old);
        var pg = new GameObject("ProbeVolume"); pg.transform.position = new Vector3(0, 2.0f, 1.6f);
        var pv = pg.AddComponent<ProbeVolume>(); pv.mode = ProbeVolume.Mode.Local; pv.size = new Vector3(6.8f, 4.4f, 8.6f);
        var scene = EditorSceneManager.GetActiveScene(); EditorSceneManager.SaveScene(scene);
        string guid = AssetDatabase.AssetPathToGUID(scene.path);
        string sp = GEN + "/stage_bakingset.asset";
        var set = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(sp);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();
            var sd = typeof(ProbeVolumeBakingSet).GetMethod("SetDefaults", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (sd != null) sd.Invoke(set, null); else log.AppendLine("  (no SetDefaults)");
            AssetDatabase.CreateAsset(set, sp);
        }
        var sso = new SerializedObject(set); var md = sso.FindProperty("minDistanceBetweenProbes"); if (md != null) { md.floatValue = spacing; sso.ApplyModifiedProperties(); } else log.AppendLine("  (no minDistanceBetweenProbes)");
        log.AppendLine("scene added to baking set: " + set.TryAddScene(guid));
        EditorUtility.SetDirty(set); AssetDatabase.SaveAssets();
        var t0 = System.DateTime.Now;
        bool ok = Lightmapping.Bake();
        log.AppendLine($"bake returned {ok} after {(System.DateTime.Now - t0).TotalSeconds:F1} s");
        EditorSceneManager.SaveScene(scene);
        return log.ToString();
    }

    public static string GIState(bool on)
    {
        var urp = Urp(); var so = new SerializedObject(urp); var lp = so.FindProperty("m_LightProbeSystem"); lp.intValue = on ? 1 : 0; so.ApplyModifiedProperties(); EditorUtility.SetDirty(urp);
        return "light probe system = " + lp.intValue;
    }

