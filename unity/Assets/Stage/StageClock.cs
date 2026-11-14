using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// The stage as a function of film time (seconds): which windows are lit, where the camera of the current shot is and what it
/// focuses on. No random numbers, no system clock: the same time always gives the same frame. Paper moves on twos (12 poses a
/// second); the camera moves on ones.
/// First chorus (bars 26-29) on the measured beats, camera moves compounded and timed to the beat:
///   21  bar 26       the singer: an arc and a push at once, a small surge on every beat, the focus thrown to the window behind her
///   22-25  bars 27-28   four soft cuts, one every two beats, each to the window that comes on at that moment
///   26     bar 29       the street from the front of the stage with its six lit windows, a slow push that settles on the bar
[ExecuteAlways]
public class StageClock : MonoBehaviour
{
    public Renderer[] panes; public Light[] paneLights; public float[] paneOn;
    public Color paneColor = new Color(1f, 0.90f, 0.78f); public float paneGain = 1.25f, paneLight = 0.30f;      // the pane's own map is amber already and falls off toward its edges
    public float eyeHeight;                                                              // set once so the camera does not ride on her breathing
    public Camera cam; public Volume volume; public StagePerformer singer;
    public Transform nearR;                                                              // centre of the front of the house with the lit windows
    public int closePane = 4;
    public float[] beat = { 55.240f, 55.774f, 56.308f, 56.842f, 57.377f, 57.934f, 58.468f, 59.002f, 59.536f, 60.070f, 60.604f, 61.161f, 61.719f, 62.253f, 62.787f, 63.344f, 63.878f };
    MaterialPropertyBlock mpb;

    static float Ease(float u) { u = Mathf.Clamp01(u); return u * u * (3 - 2 * u); }
    /// leaves late and arrives hard: most of the way is covered in the middle of the interval, so the camera is "there" on the beat
    static float Snap(float u) { u = Mathf.Clamp01(u); return u < 0.5f ? 16f * u * u * u * u * u : 1f - Mathf.Pow(-2f * u + 2f, 5f) * 0.5f; }

    public int ShotAt(float t) { return t < beat[4] ? 21 : t < beat[6] ? 22 : t < beat[8] ? 23 : t < beat[10] ? 24 : t < beat[12] ? 25 : 26; }

    // The set is built and lit for the house side only, like any stage set. Every camera therefore stays in front of the scenery,
    // below the eaves, and looks up-stage: nothing is shot from above, from the wings or from behind.
    public Vector3 wideFrom = new Vector3(0.95f, 1.45f, -2.3f), wideLook = new Vector3(0.55f, 1.55f, 3.0f); public float wideFocal = 24f;      // from in front of the proscenium: the road, the crossing, the street, the borders above

    void Aim(Vector3 p, Vector3 look, float roll, float focal, float fstop, float focus)
    {
        cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(look - p) * Quaternion.Euler(0, 0, roll); cam.focalLength = focal;
        DepthOfField dof = null; if (volume != null && volume.profile != null) volume.profile.TryGet(out dof);
        if (dof != null) { dof.focusDistance.value = focus > 0 ? focus : Vector3.Distance(p, look); dof.aperture.value = fstop; dof.focalLength.value = focal; }
    }

    public void Apply(float t, int shot = 0)
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        float tq = Mathf.Floor(t * 12f) / 12f;
        for (int i = 0; i < panes.Length; i++)
        {
            float k = Ease((tq - paneOn[i]) / 0.22f);
            float fl = 1f + 0.05f * Mathf.Sin(tq * 9.0f + i * 2.1f) * k;                 // a paper lantern never burns quite evenly
            panes[i].GetPropertyBlock(mpb); float vary = 1f + 0.15f * Mathf.Sin(i * 2.4f + 0.7f); Color pc = Color.Lerp(paneColor, new Color(1f, 0.74f, 0.52f), 0.5f + 0.5f * Mathf.Sin(i * 1.7f + 2.1f));      /* no two windows burn alike */
            mpb.SetColor("_EmissionColor", pc * (paneGain * vary * k * fl)); mpb.SetColor("_BaseColor", new Color(0.13f, 0.14f, 0.19f)); panes[i].SetPropertyBlock(mpb);
            if (paneLights != null && i < paneLights.Length && paneLights[i] != null) { paneLights[i].intensity = paneLight * k * fl; paneLights[i].enabled = k > 0.001f; }
        }
        if (cam == null) return;
        if (shot == 0) shot = ShotAt(t);
        Vector3 H = nearR != null ? nearR.position : Vector3.zero;                       // (centre of the house front, its mid height, its face)
        Vector3 W(int i) { var p = panes[i].transform.position; return new Vector3(p.x, p.y, H.z); }      // a window, on the face of the wall

        if (shot == 21 && singer != null)
        {
            // 70 mm f/2.8. She is circled from 40 to 28 degrees off her front while the camera closes from 2.35 m to 2.12 m; each beat
            // adds a short surge. In the last 0.4 s the focus leaves her for the window that has just come on behind her.
            var head = singer.animator.GetBoneTransform(HumanBodyBones.Head);
            if (eyeHeight <= 0) eyeHeight = head.position.y + 0.055f;
            Vector3 eyes = new Vector3(singer.transform.position.x, 0, singer.transform.position.z) + Vector3.up * eyeHeight;
            float u = Ease((t - (beat[0] - 0.5f)) / (beat[4] + 0.1f - (beat[0] - 0.5f)));
            float surge = 0f; for (int b = 0; b <= 4; b++) { float d = t - beat[b]; if (d >= 0) surge += 0.035f * (1f - Mathf.Exp(-d * 14f)) * 1f; }      // 3.5 cm gained on each beat, quickly
            Vector3 dir = Quaternion.Euler(0, Mathf.Lerp(31f, 19f, u), 0) * Vector3.back;      // far enough round that the dark edge of the proscenium stays out of frame
            Vector3 p = eyes + dir * (Mathf.Lerp(2.35f, 2.22f, u) - surge) + Vector3.up * Mathf.Lerp(-0.04f, 0.03f, u);
            Vector3 side = Quaternion.LookRotation(eyes - p) * Vector3.right;
            Vector3 look = eyes + side * Mathf.Lerp(0.24f, 0.17f, u) + Vector3.down * 0.125f;
            float near = Vector3.Distance(p, head.position + head.forward * 0.06f), far = Vector3.Distance(p, W(2));
            Aim(p, look, Mathf.Lerp(-1.5f, 1.0f, u), 70f, 2.0f, near);                 // the focus stays on her eyes: the windows behind her are warm blurs
        }
        else if (shot >= 22 && shot <= 25)
        {
            // Four soft cuts, one every two beats, each to the window that comes on at that moment (panes 2-5). Seen from the stage
            // floor side, a little below the window; each shot pushes in a few centimetres and has settled by the time the paper glows.
            // Sizes: medium, medium, close, medium, and then the wide.
            int k = shot - 22, b0 = 4 + 2 * k;
            float u = Mathf.Clamp01((t - beat[b0]) / (beat[b0 + 2] - beat[b0])), e = 1f - (1f - u) * (1f - u) * (1f - u);
            // each from 25-30 degrees off the wall and a little above or below, 70 mm at f/2: the near and the far end of the frame fall out of focus
            Vector3[] from = { new Vector3(-0.80f, -0.10f, -1.50f), new Vector3(-0.95f, 0.34f, -1.70f), new Vector3(0.70f, -0.25f, -1.55f), new Vector3(-0.92f, 0.32f, -1.72f) };
            Vector3[] aim = { new Vector3(0.03f, -0.05f, 0.12f), new Vector3(0f, 0.00f, 0.12f), new Vector3(0f, -0.06f, 0.12f), new Vector3(0f, 0.00f, 0.12f) };
            float[] focal = { 70f, 50f, 70f, 50f };                                                                 // the balcony windows are taller: 50 mm so that the whole silhouette is in frame
            Vector3 w = W(2 + k), p = w + from[k] - from[k].normalized * 0.07f * e;
            Aim(p, w + aim[k], 0f, focal[k], 1.8f, Vector3.Distance(p, w + new Vector3(0, 0, 0.12f)));
        }
        else if (shot == 26)
        {
            // the street from the front of the stage, low, all six windows lit: a slow push with a slight rise that settles on the bar
            float u = Ease((t - (beat[12] - 0.1f)) / (beat[16] + 0.5f - (beat[12] - 0.1f)));
            Vector3 p = wideFrom + new Vector3(-0.05f, 0.10f, 0.28f) * u;
            Aim(p, wideLook, 0f, wideFocal, 4f, Vector3.Distance(p, wideLook));
        }
    }
}
