using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// Play-mode capture of the look test: the engine really runs (fixed 30 fps time step), so the VRM spring bones move her hair
/// and coat. One continuous take of film time; each frame is rendered from the camera of the shot it belongs to.
///   shot 1  her face from beside the television, drifting in (her hand comes up to her chest)
///   shot 2  over her shoulder, pushing in on the television
///   shot 3  the paper street, the camera sliding sideways
/// Frames: Capture/shot<N>/f_00000.jpg. Paper moves on twos (12 poses a second), the cameras on ones.
public class ShotDumper : MonoBehaviour
{
    public SingDriver driver; public Camera roomCam, theatreCam; public Transform screen; public Light tvGlow;
    public Vector3 seat, tvFacing; public float seatYaw; public Vector3 streetOrigin;
    public int fps = 30, width = 1920, height = 1080, warmupFrames = 90;
    public float t0 = 0.9f; public int frames1 = 105, frames2 = 90, frames3 = 105;
    public Vector3 breeze = new Vector3(0.0f, 0f, 0f);                // not used unless set: a constant push on the spring bones
    public static int Written; public static bool Done; public static string Note = "";

    struct Sway { public Transform t; public Vector3 pos; public Quaternion rot; public float amp, speed, phase; }
    static float Ease(float u) { u = Mathf.Clamp01(u); return u * u * (3 - 2 * u); }
    Vector3 HerRight => Quaternion.Euler(0, seatYaw, 0) * Vector3.right;
    Vector3 HeadPos() { return driver.animator.GetBoneTransform(HumanBodyBones.Head).position + Vector3.up * 0.06f; }

    IEnumerator Start()
    {
        Written = 0; Done = false; Note = "";
        Time.captureFramerate = fps; driver.usePreviewTime = true; driver.previewTime = t0;
        if (driver.audioSource != null) driver.audioSource.mute = true;
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        roomCam.enabled = false;                                       // rendered by hand at the end of each frame; the television's camera keeps running on its own
        var tvRT = theatreCam.targetTexture;
        DepthOfField dof = null; var vol = roomCam.GetComponentInChildren<Volume>(); if (vol != null) vol.profile.TryGet(out dof);
        var sways = new List<Sway>();
        void Add(string n, float amp, float speed, float phase) { var g = GameObject.Find(n); if (g != null) sways.Add(new Sway { t = g.transform, pos = g.transform.position, rot = g.transform.rotation, amp = amp, speed = speed, phase = phase }); }
        Add("crowd_A", 1.3f, 1.9f, 0f); Add("crowd_B", 1.1f, 2.3f, 1.7f); Add("puppet_jinglin", 1.6f, 1.3f, 0.6f); Add("tape", 0.35f, 2.9f, 2.2f);
        float g0 = tvGlow != null ? tvGlow.intensity : 0;
        foreach (int k in new[] { 1, 2, 3 }) { string d = "Capture/shot" + k; if (Directory.Exists(d)) Directory.Delete(d, true); Directory.CreateDirectory(d); }
        var hair = FindHairTip();
        var log = new System.Text.StringBuilder("frame,t,hair_x,hair_y,hair_z,head_x,head_y,head_z\n");
        int total = frames1 + frames2 + frames3; Vector3 tp0 = theatreCam.transform.position; Quaternion tr0 = theatreCam.transform.rotation; float tf0 = theatreCam.fieldOfView;

        for (int f = -warmupFrames; f < total; f++)
        {
            float t = t0 + Mathf.Max(0, f) / (float)fps, tq = Mathf.Floor(t * 12f) / 12f;
            driver.previewTime = t;                                    // during the warm-up she holds the first pose while hair and coat settle
            foreach (var s in sways) { s.t.position = s.pos; s.t.rotation = s.rot; if (s.amp > 0) s.t.RotateAround(s.pos - s.t.up * s.t.lossyScale.y * 0.5f, s.t.forward, s.amp * Mathf.Sin(tq * s.speed + s.phase)); }
            if (tvGlow != null) tvGlow.intensity = g0 * (0.80f + 0.34f * Mathf.PerlinNoise(tq * 2.3f, 0.5f));
            int shot = f < frames1 ? 1 : f < frames1 + frames2 ? 2 : 3; int fi = shot == 1 ? Mathf.Max(0, f) : shot == 2 ? f - frames1 : f - frames1 - frames2;
            Vector3 s0 = screen.position;
            yield return new WaitForEndOfFrame();                      // gestures, the seat, spring bones and skinning of this frame are done
            if (f < 0) continue;
            if (shot == 1)
            {
                float u = Ease(fi / (float)(frames1 - 1));
                Vector3 p = Vector3.Lerp(s0 + tvFacing * 0.10f - HerRight * 0.62f + Vector3.up * 0.26f, s0 + tvFacing * 0.30f - HerRight * 0.44f + Vector3.up * 0.30f, u), look = HeadPos() - Vector3.up * 0.10f;
                roomCam.transform.position = p; roomCam.transform.rotation = Quaternion.LookRotation(look - p); roomCam.fieldOfView = Mathf.Lerp(37f, 31f, u);
                if (dof != null) dof.focusDistance.value = Vector3.Distance(p, HeadPos());
                Render(roomCam, rt, tex, null);
            }
            else if (shot == 2)
            {
                float u = Ease(fi / (float)(frames2 - 1));
                Vector3 p = Vector3.Lerp(seat + tvFacing * 0.95f + HerRight * 0.52f + Vector3.up * 1.20f, seat + tvFacing * 0.50f + HerRight * 0.40f + Vector3.up * 1.12f, u), look = s0 + Vector3.up * 0.02f;
                roomCam.transform.position = p; roomCam.transform.rotation = Quaternion.LookRotation(look - p); roomCam.fieldOfView = Mathf.Lerp(40f, 29f, u);
                if (dof != null) dof.focusDistance.value = Vector3.Distance(p, Vector3.Lerp(HeadPos(), s0, Ease(u * 1.6f)));
                Render(roomCam, rt, tex, null);
            }
            else
            {
                float u = Ease(fi / (float)(frames3 - 1));
                Vector3 a = streetOrigin + new Vector3(-1.5f, 1.45f, -3.9f), b = streetOrigin + new Vector3(1.35f, 1.62f, -3.3f), look = streetOrigin + new Vector3(0.15f, 1.55f, 2.6f);
                theatreCam.transform.position = Vector3.Lerp(a, b, u); theatreCam.transform.rotation = Quaternion.LookRotation(look - theatreCam.transform.position); theatreCam.fieldOfView = Mathf.Lerp(46f, 41f, u);
                Render(theatreCam, rt, tex, tvRT);
            }
            File.WriteAllBytes($"Capture/shot{shot}/f_{fi:00000}.jpg", tex.EncodeToJPG(95));
            Written = f + 1;
            var hd = driver.animator.GetBoneTransform(HumanBodyBones.Head).position; var hp = hair != null ? hair.position : Vector3.zero;
            log.Append(f).Append(',').Append(t.ToString("F3")).Append(',').Append(hp.x.ToString("F5")).Append(',').Append(hp.y.ToString("F5")).Append(',').Append(hp.z.ToString("F5")).Append(',')
               .Append(hd.x.ToString("F5")).Append(',').Append(hd.y.ToString("F5")).Append(',').Append(hd.z.ToString("F5")).Append('\n');
        }
        File.WriteAllText("Capture/hair.csv", log.ToString());
        theatreCam.transform.position = tp0; theatreCam.transform.rotation = tr0; theatreCam.fieldOfView = tf0; theatreCam.targetTexture = tvRT; if (tvGlow != null) tvGlow.intensity = g0;
        Note = "hair probe: " + (hair != null ? hair.name : "none"); Done = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    static void Render(Camera cam, RenderTexture rt, Texture2D tex, RenderTexture restore)
    {
        var keep = cam.targetTexture; cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); RenderTexture.active = null;
        cam.targetTexture = restore != null ? restore : keep;
    }

    /// the end of the longest chain of bones hanging off the head: the tip of a twin tail, to measure whether the hair moves
    Transform FindHairTip()
    {
        var head = driver.animator.GetBoneTransform(HumanBodyBones.Head); Transform best = null; int bestDepth = 0;
        void Walk(Transform t, int depth) { if (t.childCount == 0 && depth > bestDepth) { best = t; bestDepth = depth; } foreach (Transform c in t) Walk(c, depth + 1); }
        Walk(head, 0); return best;
    }
}
