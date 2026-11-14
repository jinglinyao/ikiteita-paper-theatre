p = r'E:/sucai/ikiteita/unity/AgentScripts/Look.cs'; s = open(p, encoding='utf-8').read()
def rep(a, b):
    global s
    assert a in s, a[:90]
    s = s.replace(a, b, 1)

# the seated pose now lives in Assets/Look/SeatPose.cs (shared by edit-mode stills and the play-mode capture)
a = s.index('    // ---------------------------------------------------------------- the seated pose')
b = s.index('    static Vector3 ScreenPos()')
s = s[:a] + '    // ---------------------------------------------------------------- the seated pose (Assets/Look/SeatPose.cs)\n    static string lastSeatNote => SeatPose.lastNote;\n' + s[b:]
a = s.index('        var d = Object.FindFirstObjectByType<SingDriver>(); if (d == null) return;\n        if (!SingDriver.Library.ContainsKey("lap"))')
b = s.index('        d.Apply(t); Seat(d, ScreenPos());')
s = s[:a] + '        var d = Object.FindObjectsByType<SingDriver>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == "Jinglin"); if (d == null) return;\n        SeatPose.EnsureLap();\n        d.Apply(t); SeatPose.Seat(d, ScreenPos());' + s[b + len('        d.Apply(t); Seat(d, ScreenPos());'):]

rep('            try { PoseAt(0f); log.AppendLine("jinglin seated; " + lastSeatNote); }',
    '''            var sp = go.AddComponent<SeatPose>(); sp.driver = d; var scrGo = GameObject.Find("Screen"); sp.lookTarget = scrGo != null ? scrGo.transform : null;
            try { PoseAt(0f); log.AppendLine("jinglin seated; " + lastSeatNote); }''')

# play-mode capture set-up and progress
i = s.rindex('}')
s = s[:i] + r'''
    /// add the play-mode capture component (Assets/Look/ShotDumper.cs); then enter play mode and poll Progress()
    public static string SetupDumper(int warmup)
    {
        foreach (var old in Object.FindObjectsByType<ShotDumper>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
        var d = Object.FindObjectsByType<SingDriver>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == "Jinglin"); if (d == null) return "no character";
        var g = new GameObject("ShotDumper"); var sd = g.AddComponent<ShotDumper>();
        sd.driver = d; sd.roomCam = GameObject.Find("RoomCam").GetComponent<Camera>(); sd.theatreCam = GameObject.Find("TheatreCam").GetComponent<Camera>();
        sd.screen = GameObject.Find("Screen").transform; var gl = GameObject.Find("TvGlow"); sd.tvGlow = gl != null ? gl.GetComponent<Light>() : null;
        sd.seat = SEAT; sd.tvFacing = TVF; sd.seatYaw = SEAT_YAW; sd.streetOrigin = T; sd.warmupFrames = warmup;
        d.usePreviewTime = true; d.previewTime = sd.t0;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return "dumper ready: " + (sd.frames1 + sd.frames2 + sd.frames3) + " frames + " + warmup + " warm-up";
    }

    public static string Progress() { return $"playing {EditorApplication.isPlaying} written {ShotDumper.Written} done {ShotDumper.Done} {ShotDumper.Note}"; }
}
'''
open(p, 'w', encoding='utf-8').write(s); print('patched', len(s))
