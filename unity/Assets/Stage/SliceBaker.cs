using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// First pass of the pipeline over shots S21-S23 (shot list v3). Runs in play mode at a fixed 24 frames a second.
///   1  S21 live: the singer is posed from the film time, the spring bones move her hair; every frame her whole skeleton and face
///      are written into a cache (C: every frame baked) and the frame is rendered                              -> Capture/s21_24
///   2  S22 (one window comes on) and S23 (the rise along the facade)                                           -> Capture/s22, Capture/s23
///   3  S21 again from the cache alone, holding each pose for two frames (12 poses a second), camera on ones   -> Capture/s21_12
/// Frame ranges carry two frames of handle on each side of a cut for the four-frame soft cut.
/// Also writes Capture/slice_probe.csv (wrists, chest lean, hips, head, a hair tip per frame) for the motion checks.
public class SliceBaker : MonoBehaviour
{
    public StagePerformer singer; public StageClock clock; public Camera cam;
    public int fps = 24, width = 2560, height = 1440, warmup = 72;
    public int f21a = 1314, f21b = 1379;                                                          // [a, b) in film frames
    public int[] cuts = { 1377, 1403, 1429, 1454, 1481 }; public int last = 1545;                 // the cuts after shot 21; shot 21 + i runs from cuts[i - 1]
    public static int Written; public static bool Done; public static string Note = "";

    IEnumerator Start()
    {
        Written = 0; Done = false; Note = "";
        Time.captureFramerate = fps;
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        cam.enabled = false;
        var all = singer.GetComponentsInChildren<Transform>(true);
        var smrs = singer.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var r in smrs) { r.forceMatrixRecalculationPerRender = true; r.updateWhenOffscreen = true; }
        var face = singer.face; int nShapes = face.sharedMesh.blendShapeCount;
        var dirs = new List<string> { "Capture/s21_24", "Capture/s21_12" }; for (int i = 1; i <= cuts.Length; i++) dirs.Add("Capture/s" + (21 + i));
        foreach (var d in dirs) { if (Directory.Exists(d)) Directory.Delete(d, true); Directory.CreateDirectory(d); }

        int f0 = f21a, n = f21b - f21a;
        var cp = new Vector3[n][]; var cr = new Quaternion[n][]; var cw = new float[n][];
        var anim = singer.animator;
        Transform wR = anim.GetBoneTransform(HumanBodyBones.RightHand), wL = anim.GetBoneTransform(HumanBodyBones.LeftHand), chest = anim.GetBoneTransform(HumanBodyBones.Chest),
                  hips = anim.GetBoneTransform(HumanBodyBones.Hips), head = anim.GetBoneTransform(HumanBodyBones.Head), tip = HairTip(head);
        var log = new System.Text.StringBuilder("frame,t,rx,ry,rz,lx,ly,lz,lean,hx,hz,headyaw,headpitch,tipx,tipy,tipz,headx,heady,headz\n");
        singer.live = true;

        // ---- 1: S21 live
        for (int i = -warmup; i < n; i++)
        {
            float t = (f0 + Mathf.Max(0, i)) / (float)fps;
            singer.time = t; clock.Apply(t, 21);
            yield return new WaitForEndOfFrame();
            if (i < 0) continue;
            cp[i] = new Vector3[all.Length]; cr[i] = new Quaternion[all.Length]; cw[i] = new float[nShapes];
            for (int k = 0; k < all.Length; k++) { cp[i][k] = all[k].localPosition; cr[i][k] = all[k].localRotation; }
            for (int k = 0; k < nShapes; k++) cw[i][k] = face.GetBlendShapeWeight(k);
            Render(cam, rt, tex); File.WriteAllBytes($"Capture/s21_24/f_{f0 + i:00000}.jpg", tex.EncodeToJPG(96)); Written++;
            Vector3 up = chest.up; float lean = Mathf.Atan2(Vector3.Dot(up, singer.transform.forward), up.y) * Mathf.Rad2Deg;
            Vector3 hf = singer.transform.InverseTransformDirection(head.forward);
            log.Append(f0 + i).Append(',').Append(t.ToString("F4")).Append(',').Append(V(wR.position)).Append(',').Append(V(wL.position)).Append(',').Append(lean.ToString("F3")).Append(',')
               .Append(hips.position.x.ToString("F5")).Append(',').Append(hips.position.z.ToString("F5")).Append(',').Append((Mathf.Atan2(hf.x, hf.z) * Mathf.Rad2Deg).ToString("F3")).Append(',')
               .Append((-Mathf.Asin(Mathf.Clamp(hf.y, -1, 1)) * Mathf.Rad2Deg).ToString("F3")).Append(',').Append(V(tip != null ? tip.position : Vector3.zero)).Append(',').Append(V(head.position)).Append('\n');
        }
        File.WriteAllText("Capture/slice_probe.csv", log.ToString());

        // ---- 2: S22 and S23
        var segs = new List<int[]>(); for (int i = 1; i <= cuts.Length; i++) segs.Add(new[] { 21 + i, cuts[i - 1] - 2, i < cuts.Length ? cuts[i] + 2 : last });
        foreach (var seg in segs)
            for (int g = seg[1]; g < seg[2]; g++)
            {
                float t = g / (float)fps; singer.time = t; clock.Apply(t, seg[0]);
                yield return new WaitForEndOfFrame();
                Render(cam, rt, tex); File.WriteAllBytes($"Capture/s{seg[0]}/f_{g:00000}.jpg", tex.EncodeToJPG(96)); Written++;
            }

        // ---- 3: S21 from the cache, on twos. Nothing drives her now: no pose code, no spring bones, no expression pass.
        singer.live = false;
        foreach (var mb in singer.GetComponents<MonoBehaviour>()) if (mb != null && mb != singer && mb.GetType().Name.StartsWith("Vrm10")) mb.enabled = false;
        for (int i = 0; i < n; i++)
        {
            int j = i - (i % 2); float t = (f0 + i) / (float)fps;
            for (int k = 0; k < all.Length; k++) { all[k].localPosition = cp[j][k]; all[k].localRotation = cr[j][k]; }
            for (int k = 0; k < nShapes; k++) face.SetBlendShapeWeight(k, cw[j][k]);
            clock.Apply(t, 21);
            yield return new WaitForEndOfFrame();
            for (int k = 0; k < all.Length; k++) { all[k].localPosition = cp[j][k]; all[k].localRotation = cr[j][k]; }
            for (int k = 0; k < nShapes; k++) face.SetBlendShapeWeight(k, cw[j][k]);
            Render(cam, rt, tex); File.WriteAllBytes($"Capture/s21_12/f_{f0 + i:00000}.jpg", tex.EncodeToJPG(96)); Written++;
        }

        using (var bw = new BinaryWriter(File.Create("Capture/s21_cache.bin")))
        {
            bw.Write(n); bw.Write(all.Length); bw.Write(nShapes); bw.Write(f0); bw.Write(fps);
            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < all.Length; k++) { bw.Write(cp[i][k].x); bw.Write(cp[i][k].y); bw.Write(cp[i][k].z); bw.Write(cr[i][k].x); bw.Write(cr[i][k].y); bw.Write(cr[i][k].z); bw.Write(cr[i][k].w); }
                for (int k = 0; k < nShapes; k++) bw.Write(cw[i][k]);
            }
        }
        Note = $"S21 {f21a}-{f21b - 1}, then {cuts.Length} shots to {last - 1}, bones {all.Length}"; Done = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    static string V(Vector3 v) { return v.x.ToString("F5") + "," + v.y.ToString("F5") + "," + v.z.ToString("F5"); }

    static void Render(Camera cam, RenderTexture rt, Texture2D tex)
    {
        var keep = cam.targetTexture; cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); RenderTexture.active = null;
        cam.targetTexture = keep;
    }

    static Transform HairTip(Transform head)
    {
        Transform best = null; int bestDepth = 0;
        void Walk(Transform t, int depth) { if (t.childCount == 0 && depth > bestDepth) { best = t; bestDepth = depth; } foreach (Transform c in t) Walk(c, depth + 1); }
        Walk(head, 0); return best;
    }
}
