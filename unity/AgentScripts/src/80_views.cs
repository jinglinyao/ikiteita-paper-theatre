    /// the whole theatre from the house, with the warm house light on the proscenium
    public static string Wide(float t, float dist, float focal, string name)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null) return "no stage";
        if (ck.singer != null) ck.singer.Apply(t);
        ck.Apply(t, 26);
        var hl = GameObject.Find("Stage").GetComponentsInChildren<Light>(true).FirstOrDefault(l => l.name == "HouseLight"); if (hl != null) hl.enabled = true;
        var cam = ck.cam; Vector3 p = new Vector3(1.1f, 1.9f, -dist), c = new Vector3(0, 2.0f, 1.2f);
        cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(c - p); cam.focalLength = focal;
        DepthOfField dof; if (ck.volume.profile.TryGet(out dof)) { dof.focusDistance.value = dist + 1.4f; dof.aperture.value = 8f; dof.focalLength.value = focal; }
        if (ck.singer != null) ck.singer.Apply(t);
        RenderTo(cam, 1920, 1080, "Capture/" + name + ".jpg");
        if (hl != null) hl.enabled = false;
        return "wrote Capture/" + name + ".jpg";
    }

    /// any view: camera position, what it looks at, focal length, f-stop
    public static string Free(float t, float px, float py, float pz, float lx, float ly, float lz, float focal, float fstop, string name)
    {
        var ck = Object.FindFirstObjectByType<StageClock>(); if (ck == null) return "no stage";
        if (ck.singer != null) ck.singer.Apply(t);
        ck.Apply(t, 26);
        var cam = ck.cam; Vector3 p = new Vector3(px, py, pz), c = new Vector3(lx, ly, lz);
        cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(c - p); cam.focalLength = focal;
        DepthOfField dof; if (ck.volume.profile.TryGet(out dof)) { dof.focusDistance.value = Vector3.Distance(p, c); dof.aperture.value = fstop; dof.focalLength.value = focal; }
        RenderTo(cam, 1920, 1080, "Capture/" + name + ".jpg");
        return "wrote Capture/" + name + ".jpg";
    }

