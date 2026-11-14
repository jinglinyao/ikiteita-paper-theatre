using System.Collections.Generic;
using UnityEngine;

/// The whole stage as a function of film time (seconds). No random numbers, no system clock: the same time always gives the
/// same stage. It decides where the scenery is (flown, landed, struck), how bright every light of the plan is, which windows
/// and which bulbs of the proscenium burn, where the clouds and the paper sun hang, how far the paper curtain is drawn, what
/// colour the sky cloth has, whether the singer is on, and where every paper property is. Cameras are not its business.
/// Times are written in bars and beats of the song (FilmBars.At). Paper moves on twos (12 positions a second); light is continuous.
/// v2, after the independent review of v1: nothing appears or disappears in view, changes of state sit on the cuts, the dusk
/// follows the shots, the windows of the first chorus stay lit to the end of it.
[ExecuteAlways]
public partial class FilmClock : MonoBehaviour
{
    // base intensities of the plan's lights (the values the scenery was approved with)
    static readonly Dictionary<string, float> BASE = new Dictionary<string, float>
    {
        { "FollowSpot", 5f }, { "Pool", 30f }, { "WindowKick", 3.5f }, { "BackCool", 6f }, { "SceneryWash", 9f }, { "Moon", 42f }, { "LampLight", 14f }, { "GhostLight", 0.9f },
        { "LoftWork", 3f }, { "BackWork", 2.5f }, { "SideCoolR", 7f }, { "SideCoolL", 7f }, { "HouseLight", 22f }, { "ProsRim0", 2.2f }, { "ProsRim1", 2.2f },
        { "Foot0", 9f }, { "Foot1", 14f }, { "Foot2", 9f }, { "LastBulb", 8f }, { "BareBulb", 7f }, { "SunLight", 52f }, { "GhostKey", 40f },
    };
    // what each state asks of each light, as a share of its base intensity (lights not named are off)
    static Dictionary<string, float> L(params object[] kv) { var d = new Dictionary<string, float>(); for (int i = 0; i < kv.Length; i += 2) d[(string)kv[i]] = System.Convert.ToSingle(kv[i + 1]); return d; }
    static readonly Dictionary<string, Dictionary<string, float>> STATE = new Dictionary<string, Dictionary<string, float>>
    {
        { "off", L("GhostLight", 0.5) },
        { "pre", L("HouseLight", 1, "GhostLight", 1, "LoftWork", 1) },                                                    // before the curtain opens: only the front of the theatre
        { "build", L("SceneryWash", 1, "Moon", 1, "GhostLight", 1, "LoftWork", 1, "BackWork", 1, "SideCoolR", 1, "SideCoolL", 1, "HouseLight", 1) },
        { "night", L("Pool", 1, "FollowSpot", 1, "WindowKick", 1, "BackCool", 1, "SceneryWash", 1, "Moon", 1, "LampLight", 1, "GhostLight", 1, "LoftWork", 1, "BackWork", 1, "SideCoolR", 1, "SideCoolL", 1, "HouseLight", 1) },
        { "bright", L("Pool", 1.4, "FollowSpot", 1.15, "WindowKick", 1.3, "BackCool", 1, "SceneryWash", 1.7, "Moon", 1.3, "LampLight", 1.3, "GhostLight", 1, "LoftWork", 1, "BackWork", 1, "SideCoolR", 1, "SideCoolL", 1, "HouseLight", 1.5, "Foot0", 0.5, "Foot1", 0.5, "Foot2", 0.5) },
        { "spot", L("Pool", 1.8, "FollowSpot", 1, "BackCool", 1, "SceneryWash", 0.30, "SideCoolR", 0.5, "SideCoolL", 0.5, "GhostLight", 1, "LoftWork", 1) },
        { "dim", L("SceneryWash", 0.35, "Moon", 0.5, "SideCoolR", 0.5, "SideCoolL", 0.5, "GhostLight", 1, "LoftWork", 1, "BackWork", 1, "HouseLight", 0.3) },   // the set is back but nothing burns
        { "dusk", L("SceneryWash", 0.30, "GhostLight", 1, "LoftWork", 1, "BackWork", 1, "HouseLight", 1) },                                                    // the sun's own light has a track of its own
        { "empty", L("Foot1", 1.4, "FollowSpot", 1, "BackCool", 1, "SideCoolR", 0.5, "SideCoolL", 0.5, "GhostLight", 0.25, "LoftWork", 1) },                 // the bare bulb has a track of its own
        { "dark", L("FollowSpot", 0.8, "BackCool", 0.6, "SceneryWash", 0.15, "GhostLight", 1) },
    };
    // the state track: from this bar and beat on, this state, reached over this many seconds. Every change sits on a cut and is
    // over within a quarter of a second, or belongs to a shot that shows it (the opening of the curtain, the dusk, the end).
    struct Key { public float t; public string s; public float fade; public Key(float bar, float beat, string s, float fade) { t = FilmBars.At(bar, beat); this.s = s; this.fade = fade; } }
    static readonly Key[] TRACK =
    {
        new Key(1, 0, "pre", 0f), new Key(8, 0, "build", 1.0f), new Key(12, 0.5f, "night", 0.08f), new Key(24, 3.5f, "spot", 0.25f), new Key(26, 0, "night", 0.25f), new Key(29, 3.5f, "spot", 0.25f),
        new Key(34, 0, "night", 0.25f), new Key(45, 0, "dusk", 0.6f), new Key(48, 3, "night", 0.10f), new Key(57, 0, "empty", 1.2f), new Key(61, 3.5f, "dim", 0.25f), new Key(68, 0, "spot", 0.25f),
        new Key(71, 0, "night", 0.25f), new Key(75, 0, "bright", 0.4f), new Key(84, 0, "dark", 1.5f), new Key(86, 0, "off", 0.6f),
    };
    public static readonly float[] PANE_ON1 = { 55.48f, 56.55f, 57.42f, 58.50f, 59.56f, 60.63f };
    const float SINGER_END = 185.6f;                                                                                   // she stays until the television goes out
    static readonly float[] CLOUD_Y = { 3.32f, 3.22f, 3.36f };                                                         // the heights the three clouds were hung at

    public Color paneColor = new Color(1f, 0.90f, 0.78f); public float paneGain = 1.25f, paneLight = 0.30f;
    public Vector3 mark = new Vector3(-0.5f, 0.008f, 1.3f), paper = new Vector3(0.62f, 0.0095f, 1.52f);
    public Vector3 markB = new Vector3(0.05f, 0.008f, 1.3f); [System.NonSerialized] public Vector3 markNow = new Vector3(-0.5f, 0.008f, 1.3f);      // where she stands from the return of the set on; where she stands now
    public StagePerformer singer;
    [Tooltip("-1 the clock decides; 0 or 1 lets a shot keep her off or on across a dissolve")] public float singerOverride = -1f;

    // found by name the first time the clock is applied
    bool bound; Dictionary<string, Light> lights; Transform houseR, houseL, farRow, sun, sunRod, lampPost, street, props, cyc, bulbProp, cloudsRoot; Transform[] cloud;
    struct Glow { public Renderer r; public int i; public Color c; } List<Glow> herGlow;
    Transform redPaper, greyPaper, redCorner, greyCorner, folds, bird, wingL, wingR, tape0, tape1, tape2, hand, handThumb, handIndex, rain, snow, palm, palmMid; Renderer[] birdR; Renderer[] cloudR; Color[] cloudC;
    Renderer[] sky, skyDay, farPanes, bulbs, panes, singerR, sunR; float[] farRow3; int soloFar, lastK; int[] keeper; Renderer globe, bulbGlow; List<Renderer> ghostGlass = new List<Renderer>(); Light[] paneLights; List<Transform> pleats; MaterialPropertyBlock mpb;

    static float S(float u) { u = Mathf.Clamp01(u); return u * u * (3 - 2 * u); }
    static float Ramp(float t, float a, float b) { return b <= a ? (t >= a ? 1 : 0) : S((t - a) / (b - a)); }                     // 0 before a, 1 after b
    /// a piece coming down on its line: fast, then stopped, with a small swing of the paper after it has landed (0 up, 1 down)
    static float Land(float t, float a, float b) { if (t <= a) return 0; float u = (t - a) / (b - a); if (u < 1) return 1 - (1 - u) * (1 - u) * (1 - u); float w = t - b; return 1 - 0.012f * Mathf.Exp(-5f * w) * Mathf.Sin(22f * w); }
    static float B(float bar, float beat = 0) { return FilmBars.At(bar, beat); }
    static float Hash(int i, float k) { return Mathf.Repeat(Mathf.Sin(i * 12.9898f + k * 78.233f) * 43758.5453f, 1f); }
    static void Show(Transform t, bool on) { if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on); }

    public void Bind()
    {
        var stage = GameObject.Find("Stage"); if (stage == null) return;
        var all = stage.GetComponentsInChildren<Transform>(true); Transform T(string n) { foreach (var t in all) if (t.name == n) return t; return null; }
        lights = new Dictionary<string, Light>(); foreach (var l in stage.GetComponentsInChildren<Light>(true)) if (!l.name.StartsWith("PaneLight") && l.name != "LampGlow") lights[l.name] = l;
        houseR = T("HouseR"); houseL = T("HouseL"); farRow = T("FarRow"); sun = T("PaperSun"); sunRod = T("SunRod"); lampPost = T("LampPost"); street = T("Street"); props = T("StreetProps"); cyc = T("Cyc"); bulbProp = T("BareBulbProp");
        cloud = new[] { T("Cloud0"), T("Cloud1"), T("Cloud2") }; cloudsRoot = T("Clouds");
        { var cr = new List<Renderer>(); foreach (var c0 in cloud) if (c0 != null) cr.AddRange(c0.GetComponentsInChildren<Renderer>(true)); cloudR = cr.ToArray(); cloudC = new Color[cloudR.Length]; for (int k = 0; k < cloudR.Length; k++) cloudC[k] = cloudR[k].sharedMaterial != null && cloudR[k].sharedMaterial.HasProperty("_BaseColor") ? cloudR[k].sharedMaterial.GetColor("_BaseColor") : Color.white; }
        redPaper = T("RedPaper"); greyPaper = T("GreyPaper"); redCorner = redPaper != null ? redPaper.Find("Corner") : null; greyPaper = T("GreyPaper"); greyCorner = greyPaper != null ? greyPaper.Find("Corner") : null; folds = T("Folds"); bird = T("Bird"); wingL = T("WingL"); wingR = T("WingR");
        tape0 = T("Tape0"); tape1 = T("Tape1"); tape2 = T("Tape2"); hand = T("StageHand"); handThumb = T("HandThumb"); handIndex = T("HandIndex"); rain = T("Rain"); snow = T("Snow");
        var rs = stage.GetComponentsInChildren<MeshRenderer>(true); var a = new List<Renderer>(); var b = new List<Renderer>(); var c = new List<Renderer>(); var d = new List<Renderer>();
        foreach (var r in rs) { if (r.name.StartsWith("SkyDay")) b.Add(r); else if (r.name.StartsWith("Sky")) a.Add(r); else if (r.name == "FarPane") c.Add(r); else if (r.name == "bulb") d.Add(r); else if (r.name == "globe") globe = r; else if (r.name == "ghost_glow") bulbGlow = r; else if (r.name == "ghost_glass") ghostGlass.Add(r); }
        sky = a.ToArray(); skyDay = b.ToArray();
        float fy = farRow != null ? farRow.position.y : 0f;
        farPanes = SortBy(c.ToArray(), r => Mathf.Round((r.transform.position.y - fy) * 8f) * 10f + r.transform.position.x); bulbs = SortBy(d.ToArray(), r => BulbOrder(r.transform.position));
        if (sun != null) sunR = sun.GetComponentsInChildren<Renderer>(true);
        // far windows: which third of the height each is in (0 top), and the one that burns alone in bars 43-44: high, and clear of the lamp post and the sun's rod
        farRow3 = new float[farPanes.Length]; float y0 = 1e9f, y1 = -1e9f; foreach (var r in farPanes) { float y = r.transform.position.y - fy; y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y); }
        soloFar = 0; float best = -1e9f;
        for (int j = 0; j < farPanes.Length; j++) { Vector3 p = farPanes[j].transform.position; float y = p.y - fy; farRow3[j] = Mathf.Min(2, Mathf.FloorToInt((y1 - y) / Mathf.Max(0.01f, y1 - y0) * 3f)); float sc = y - 1.2f * Mathf.Abs(p.x + 0.30f); if (sc > best) { best = sc; soloFar = j; } }
        // the bulb that goes last is the one beside the last light of the plan; three more are left with it for bar 84
        lastK = 0; float bd = 1e9f; for (int k = 0; k < bulbs.Length; k++) { float dd = (bulbs[k].transform.position - new Vector3(1.95f, 0.35f, -0.02f)).sqrMagnitude; if (dd < bd) { bd = dd; lastK = k; } }
        int n = Mathf.Max(1, bulbs.Length); keeper = new[] { lastK, (lastK + n / 4) % n, (lastK + n / 2) % n, (lastK + 3 * n / 4) % n };
        var sc0 = Object.FindFirstObjectByType<StageClock>(); if (sc0 != null) { panes = sc0.panes; paneLights = sc0.paneLights; if (singer == null) singer = sc0.singer; }
        herGlow = new List<Glow>();
        if (singer != null) { singerR = singer.GetComponentsInChildren<Renderer>(true); foreach (var r in singerR) { var ms = r.sharedMaterials; for (int k = 0; k < ms.Length; k++) if (ms[k] != null && ms[k].HasProperty("_EmissionColor")) { Color ec = ms[k].GetColor("_EmissionColor"); if (ec.maxColorComponent > 0.01f) herGlow.Add(new Glow { r = r, i = k, c = ec }); } } if (singer.animator != null) { palm = singer.animator.GetBoneTransform(HumanBodyBones.RightHand); palmMid = singer.animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal); } }
        birdR = bird != null ? bird.GetComponentsInChildren<Renderer>(true) : null;
        pleats = new List<Transform>(); foreach (var t in all) if (t.name.StartsWith("pleat_")) pleats.Add(t);
        BindPeople(all);
        mpb = new MaterialPropertyBlock(); birdPath = null; bound = lights.Count > 0;
    }
    static Renderer[] SortBy(Renderer[] a, System.Func<Renderer, float> key) { var l = new List<Renderer>(a); l.Sort((x, y) => key(x).CompareTo(key(y))); return l.ToArray(); }
    // round the opening: up the left side, across the top, down the right side
    static float BulbOrder(Vector3 p) { return p.y > 2.8f ? p.x + 2f : 5f + (3f - p.y) + (p.x > 0 ? 0.05f : 0f); }

    public string StateAt(float t, out string from, out float mix)
    {
        int i = 0; while (i + 1 < TRACK.Length && t >= TRACK[i + 1].t) i++;
        from = i > 0 ? TRACK[i - 1].s : TRACK[0].s; mix = TRACK[i].fade <= 0 ? 1f : S((t - TRACK[i].t) / TRACK[i].fade); return TRACK[i].s;
    }
    static float Level(string state, string light) { return STATE[state].TryGetValue(light, out float v) ? v : 0f; }
    /// when she is on the stage: first chorus, second chorus, the struck stage until the paper falls, and from the bar the bird comes to her hand until the television goes out
    public bool SingerOn(float t)
    {
        if (singerOverride >= 0f) return singerOverride > 0.5f;
        return t >= B(26) && t < B(30) || t >= B(49) && t < B(53) || t >= B(58) && t < B(60, 2) || t >= B(68) && t < SINGER_END;
    }

    /// brightness 0..1 of one of the six windows of the near house (the bird's passing is added in Apply)
    public float Pane(int i, float t)
    {
        if (t < B(31)) { float off = B(30) + 0.10f * i; return Ramp(t, PANE_ON1[i], PANE_ON1[i] + 0.22f) * (1 - Ramp(t, off, off + 0.45f)); }          // first chorus: one at a time; all six burn to the end of bar 29, then go out one by one
        if (t < B(58))
        {
            float on = B(49, 0.5f * i), v = Ramp(t, on, on + 0.22f);                                                                                 // second chorus: all six again, half a beat apart
            if (i == 4) v *= 1 - Ramp(t, B(55, 2), B(56, 2));                                                                                         // the lamp's window goes out slowly
            return v * (1 - Ramp(t, B(57), B(57) + 0.25f));
        }
        if (t < B(75)) return 0;
        int row = i / 2; float on3 = B(75) + 0.12f * i, off3 = B(83, 1 + (2 - row)); return Ramp(t, on3, on3 + 0.22f) * (1 - Ramp(t, off3, off3 + 0.35f));   // all on at the strongest line; out row by row from the top in bar 83
    }
    /// brightness 0..1 of one of the twenty small windows of the far houses
    public float FarPane(int j, int n, float t)
    {
        float v = 0;
        if (j == soloFar && t < B(48)) v = Ramp(t, B(44, 0.5f), B(44, 0.5f) + 0.3f) * (1 - Ramp(t, B(45), B(45, 1)));                                       // "far away": a single small window
        if (t >= B(50) && t < B(58)) { float on = j < 8 ? B(50, 0.5f * j) : B(51) + (B(52) - B(51)) * (j - 8) / Mathf.Max(1, n - 8); v = Ramp(t, on, on + 0.15f) * (1 - Ramp(t, B(57), B(57) + 0.25f)); }   // second chorus: one every half beat, then faster
        if (t >= B(75)) { float on = B(75) + (B(75, 1) - B(75)) * j / n, off = B(83, 1 + farRow3[j]); v = Ramp(t, on, on + 0.15f) * (1 - Ramp(t, off, off + 0.35f)); }                          // all within one beat; out by rows from the top, with the near house
        return v;
    }
    /// brightness 0..1 of one bulb of the proscenium (k counted round the opening)
    public float Bulb(int k, int n, float t)
    {
        if (t < B(8, 2)) return 0.6f * (1 - Ramp(t, B(8), B(8, 2)));                                                                                   // lit before the show, out as the curtain opens
        float on = B(74) + (B(75) - B(74)) * k / n; float v = Ramp(t, on, on + 0.12f);                                                                 // one after another round the opening
        int ki = System.Array.IndexOf(keeper, k); float off = ki < 0 ? B(83, 2) + (B(84) - B(83, 2)) * k / n : ki == 0 ? 185.0f : B(85, ki - 1);        // four are left; three go on the beats of bar 85, the last at 3:05
        return v * (1 - Ramp(t, off, off + 0.15f));
    }

    public void Apply(float t)
    {
        if (!bound || lights == null) Bind(); if (!bound) return;
        float tq = Mathf.Floor(t * 12f + 0.001f) / 12f;                                                                                               // paper moves on twos
        string from; float mix; string to = StateAt(t, out from, out mix); bool on = SingerOn(t);
        float her = singerOverride >= 0f ? singerOverride : Mathf.Max(Ramp(t, B(26), B(26) + 0.15f) * (1 - Ramp(t, B(30) - 0.15f, B(30))), Ramp(t, B(49), B(49) + 0.15f) * (1 - Ramp(t, B(53) - 0.15f, B(53))),
            Mathf.Max(Ramp(t, B(58), B(58) + 0.15f) * (1 - Ramp(t, B(60, 2) - 0.15f, B(60, 2))), Ramp(t, B(68), B(68) + 0.15f) * (1 - Ramp(t, SINGER_END - 0.4f, SINGER_END))));
        markNow = t >= B(61) ? markB : mark; if (singer != null) { var sp0 = singer.transform.position; if (Mathf.Abs(sp0.x - markNow.x) > 1e-4f || Mathf.Abs(sp0.z - markNow.z) > 1e-4f) singer.transform.position = new Vector3(markNow.x, sp0.y, markNow.z); }      /* from the return of the set she stands in front of the gap in the row, so that none of the six is hidden behind her */
        float shadowPlay = Ramp(t, B(18), B(18) + 0.25f) * (1 - Ramp(t, B(20), B(20) + 0.25f));
        // the ghost lamp, the one lamp of the struck stage: dark while the set is struck, on with a click at bar 58, back to its dim self when the set returns
        float ghostOff = Ramp(t, B(57), B(57) + 0.4f) * (1 - Ramp(t, B(62), B(62) + 0.3f)), click = Ramp(t, B(58), B(58) + 0.08f) * (1 - Ramp(t, B(61, 3), B(61, 3.6f)));      /* out before the cut to the bird: the bar of light on the boards must not flash across the join */
        foreach (var kv in lights)
        {
            if (!BASE.TryGetValue(kv.Key, out float b0)) continue; if (kv.Key == "SunLight" || kv.Key == "BareBulb" || kv.Key == "LastBulb" || kv.Key == "GhostKey" || kv.Key.StartsWith("ProsRim")) continue;
            float lv = Mathf.Lerp(Level(from, kv.Key), Level(to, kv.Key), mix);
            if (kv.Key == "FollowSpot" || kv.Key == "WindowKick" || kv.Key == "BackCool") lv *= her;                                                  // these three are for her
            // bars 18-19: one footlight alone, low and small, throws the row of paper people up the fronts of the houses; the rest of the street is taken down for it
            if (kv.Key == "Foot1") lv = Mathf.Max(lv, 1.6f * shadowPlay); else if (kv.Key == "SceneryWash") lv *= 1 - 0.6f * shadowPlay; else if (kv.Key == "LampLight" || kv.Key == "Moon") lv *= 1 - 0.5f * shadowPlay;
            if (kv.Key == "GhostLight") lv = lv * (1 - ghostOff) + 1.0f * click;
            kv.Value.intensity = b0 * lv; kv.Value.enabled = lv > 0.002f;
        }
        // the pool of light: on her mark from the bar before she appears; on the red paper when she is away; closed to the size of the paper in bars 32-38
        if (lights.TryGetValue("Pool", out var pool))
        {
            bool onMark = t >= B(24, 3.5f) && t < B(29, 3.5f) || t >= B(49) && t < B(53) || t >= B(57) && t < B(60, 2) || t >= B(62);
            float spotness = (to == "spot" ? mix : 0f) + (from == "spot" ? 1f - mix : 0f), onPaper = onMark ? 0f : spotness, narrow = Ramp(t, B(32), B(34)) * (1 - Ramp(t, B(39), B(39) + 0.25f));
            float fold = Ramp(t, B(60, 2), B(60, 2) + 0.2f) * (1 - Ramp(t, B(62), B(62) + 0.3f));                                                      // alone on the sheet that folds itself
            pool.transform.rotation = Quaternion.LookRotation((onMark ? markNow : paper) - pool.transform.position);
            // alone on the paper it has to read as a circle on dark asphalt: narrower and stronger
            pool.spotAngle = Mathf.Lerp(Mathf.Lerp(18f, 9f, Mathf.Max(onPaper, fold)), 3.2f, narrow); pool.innerSpotAngle = pool.spotAngle * 0.6f; pool.intensity *= Mathf.Lerp(1f, 3f, Mathf.Max(onPaper, 0.8f * narrow));
            if (fold > 0.001f) { pool.intensity = BASE["Pool"] * 4.5f * fold; pool.enabled = true; }
        }
        // proscenium bulbs; the two lights along the inside of the proscenium follow them all, the last light follows the last bulb
        float bulbAll = 0; int nb = bulbs != null ? bulbs.Length : 0;
        for (int k = 0; k < nb; k++) { float v = Bulb(k, nb, t); bulbAll += v; bulbs[k].GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", new Color(1f, 0.72f, 0.36f) * (1.7f * v)); mpb.SetColor("_BaseColor", Color.Lerp(new Color(0.30f, 0.27f, 0.22f), new Color(1f, 0.9f, 0.7f), v)); bulbs[k].SetPropertyBlock(mpb); }
        bulbAll = nb > 0 ? bulbAll / nb : 0;
        foreach (var n in new[] { "ProsRim0", "ProsRim1" }) if (lights.TryGetValue(n, out var l)) { l.intensity = BASE[n] * bulbAll; l.enabled = bulbAll > 0.01f; }
        if (lights.TryGetValue("LastBulb", out var last) && nb > 0)
        {
            float v = Bulb(lastK, nb, t) * Ramp(t, B(83, 2), B(84));                                                                                  // it takes over as the others go out
            last.transform.position = bulbs[lastK].transform.position + new Vector3(-0.06f, 0f, -0.12f); last.transform.rotation = Quaternion.LookRotation(new Vector3(-0.2f, 0.5f, 0.6f) - last.transform.position);      /* just in front of the proscenium, so that it rakes the stage lip where the bird sits */ last.spotAngle = 84f; last.innerSpotAngle = 50f;
            last.intensity = BASE["LastBulb"] * v; last.enabled = v > 0.01f;
        }
        // the lamp of the struck stage is the ghost lamp on its stand at the back (nothing hangs over the empty stage)
        if (lights.TryGetValue("BareBulb", out var bb)) bb.enabled = false; if (bulbProp != null) Show(bulbProp, false);
        if (lights.TryGetValue("GhostKey", out var gk)) { gk.intensity = BASE["GhostKey"] * click; gk.enabled = click > 0.01f; }
        if (bulbGlow != null) { float gg = lights.TryGetValue("GhostLight", out var gl0) ? gl0.intensity / BASE["GhostLight"] : 1f; bulbGlow.GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", new Color(1f, 0.80f, 0.55f) * (2.2f * Mathf.Min(1f, gg) + 6f * click)); bulbGlow.SetPropertyBlock(mpb); foreach (var gr in ghostGlass) { gr.GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", new Color(1f, 0.66f, 0.32f) * (0.2f * Mathf.Min(1f, gg) + 0.30f * click)); mpb.SetFloat("_Gain", 0.03f + 0.22f * Mathf.Min(1f, gg) + 1.0f * click); gr.SetPropertyBlock(mpb); } }

        // scenery. Build: the left house lands on bar 10, the far row a beat and a half later, the near house on the fourth beat. Strike in
        // bar 57: the near house first, the others half a beat behind, the sky cloth from the second beat, the street and its furniture last,
        // sinking with the stage lift. The return in bars 62-63 runs the other way; the lamp post comes down on its line last.
        float inL = Land(tq, B(9, 1), B(10)) * (1 - Ramp(tq, B(57, 0.5f), B(57, 3))) + (tq >= B(62) ? Land(tq, B(62), B(63)) : 0);
        float inF = Land(tq, B(9, 2.5f), B(10, 1.5f)) * (1 - Ramp(tq, B(57, 0.5f), B(57, 2.5f))) + (tq >= B(62) ? Land(tq, B(62), B(63)) : 0);
        float inR = Land(tq, B(9, 3.5f), B(10, 3)) * (1 - Ramp(tq, B(57), B(57, 2.5f))) + (tq >= B(62) ? Land(tq, B(62, 2), B(63, 2)) : 0);
        bool struck = tq >= B(57); Show(lampPost, true); Show(sunRod, true); if (cloudsRoot != null) cloudsRoot.localPosition = Vector3.zero;      /* whatever an editor state left behind */
        Fly(houseR, Mathf.Lerp(4.3f, 0.04f, inR)); Fly(houseL, Mathf.Lerp(4.3f, 0.04f, inL)); Fly(farRow, Mathf.Lerp(struck ? 6.4f : 4.3f, 0f, inF));
        // the street is a painted floor cloth: at the strike it is drawn off into the left wing, and drawn back over the boards when the set
        // returns; the street furniture is flown like the houses. Nothing sinks into the floor.
        float cloth = In2((tq - B(57, 1.5f)) / (B(57, 3.5f) - B(57, 1.5f))) * (1 - Out2((tq - B(62)) / (B(62, 2.5f) - B(62))));
        if (street != null) { Show(street, cloth < 0.999f); var p = street.localPosition; p.x = -6.3f * cloth; p.y = 0; street.localPosition = p; }
        float furn = 1 - Ramp(tq, B(57, 0.25f), B(57, 2.25f)) + (tq >= B(62, 1) ? Land(tq, B(62, 1), B(63)) : 0);
        if (props != null) { Show(props, true); var p = props.localPosition; p.y = Mathf.Lerp(4.6f, 0f, Mathf.Min(1.02f, furn)); props.localPosition = p; }
        if (lampPost != null)
        {   // raised from the road in bar 11; at the strike it is flown like the houses, and comes down again on its line
            float up0 = Ramp(tq, B(11), B(11, 3)), out1 = Ramp(tq, B(57, 0.5f), B(57, 2.5f)) * (1 - (tq >= B(63, 2) ? Land(tq, B(63, 2), B(64)) : 0));
            lampPost.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(90f, 0f, up0)); var p = lampPost.localPosition; p.y = Mathf.Lerp(0.12f, 0f, up0) + 4.6f * out1; lampPost.localPosition = p;
        }
        float skyIn = (1 - Ramp(tq, B(57, 1), B(57, 3))) + Ramp(tq, B(62), B(63, 2));
        if (cyc != null) { var p = cyc.localPosition; p.y = Mathf.Lerp(5.4f, 0f, skyIn); cyc.localPosition = p; foreach (var r in sky) r.enabled = skyIn > 0.02f; }
        if (sunRod != null) { var p = sunRod.localPosition; p.y = 5.4f * (1 - skyIn); sunRod.localPosition = p; }
        // clouds: flown just out of sight with the set; lowered one after another in bar 62, up again from the middle of bar 63 so that they are clear of the windows by bar 64
        for (int i = 0; i < 3; i++) if (cloud != null && cloud[i] != null)
        {
            float outC = Ramp(tq, B(57, 0.5f * i), B(57, 2 + 0.5f * i)) * (1 - Ramp(tq, B(62, 0.5f * i), B(62, 2 + 0.5f * i)));
            float low = Ramp(tq, B(62, 0.5f * i), B(62, 2.5f + 0.5f * i)) * (1 - Ramp(tq, B(63, 2), B(64, 1)));
            var p = cloud[i].localPosition; p.y = CLOUD_Y[i] + 1.1f * outC - 1.4f * low; cloud[i].localPosition = p;
        }
        // the paper day (bars 45-48): the sun runs up its rod in one beat and slides down to the roofs of the far houses by 46+2, then behind them;
        // the cloth goes white, then orange (46+2 to 47+2, the long shadows), then back to night by 48+3. The sun's light grows as it sinks and rakes lower.
        float up = Ramp(tq, B(45), B(45, 1)), down = Ramp(tq, B(45, 1), B(47, 2)); float sunY = Mathf.Lerp(-0.60f, 2.70f, up) - 3.30f * down;      /* its highest point stays under the head of the street lamp, which stands in front of it */
        if (sun != null) { var p = sun.localPosition; p.y = Mathf.Max(-0.6f, sunY); sun.localPosition = p; Show(sun, sunY > -0.59f); }
        float day = Ramp(t, B(45), B(45, 1)) * (1 - Ramp(t, B(47, 2), B(48, 3))); Color dc = Color.Lerp(new Color(1.25f, 1.2f, 1.1f), new Color(1.3f, 0.60f, 0.22f), Ramp(t, B(45, 2), B(46, 2))); dc.a = day;
        if (cloudR != null) { for (int k = 0; k < cloudR.Length; k++) { cloudR[k].GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", Color.Lerp(cloudC[k], new Color(0.05f, 0.05f, 0.07f), 0.92f * day)); cloudR[k].SetPropertyBlock(mpb); } mpb.Clear(); }      /* in the paper day all three clouds are dark shapes against the cloth */
        if (skyDay != null) foreach (var r in skyDay) { r.enabled = day > 0.004f && skyIn > 0.02f; r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", dc); r.SetPropertyBlock(mpb); }
        if (sunR != null) foreach (var r in sunR) { r.GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", r.name == "sun_front" ? new Color(1f, 0.86f, 0.56f) * (3.0f * day) : new Color(1f, 0.56f, 0.20f) * (1.8f * day)); r.SetPropertyBlock(mpb); }
        if (lights.TryGetValue("SunLight", out var sl))
        {
            // the light of the day comes from the paper sun itself: as it sinks behind the roofs of the far houses their shadows run out toward the audience
            float v = Ramp(t, B(45), B(45, 2)) * (1 - Ramp(t, B(47, 1.2f), B(47, 2)));
            float sunYc = Mathf.Lerp(-0.60f, 2.70f, Ramp(t, B(45), B(45, 1))) - 3.30f * Ramp(t, B(45, 1), B(47, 2));                                     // light is continuous: where the sun would be between two of its poses
            if (sun != null) { sl.transform.position = new Vector3(sun.position.x, Mathf.Max(sunYc + 0.5f, 1.75f), sun.position.z - 0.16f); sl.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.2f, 0.8f) - sl.transform.position); }
            sl.range = 12f; sl.spotAngle = 112f; sl.innerSpotAngle = 80f; sl.intensity = BASE["SunLight"] * v; sl.enabled = v > 0.01f && sun != null;
        }
        // the paper curtain: closed until bar 8, drawn open during that bar
        float open = Ramp(tq, B(8), B(8, 3.5f));
        foreach (var p in pleats)
        {
            var q = p.name.Split('_'); float sd = q[1] == "L" ? -1f : 1f; int k = int.Parse(q[2]);
            float xo = sd * (2.28f - 0.034f * k), xc = sd * (1.905f - 0.166f * k); float yo = (k % 2 == 0 ? 66f : -66f) * sd, yc = (k % 2 == 0 ? 14f : -14f) * sd;
            p.localPosition = new Vector3(Mathf.Lerp(xc, xo, open), 0, 0.12f); p.localEulerAngles = new Vector3(0, Mathf.Lerp(yc, yo, open), 0);
        }
        if (singerR != null) foreach (var r in singerR) r.enabled = on;
        if (herGlow != null) { float lit = Mathf.Clamp01(2f * her * Mathf.Lerp(Level(from, "FollowSpot"), Level(to, "FollowSpot"), mix)); foreach (var g in herGlow) { g.r.GetPropertyBlock(mpb, g.i); mpb.SetColor("_EmissionColor", g.c * lit); g.r.SetPropertyBlock(mpb, g.i); } mpb.Clear(); }
        birdGlow = Mathf.Lerp(0.55f, 0.40f, Mathf.Clamp01(Mathf.Lerp(Level(from, "SceneryWash"), Level(to, "SceneryWash"), mix)));      /* it never goes dull when the lights come up: lit paper, with the light through it */
        Vector3 birdPos = Properties(tq, t);
        People(tq, t);

        // windows: their own track, plus a stir where the bird passes close (bars 62-74, the dark set)
        for (int i = 0; panes != null && i < panes.Length; i++)
        {
            float k = Pane(i, t); if (t >= B(62) && t < B(75) && birdPath != null) { int wv; Vector3 bl = BirdPos(t, out wv); Vector3 w = panes[i].transform.position; w.z = 2.63f; k = Mathf.Max(k, 0.45f * S(1 - (bl - w).magnitude / 0.85f)); }      /* light is continuous: from where the bird is now, not from its last pose */
            float fl = 1f + 0.05f * Mathf.Sin(Mathf.Floor(t * 12f) / 12f * 9.0f + i * 2.1f) * k, vary = 1f + 0.15f * Mathf.Sin(i * 2.4f + 0.7f);
            Color pc = Color.Lerp(paneColor, new Color(1f, 0.74f, 0.52f), 0.5f + 0.5f * Mathf.Sin(i * 1.7f + 2.1f));
            panes[i].GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", pc * (paneGain * vary * k * fl)); mpb.SetColor("_BaseColor", new Color(0.13f, 0.14f, 0.19f)); panes[i].SetPropertyBlock(mpb);
            if (paneLights != null && i < paneLights.Length && paneLights[i] != null) { paneLights[i].intensity = paneLight * k * fl; paneLights[i].enabled = k > 0.001f; }
        }
        for (int j = 0; farPanes != null && j < farPanes.Length; j++) { float k = FarPane(j, farPanes.Length, t); farPanes[j].GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", new Color(1f, 0.80f, 0.60f) * (1.1f * k * (1f + 0.12f * Mathf.Sin(j * 2.9f)))); mpb.SetColor("_BaseColor", new Color(0.10f, 0.11f, 0.15f)); farPanes[j].SetPropertyBlock(mpb); }
        if (globe != null && lights.TryGetValue("LampLight", out var lamp)) { globe.GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", new Color(1f, 0.70f, 0.36f) * (1.5f * lamp.intensity / BASE["LampLight"])); globe.SetPropertyBlock(mpb); }
        V3(t, tq, her);
    }
    static void Fly(Transform t, float y) { if (t == null) return; var p = t.localPosition; p.y = y; t.localPosition = p; }

    // ------------------------------------------------------------ the paper properties
    // the bird's way through the air: time, place, and how it holds its wings (0 beating, 1 gliding, 2 folded)
    struct BK { public float t; public Vector3 p; public int wings; public BK(float bar, float beat, float x, float y, float z, int w) { t = FilmBars.At(bar, beat); p = new Vector3(x, y, z); wings = w; } }
    BK[] birdPath; float birdGlow;
    void BirdPath()
    {
        Vector3 pp = paper + new Vector3(0, 0.03f, 0); const float PALM = 999f;
        birdPath = new[]
        {
            new BK(61, 3, pp.x, pp.y, pp.z, 2), new BK(62, 0, pp.x, pp.y, pp.z, 2), new BK(62, 2, 0.55f, 1.10f, 1.75f, 0), new BK(63, 1, 0.42f, 2.00f, 2.35f, 0), new BK(63, 2.5f, 0.45f, 2.70f, 2.45f, 0),      // up from the floor, between two of the lowered clouds
            new BK(64, 0, 1.95f, 2.25f, 2.25f, 0), new BK(64, 2, 0.60f, 1.95f, 2.10f, 0), new BK(65, 0, -1.55f, 1.80f, 2.05f, 0),                                                                         // across the fronts of the two houses
            new BK(65, 2, -0.60f, 1.65f, 0.60f, 0), new BK(66, 0, 0.25f, 1.60f, -1.10f, 0),                                                                                                               // toward the proscenium and past the camera
            new BK(66, 2, 0.90f, 2.35f, 0.90f, 1), new BK(67, 0, 0.80f, 1.75f, 2.55f, 1),                                                                                                                 // a slow glide back in, down to the lowest windows
            new BK(67, 1.5f, 1.25f, 1.80f, 2.55f, 0), new BK(67, 3, 1.90f, 2.05f, 2.55f, 0), new BK(68, 0.5f, 2.00f, 2.45f, 2.55f, 0), new BK(68, 2, 0.30f, 2.40f, 0.90f, 0), new BK(68, 2.6f, PALM - 0.30f, 0.45f, -0.25f, 0), new BK(68, 3.5f, PALM - 0.14f, 0.20f, -0.08f, 0),      /* the last of the way down is measured from the open hand itself */      // over her head and down on the side of the open hand, never across her face                             // close along the dark windows, from the lowest to the highest
            new BK(69, 0.5f, PALM, 0, 0, 2), new BK(75, 2, PALM, 0, 0, 2),                                                                                                                                 // in her hand
            new BK(76, 0, -0.50f, 1.98f, 0.85f, 0), new BK(76, 3, 0.00f, 2.72f, 0.10f, 0), new BK(77, 0.5f, 0.12f, 2.15f, -0.70f, 0), new BK(77, 2, 0.35f, 1.85f, -1.50f, 0), new BK(78, 0, 0.95f, 1.75f, -1.85f, 1),                                                                       // up through the ring of bulbs and out of the proscenium
            new BK(78, 2, 1.60f, 1.60f, -1.20f, 1), new BK(79, 2, 0.85f, 0.075f, -0.22f, 2), new BK(87, 0, 0.85f, 0.075f, -0.22f, 2),                                                                     // it comes down on the front edge of the stage and stays
        };
    }
    Vector3 BirdAt(int i) { var k = birdPath[Mathf.Clamp(i, 0, birdPath.Length - 1)]; if (k.p.x > 900f) return (palm != null ? (palmMid != null ? Vector3.Lerp(palm.position, palmMid.position, 0.85f) : palm.position) + new Vector3(0, 0.034f, 0) : markNow + new Vector3(-0.16f, 1.30f, -0.33f)) + new Vector3(k.p.x - 999f, k.p.y, k.p.z); return k.p; }      /* in the middle of the palm, not on the wrist */
    Vector3 BirdPos(float t, out int wings)
    {
        int i = 0; while (i + 2 < birdPath.Length && t >= birdPath[i + 1].t) i++;
        float u = Mathf.Clamp01((t - birdPath[i].t) / Mathf.Max(0.001f, birdPath[i + 1].t - birdPath[i].t)); wings = u < 0.5f ? birdPath[i].wings : birdPath[i + 1].wings;
        if (birdPath[i].wings == 2 && birdPath[i + 1].wings == 2) { wings = 2; return Vector3.Lerp(BirdAt(i), BirdAt(i + 1), u); }
        if (birdPath[i].wings == 2 || birdPath[i + 1].wings == 2) wings = u > 0.08f && u < 0.92f ? 0 : 2;                                               // it beats its wings all the way off the ground and on to it
        Vector3 p0 = BirdAt(i - 1), p1 = BirdAt(i), p2 = BirdAt(i + 1), p3 = BirdAt(i + 2); float u2 = u * u, u3 = u2 * u;
        return 0.5f * ((2f * p1) + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 + (-p0 + 3f * p1 - 3f * p2 + p3) * u3);                   // Catmull-Rom through the keys
    }

    /// t is the time on twos (paper), tc the continuous time (the beat of the bird's wings); returns where the bird is
    Vector3 Properties(float t, float tc)
    {
        if (birdPath == null) BirdPath();
        // paper snow in front of the closed curtain, thinning out as it opens
        bool snowing = t < B(8, 3); Show(snow, snowing);
        if (snowing && snow != null)
        {
            float thin = 1 - Ramp(t, B(7, 2), B(8, 3)); int n = snow.childCount;
            for (int i = 0; i < n; i++)
            {
                var c = snow.GetChild(i); float sp = 0.45f + 0.5f * Hash(i, 1), ph = Hash(i, 2) * 3.6f, fall = Mathf.Repeat(t * sp + ph, 3.6f);
                float born = t - fall / sp; Show(c, Hash(i, 7) < 1 - Ramp(born, B(7, 2), B(8, 2)));                                  // none are added from bar 7; those already in the air fall to the end
                c.localPosition = new Vector3(-2.0f + 4.0f * Hash(i, 3) + 0.10f * Mathf.Sin(t * (0.8f + Hash(i, 4)) + i), 3.3f - fall, -0.34f + 0.36f * Hash(i, 5));
                c.localEulerAngles = new Vector3(60f * Mathf.Sin(t * (1.1f + Hash(i, 6)) + i), 360f * Hash(i, 8) + 90f * t * (Hash(i, 9) - 0.5f), 40f * Mathf.Sin(t * 1.7f + i * 2f));
            }
        }
        // The red paper lies on the road from the start. A hand of black card comes in level from the left wing (bar 34), stops over the
        // corner of the sheet (bars 35-36), comes down and pinches it (bar 37), lifts the corner, then the whole sheet, which hangs from
        // the pinch as it is carried off (bar 38); it comes back with a grey sheet hanging the same way, lays it down in the same place
        // and is gone before bar 39.
        Vector3 cL = new Vector3(0.098f, 0, 0.066f); Quaternion flat = Quaternion.Euler(0, 14f, 0); Vector3 pinch = paper + flat * cL;
        Vector3 off = new Vector3(-3.4f, 0.55f, pinch.z), over = pinch + new Vector3(0, 0.11f, 0), at = pinch + new Vector3(0, 0.004f, 0), up = pinch + new Vector3(-0.012f, 0.032f, -0.012f), carry = pinch + new Vector3(-0.01f, 0.27f, 0);
        float enter = Ramp(t, B(34), B(34, 3)), hover = Ramp(t, B(35), B(35, 3)), dip = Ramp(t, B(37), B(37, 1)), lift = Ramp(t, B(37, 1), B(37, 2.5f)), hang = Ramp(t, B(37, 2.5f), B(38)), away = In3((t - B(38)) / (B(38, 1.3f) - B(38)));
        float back = Out3((t - B(38, 1.4f)) / (B(38, 2.6f) - B(38, 1.4f))), lay = Ramp(t, B(38, 2.6f), B(38, 3.0f)), exit = In3((t - B(38, 3.1f)) / (B(39, 0.9f) - B(38, 3.1f)));   // slow away from the paper and slow back to it, so that it is seen to go and to come
        Vector3 hp = Vector3.Lerp(off, new Vector3(-1.3f, 0.45f, pinch.z), enter); hp = Vector3.Lerp(hp, over, hover); hp = Vector3.Lerp(hp, at, dip); hp = Vector3.Lerp(hp, up, lift); hp = Vector3.Lerp(hp, carry, hang);
        hp = Vector3.Lerp(hp, off, away); hp = Vector3.Lerp(hp, carry, back); hp = Vector3.Lerp(hp, at, lay); hp = Vector3.Lerp(hp, off, exit);
        Show(hand, t >= B(34) && t < B(39, 0.9f)); if (hand != null) { hand.localPosition = hp; hand.localEulerAngles = Vector3.zero; float grip = Mathf.Max(Ramp(t, B(37, 0.6f), B(37, 1)) * (1 - away), back * (1 - Ramp(t, B(38, 2.9f), B(38, 3.1f)))); if (handThumb != null) handThumb.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(-40f, -27f, grip)); if (handIndex != null) handIndex.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(6f, 13f, grip)); }
        Show(redPaper, away < 1f);
        if (redPaper != null)
        {   // once it is pinched the sheet is where the hand is: its corner at the pinch, the rest hanging
            Quaternion q = Quaternion.Euler(0, 0, 78f * hang) * flat; redPaper.localRotation = q; redPaper.localPosition = hang > 0f ? hp - q * cL : paper;
            if (redCorner != null) redCorner.localEulerAngles = new Vector3(-70f * lift * (1 - hang), -45f, 0);
        }
        Show(greyPaper, back > 0f && t < B(58));
        if (greyPaper != null)
        {
            Quaternion q = Quaternion.Euler(0, 0, 78f * (1 - lay)) * flat; greyPaper.localRotation = q; greyPaper.localPosition = (lay < 1f ? hp - q * cL : paper) + new Vector3(street != null ? street.localPosition.x : 0f, 0, 0);
            if (greyCorner != null) greyCorner.localEulerAngles = new Vector3(0, -45f, 0);
        }
        // yellow tape: one drawn across the street in bar 16, two more crossed in the proscenium in bar 39; all drawn off again before the paper day
        float t0 = Ramp(t, B(16), B(16, 3)) * (1 - Ramp(t, B(44, 2), B(45))) * (1 - Ramp(t, B(24, 3.5f), B(25, 2)) * (1 - Ramp(t, B(30), B(30, 2.5f)))), t1 = Ramp(t, B(39), B(39, 1.5f)) * (1 - Ramp(t, B(44, 2), B(45)));
        Show(tape0, t0 > 0f); if (tape0 != null) tape0.localPosition = new Vector3(Mathf.Lerp(-7.0f, 0f, t0), 0.95f, 1.78f);
        Show(tape1, t1 > 0f); Show(tape2, t1 > 0f);
        if (tape1 != null) { tape1.localPosition = new Vector3(0, 1.5f, 0.07f) + new Vector3(0.857f, 0.515f, 0) * (6.2f * (1 - t1)); tape1.localEulerAngles = new Vector3(0, 0, 31f); }      /* each runs along its own length, down from an upper corner */
        if (tape2 != null) { tape2.localPosition = new Vector3(0, 1.5f, 0.09f) + new Vector3(-0.857f, 0.515f, 0) * (6.2f * (1 - t1)); tape2.localEulerAngles = new Vector3(0, 0, -31f); }
        // paper rain: down in bar 22, up again in bar 25
        float rn = Ramp(t, B(22), B(22, 2)) * (1 - Ramp(t, B(25), B(25, 1.5f)));
        Show(rain, rn > 0f); if (rain != null) { rain.localPosition = new Vector3(0, Mathf.Lerp(5.2f, 0f, rn), 0); rain.localEulerAngles = new Vector3(0.35f * Mathf.Sin(t * 1.9f), 0, 0.25f * Mathf.Sin(t * 1.3f + 1f)); }
        // the folding on the struck stage: the sheet comes down in the first half of bar 60, then one fold a beat, and it is a bird on the last beat of bar 61
        if (folds != null)
        {
            bool any = t >= B(59, 3) && t < B(61, 3); Show(folds, any);
            if (any)
            {
                int stage = t < B(61) ? 0 : t < B(61, 1) ? 1 : t < B(61, 2) ? 2 : t < B(61, 2.5f) ? 3 : t < B(61, 2.75f) ? 4 : 5;                                  // a fold on each beat, the last two quicker
                for (int k = 0; k < folds.childCount; k++) Show(folds.GetChild(k), k == stage);
                float down = Ramp(t, B(59, 3), B(60, 2.5f));      /* it is already coming down behind the ghost lamp before the cut to it */ float sway = 1 - down;
                folds.localPosition = paper + new Vector3(0.25f * sway * Mathf.Sin(t * 5f), 3.6f * (1 - down) * (1 - down), 0.12f * sway * Mathf.Cos(t * 4f)); folds.localEulerAngles = new Vector3(40f * sway * Mathf.Sin(t * 6f), 20f + 200f * sway, 30f * sway * Mathf.Cos(t * 5f));
            }
        }
        // the bird
        bool flying = t >= B(61, 3); Show(bird, flying); Vector3 bp = paper;
        if (flying && bird != null)
        {
            int w; bp = BirdPos(t, out w); int w2; Vector3 dir = BirdPos(t + 0.05f, out w2) - bp;
            bird.localPosition = bp;
            if (w == 2) bird.localEulerAngles = new Vector3(0, t < B(69) ? 200f : t < B(76) ? 75f : 250f, 0);                                       // at rest it is seen from the side: head, back and tail
            else if (dir.sqrMagnitude > 1e-6f) bird.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            float beat = w == 0 ? 38f * Mathf.Sin(t * 2f * Mathf.PI * 3f) + 12f : w == 1 ? 8f + 4f * Mathf.Sin(t * 2.2f) : 24f;                      // three beats of the wings a second, four poses each (paper moves on twos); half folded at rest
            if (wingL != null) wingL.localEulerAngles = new Vector3(0, 0, -beat); if (wingR != null) wingR.localEulerAngles = new Vector3(0, 0, beat);
            if (birdR != null) { foreach (var r in birdR) { r.GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", new Color(0.95f, 0.10f, 0.06f) * birdGlow); r.SetPropertyBlock(mpb); } mpb.Clear(); }      /* thin paper: in the dark it holds a little light of its own */
        }
        return bp;
    }

    // ------------------------------------------------------------ the animal-headed paper people
    // Six card puppets that ride in the second groove (z 2.22). From the left: vulture, hyena, crow; from the right: boar, crocodile, wolf.
    // They are on for the verses (bars 14-24, 29-48) and from the return of the set (bar 62) to the end; they leave for the two
    // choruses and the struck stage. Each carries a white slip in the right hand and a small dark window lamp in the left.
    static readonly float[] PX = { -1.50f, -0.94f, -0.38f, 0.48f, 1.04f, 1.60f };
    const float WING_X = 2.72f, GROOVE_Z = 2.22f; const int HYENA = 1;
    Transform crowd; Transform[] person, pBody, pHead, pArmR, pForeR, pArmL, pForeL, pSlip, pSpeech, pLamp; Renderer[] pSlipR, pLampR; Light[] pGlow; List<Renderer>[] pEyes; List<Transform>[] pLids; List<float>[] pLidY;
    void BindPeople(Transform[] all)
    {
        crowd = null; foreach (var t in all) if (t.name == "Crowd") crowd = t;
        person = new Transform[6]; pBody = new Transform[6]; pHead = new Transform[6]; pArmR = new Transform[6]; pForeR = new Transform[6]; pArmL = new Transform[6]; pForeL = new Transform[6];
        pSlip = new Transform[6]; pSpeech = new Transform[6]; pLamp = new Transform[6]; pSlipR = new Renderer[6]; pLampR = new Renderer[6]; pGlow = new Light[6]; pEyes = new List<Renderer>[6]; pLids = new List<Transform>[6]; pLidY = new List<float>[6];
        if (crowd == null) return;
        for (int i = 0; i < 6; i++)
        {
            pEyes[i] = new List<Renderer>(); pLids[i] = new List<Transform>(); pLidY[i] = new List<float>(); person[i] = crowd.Find("Person" + i); if (person[i] == null) continue; var sub = person[i].GetComponentsInChildren<Transform>(true);
            foreach (var x in sub)
            {
                switch (x.name)
                {
                    case "Body": pBody[i] = x; break; case "Head": pHead[i] = x; break; case "ArmR": pArmR[i] = x; break; case "ForeR": pForeR[i] = x; break; case "ArmL": pArmL[i] = x; break; case "ForeL": pForeL[i] = x; break;
                    case "Slip": pSlip[i] = x; break; case "Speech": pSpeech[i] = x; break; case "Lamp": pLamp[i] = x; break;
                    case "slip_face": pSlipR[i] = x.GetComponentInChildren<Renderer>(true); break; case "Eye": { var er = x.GetComponentInChildren<Renderer>(true); if (er != null) pEyes[i].Add(er); break; } case "Lid": pLids[i].Add(x); pLidY[i].Add(x.localPosition.y); break; case "LampPane": pLampR[i] = x.GetComponentInChildren<Renderer>(true); break; case "LampGlow": pGlow[i] = x.GetComponent<Light>(); break;
                }
            }
        }
    }
    /// where the bird is at this instant (cameras and lights are continuous; the paper itself moves on twos)
    public Vector3 BirdSmooth(float t) { if (birdPath == null) BirdPath(); if (t < birdPath[0].t) return paper; int w; return BirdPos(t, out w); }
    /// where person i is along the groove at this instant
    public Vector3 PersonSmooth(int i, float t, float y) { float stop; float a = PersonIn(i, t, out stop); return new Vector3(Mathf.Lerp((i < 3 ? -1f : 1f) * WING_X, PX[i], a), y, GROOVE_Z); }
    static float In3(float u) { u = Mathf.Clamp01(u); return u * u * u; }
    static float Out3(float u) { u = Mathf.Clamp01(u); return 1 - (1 - u) * (1 - u) * (1 - u); }
    static float Out2(float u) { u = Mathf.Clamp01(u); return 1 - (1 - u) * (1 - u); }                                                        // pushed off, coasting to a stop
    static float In2(float u) { u = Mathf.Clamp01(u); return u * u; }
    /// how far person i has come along the groove from its wing (0) to its place (1); `stop` is when it last came to rest
    float PersonIn(int i, float t, out float stop)
    {
        float beat = B(14, 1) - B(14); int rank = i < 3 ? i : 5 - i;                                                                           // 0 the outermost of its side
        float in1 = i == 1 ? B(14) : i == 0 ? B(14, 2) : i == 2 ? B(16) : i == 3 ? B(17) : i == 4 ? B(17, 0.75f) : B(17, 1.5f), d1 = (i == 0 ? 5f : 2.4f) * beat;
        float out1 = B(25, 0.25f + 0.35f * rank), in2 = B(29, 3.3f + 0.15f * (2 - rank)), out2 = B(47, 2f + 0.4f * rank), in3 = B(62, 2f + 0.4f * (2 - rank));
        if (t < B(29)) { stop = in1 + d1; return Out2((t - in1) / d1) * (1 - In2((t - out1) / (2.2f * beat))); }
        if (t < B(57)) { stop = in2 + 1.8f * beat; return Out2((t - in2) / (1.8f * beat)) * (1 - In2((t - out2) / (2.5f * beat))); }
        stop = in3 + 2.5f * beat; return Out2((t - in3) / (2.5f * beat));
    }
    /// t is the time on twos, tc the continuous time (light)
    void People(float t, float tc)
    {
        if (crowd == null) return; bool any = false; float beat = B(14, 1) - B(14);
        for (int i = 0; i < 6; i++) if (person[i] != null && person[i].gameObject.activeSelf) { any = true; break; }
        Show(crowd, true);
        for (int i = 0; i < 6; i++)
        {
            if (person[i] == null) continue; float stop, stop0; float a = PersonIn(i, t, out stop), a0 = PersonIn(i, t - 1f / 12f, out stop0), side = i < 3 ? -1f : 1f;
            bool here = a > 0.0005f; Show(person[i], here); if (!here) continue;
            float x = Mathf.Lerp(side * WING_X, PX[i], a), v = (a - a0) * (PX[i] - side * WING_X) * 12f;
            person[i].localPosition = new Vector3(x, 0, GROOVE_Z + (i % 2 == 0 ? -0.004f : 0.004f));                                            // every other one a card's thickness forward, so that neighbours never share a plane
            // the card leans back as it is pushed and swings once when it stops
            float w = t - stop, roll = Mathf.Clamp(v * 2.4f, -5f, 5f) + (w > 0 && w < 1.5f ? side * 2.0f * Mathf.Exp(-4f * w) * Mathf.Sin(13f * w) : 0f);
            // bars 35-37: the vulture and the hyena put their heads together over the hyena's slip
            float q = Ramp(t, B(36, 0.5f), B(36, 2)) * (1 - Ramp(t, B(36, 3.5f), B(37, 1)));                                                    // inside the shot that shows it
            roll += (i == 0 ? -4f : i == 1 ? 2f : 0f) * q;
            if (pBody[i] != null) pBody[i].localEulerAngles = new Vector3(0, 0, roll);
            // the slip: raised to the chest one after another in bar 18 (half a beat apart), lowered again in bars 73-74 (the hyena first, slowly)
            float tf = B(18, 1f + 0.5f * i), up1 = Ramp(t, tf, tf + 0.17f);                                                                  // a beat after the cut, so that the first one is seen to move
            float low0 = i == HYENA ? B(73) : B(73, 2f + 0.3f * i), low1 = i == HYENA ? B(73, 3) : low0 + 2f * beat, read = up1 * (1 - Ramp(t, low0, low1)) * (i == 0 ? 1 - 0.9f * q : 1f);      /* the vulture lets its own slip sink while it looks at the hyena's */
            float flips = up1, pulse = tc >= tf ? Mathf.Exp(-4f * (tc - tf)) : 0f;
            if (i >= 3) { float t2 = B(19, i - 2); flips += 2f * Ramp(t, t2, t2 + 0.25f); if (tc >= t2) pulse = Mathf.Max(pulse, Mathf.Exp(-4f * (tc - t2))); }   // bar 19: one turns its slip over on every beat
            if (pArmR[i] != null) pArmR[i].localEulerAngles = new Vector3(0, 0, Mathf.Lerp(3f, 15f, read)); if (pForeR[i] != null) pForeR[i].localEulerAngles = new Vector3(0, 0, Mathf.Lerp(3f, -130f, read));
            if (pSlip[i] != null) pSlip[i].localRotation = Quaternion.Euler(0, 0, 115f * read) * Quaternion.Euler(0, 180f * flips, 0);
            if (pSlipR[i] != null) { float glow = read * (0.30f + 0.9f * pulse); pSlipR[i].GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", new Color(1f, 0.97f, 0.90f) * (1.2f * glow)); mpb.SetColor("_BaseColor", new Color(0.90f, 0.89f, 0.85f)); pSlipR[i].SetPropertyBlock(mpb); }
            // the lamp: carried dark at the side; raised to the chest in the second half of bar 74, lit with the windows at bar 75, out one after another in bar 83
            float lu0 = B(75, 0.2f * i), lampUp = Ramp(t, lu0, lu0 + 0.5f), lampOn = Ramp(tc, lu0 + 0.35f, lu0 + 0.55f) * (1 - Ramp(tc, B(83, 1f + 0.4f * i), B(83, 1f + 0.4f * i) + 0.3f));
            float armL = Mathf.Lerp(-3f, -160f, lampUp), foreL = Mathf.Lerp(-3f, -20f, lampUp);                                                             // held up beside the head, above the shoulder: a window, not a candle
            if (pArmL[i] != null) pArmL[i].localEulerAngles = new Vector3(0, 0, armL); if (pForeL[i] != null) pForeL[i].localEulerAngles = new Vector3(0, 0, foreL);
            if (pLamp[i] != null) pLamp[i].localEulerAngles = new Vector3(0, 0, -(armL + foreL) - roll);                                         // it hangs from its handle, whatever the arm does
            if (pLampR[i] != null) { pLampR[i].GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", new Color(1f, 0.80f, 0.56f) * (2.0f * lampOn)); mpb.SetColor("_BaseColor", Color.Lerp(new Color(0.16f, 0.15f, 0.14f), new Color(1f, 0.9f, 0.7f), lampOn)); pLampR[i].SetPropertyBlock(mpb); }
            if (pGlow[i] != null) { pGlow[i].intensity = 1.0f * lampOn; pGlow[i].enabled = lampOn > 0.01f; }
            // the head: down over the slip while it reads; in the second half of bar 71 they look up, one after another, and toward her
            float lift = Ramp(t, B(71, 3) + 0.18f * i, B(71, 3) + 0.18f * i + 0.25f);
            float toHer = Mathf.Clamp(Mathf.Atan2(-(markNow.x - x), -(markNow.z - GROOVE_Z)) * Mathf.Rad2Deg, -50f, 50f);
            float tilt = i == 2 ? 12f * Ramp(t, B(20, 2), B(20, 2) + 0.09f) * (1 - Ramp(t, B(20, 3), B(20, 3) + 0.09f)) : 0f;            // the crow cocks its head once, half a beat after it blinks
            if (pHead[i] != null) pHead[i].localEulerAngles = new Vector3(HEAD_DOWN * up1 * (1 - lift) + 4f * lift, toHer * lift + (i == 0 ? -22f : 0f) * q, tilt);
            // its eyes: a glint from the slip it reads, later from the lamp; the crow blinks once (two frames) in bar 20
            float glint = 0.12f + 0.9f * read * (0.30f + 0.9f * pulse) + 1.1f * lampOn;
            foreach (var er in pEyes[i]) { er.GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", Color.Lerp(new Color(0.95f, 0.95f, 1f), new Color(1f, 0.78f, 0.5f), lampOn) * glint); er.SetPropertyBlock(mpb); }
            float blink = i == 2 ? Ramp(t, B(20, 1), B(20, 1) + 0.05f) * (1 - Ramp(t, B(20, 1) + 0.17f, B(20, 1) + 0.22f)) : 0f;
            for (int k = 0; k < pLids[i].Count; k++) { var lp = pLids[i][k].localPosition; lp.y = pLidY[i][k] * (1 - blink); pLids[i][k].localPosition = lp; }
            // the blank speech card on its stick: hidden behind the coat, pushed up over the head (boar bar 40; wolf and crocodile together in bar 41,
            // their cards knocking; the other three in bar 42); all drawn down again in bar 44
            if (pSpeech[i] != null)
            {
                bool has = t >= B(34) && t < B(47, 2); Show(pSpeech[i], has);
                if (has)
                {
                    float tp = i == 5 ? B(40, 1) : i >= 3 ? B(41, 1) : B(42, 1f + 0.5f * i), ws = t - tp, pop = ws <= 0 ? 0f : 1 - Mathf.Exp(-9f * ws) * Mathf.Cos(16f * ws), dn = Ramp(t, B(44, 2) + 0.1f * i, B(44, 2) + 0.1f * i + 0.35f);
                    pSpeech[i].localPosition = Vector3.LerpUnclamped(new Vector3(0.05f, 0.42f, 0.19f), new Vector3(0.05f, 1.30f, 0.19f), pop * (1 - dn));      /* straight up through its sleeve */
                    float knock = i == 3 || i == 4 ? (ws > 0 ? 11f * (1 - Mathf.Exp(-10f * ws)) + 5f * Mathf.Exp(-5f * ws) * Mathf.Sin(18f * ws) : 0f) * (1 - dn) : 0f;
                    pSpeech[i].localEulerAngles = new Vector3(0, 0, (i == 3 ? -1f : 1f) * knock);
                }
            }
        }
    }
    public const float HEAD_DOWN = -32f;
}
