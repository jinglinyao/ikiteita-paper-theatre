    /// The singing lin of the other project on this stage: the keyed performance drives the same model that stands here, the microphone stand
    /// is a child of hers (it goes where she goes and is shown when she is). on = 0 puts the old posed lin back.
    public static string LinSwap(int on)
    {
        var sp = Object.FindFirstObjectByType<StagePerformer>(); if (sp == null) return "no lin";
        var go = sp.gameObject; var old = go.transform.Find("MicStand"); if (old != null) Object.DestroyImmediate(old.gameObject);
        var drv = go.GetComponent<SingDriver>();
        if (on == 0) { sp.driver = null; if (drv != null) Object.DestroyImmediate(drv); sp.Apply(60f); EditorSceneManager.MarkSceneDirty(go.scene); EditorSceneManager.SaveScene(go.scene); return "old lin back"; }
        if (drv == null) drv = go.AddComponent<SingDriver>();
        JsonUtility.FromJsonOverwrite(File.ReadAllText("Data/singdriver_params.json"), drv);
        drv.animator = sp.animator; drv.face = sp.face; drv.lipJson = sp.lipJson; drv.audioSource = null; drv.usePreviewTime = true; drv.clipData = new TextAsset[0]; drv.bodyClips = new AnimationClip[0];
        drv.keyOn = true; drv.keyClipPath = "Data/key_film.bin"; drv.enabled = false; drv.ReloadKeyClip(); sp.driver = drv;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MV/Props/MicStand.prefab"); if (prefab == null) return "no mic prefab";
        var mic = (GameObject)PrefabUtility.InstantiatePrefab(prefab); mic.name = "MicStand"; mic.transform.SetParent(go.transform, false); mic.transform.localPosition = Vector3.zero; mic.transform.localRotation = Quaternion.identity;
        var rig = mic.GetComponent<MicStandRig>(); rig.Configure(go.transform.TransformPoint(drv.micPos), go.transform.TransformDirection(drv.micDir));
        var sb = new StringBuilder(); var an = sp.animator;
        foreach (var b in new[] { HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.RightHand, HumanBodyBones.Head, HumanBodyBones.RightShoulder })
        { var tr = an.GetBoneTransform(b); if (tr != null) sb.Append($" {b} {go.transform.InverseTransformPoint(tr.position):F3}"); }
        sp.Apply(60f);
        sb.Append(" | bars"); foreach (int bar in new[] { 26, 30, 49, 53, 58, 60, 61, 62, 68, 75, 76, 77, 80, 84, 85, 86 }) sb.Append($" {bar}:{FilmBars.At(bar, 0):F3}");
        EditorSceneManager.MarkSceneDirty(go.scene); EditorSceneManager.SaveScene(go.scene);
        return "singing lin on, mic at " + rig.Grille.ToString("F3") + " scale " + go.transform.lossyScale.ToString("F2") + sb;
    }
