    // ---------------------------------------------------------------- closed-space checks
    static Bounds MinMax(Vector3 a, Vector3 b) { var bb = new Bounds(); bb.SetMinMax(a, b); return bb; }
    static readonly Dictionary<string, Bounds> SPACE = new Dictionary<string, Bounds>
    {
        { "A", MinMax(new Vector3(-3.0f, 0f, 0f), new Vector3(3.0f, 8.2f, 5.2f)) },                 // inside the stage house, loft included
        { "B", MinMax(new Vector3(-3.5f, -1.04f, -5.4f), new Vector3(3.5f, 5.0f, -0.4f)) },        // the cavity of the picture tube
        { "C", MinMax(new Vector3(-1.82f, 0f, -1.365f), new Vector3(1.82f, 2.4f, 1.365f)) },        // the room
    };

    static Texture2D Shoot(Camera cam, int size)
    {
        EnsureDir(GEN); string ap = GEN + $"/probe_{size}.renderTexture";
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(ap);
        if (rt == null) { rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 }; AssetDatabase.CreateAsset(rt, ap); }
        cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(size, size, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, size, size), 0, 0); tex.Apply(); RenderTexture.active = null; cam.targetTexture = null;
        return tex;
    }

    /// From n x n x n points inside a space, look along the six axes. Three passes each:
    ///   void    magenta background, finish off: any magenta pixel means a sight line that ends on nothing
    ///   lit     the lights of the plan as they are: share of pure black
    ///   detail  plan lights off, grey ambient and one head light: a 32-pixel block with no edge in it is a bare surface
    public static string Probe(string space, int n)
    {
        if (!SPACE.TryGetValue(space, out var bb)) return "no space " + space;
        const int SZ = 256, BL = 32;
        string dir = "Capture/probe/" + space; if (Directory.Exists(dir)) Directory.Delete(dir, true); Directory.CreateDirectory(dir);
        var go = new GameObject("ProbeCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 90; cam.aspect = 1; cam.nearClipPlane = 0.02f; cam.farClipPlane = 80; cam.clearFlags = CameraClearFlags.SolidColor; cam.allowHDR = true; cam.enabled = false;
        var ad = go.AddComponent<UniversalAdditionalCameraData>(); ad.renderPostProcessing = false; ad.renderShadows = true;
        var hg = new GameObject("ProbeHead"); hg.transform.SetParent(go.transform, false); hg.transform.localEulerAngles = new Vector3(24, -32, 0);
        var head = hg.AddComponent<Light>(); head.type = LightType.Directional; head.intensity = 1.0f; head.shadows = LightShadows.None; head.enabled = false;
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l != head && l.enabled).ToArray();
        Color amb = RenderSettings.ambientLight;
        Quaternion[] dirs = { Quaternion.identity, Quaternion.Euler(0, 90, 0), Quaternion.Euler(0, 180, 0), Quaternion.Euler(0, 270, 0), Quaternion.Euler(-90, 0, 0), Quaternion.Euler(90, 0, 0) };
        var csv = new StringBuilder("probe,x,y,z,dir,void_px,black_pct,bare_blocks\n"); long voidAll = 0; float blackMax = 0; int bare = 0, blocks = 0, k = 0, worstVoid = -1; long worstVoidPx = 0;
        float F(int i) { return n == 1 ? 0.5f : 0.10f + 0.80f * i / (n - 1); }
        for (int iy = 0; iy < n; iy++) for (int iz = 0; iz < n; iz++) for (int ix = 0; ix < n; ix++, k++)
        {
            Vector3 p = bb.min + Vector3.Scale(bb.size, new Vector3(F(ix), F(iy), F(iz))); go.transform.position = p;
            for (int d = 0; d < 6; d++)
            {
                go.transform.rotation = dirs[d];
                cam.backgroundColor = new Color(1, 0, 1); var tv = Shoot(cam, SZ); var pv = tv.GetPixels32(); int vp = 0; foreach (var c in pv) if (c.r > 200 && c.b > 200 && c.g < 70) vp++;
                if (vp > 0) File.WriteAllBytes($"{dir}/p{k:00}_{d}_void.png", tv.EncodeToPNG()); Object.DestroyImmediate(tv);
                cam.backgroundColor = Color.black; var tl = Shoot(cam, SZ); var pl = tl.GetPixels32(); int bp = 0; foreach (var c in pl) if (c.r < 4 && c.g < 4 && c.b < 5) bp++;
                File.WriteAllBytes($"{dir}/p{k:00}_{d}_lit.jpg", tl.EncodeToJPG(90)); Object.DestroyImmediate(tl);
                foreach (var l in lights) l.enabled = false; head.enabled = true; RenderSettings.ambientLight = new Color(0.30f, 0.30f, 0.30f);
                var td = Shoot(cam, SZ); var pd = td.GetPixels32(); File.WriteAllBytes($"{dir}/p{k:00}_{d}_detail.jpg", td.EncodeToJPG(90)); Object.DestroyImmediate(td);
                foreach (var l in lights) l.enabled = true; head.enabled = false; RenderSettings.ambientLight = amb;
                int bareHere = 0;
                for (int by = 0; by < SZ; by += BL) for (int bx = 0; bx < SZ; bx += BL)
                {
                    int edges = 0;
                    for (int y = by; y < by + BL - 2; y++) for (int x = bx; x < bx + BL - 2; x++)
                    {
                        int a = pd[y * SZ + x].g, gx = Mathf.Abs(pd[y * SZ + x + 2].g - a), gy = Mathf.Abs(pd[(y + 2) * SZ + x].g - a);
                        if (gx + gy > 14) edges++;
                    }
                    blocks++; if (edges < 8) { bare++; bareHere++; }
                }
                voidAll += vp; if (vp > worstVoidPx) { worstVoidPx = vp; worstVoid = k * 10 + d; }
                float bpc = 100f * bp / pl.Length; if (bpc > blackMax) blackMax = bpc;
                csv.Append(k).Append(',').Append(p.x.ToString("F2")).Append(',').Append(p.y.ToString("F2")).Append(',').Append(p.z.ToString("F2")).Append(',').Append(d).Append(',').Append(vp).Append(',').Append(bpc.ToString("F1")).Append(',').Append(bareHere).Append('\n');
            }
        }
        Object.DestroyImmediate(go);
        File.WriteAllText(dir + "/probe.csv", csv.ToString());
        return $"space {space}: {k} probes x 6 views. void pixels {voidAll} (worst probe/dir {worstVoid}, {worstVoidPx} px). pure black: worst view {blackMax:F1}%. bare 32-px blocks {bare}/{blocks} = {100f * bare / blocks:F1}%";
    }
