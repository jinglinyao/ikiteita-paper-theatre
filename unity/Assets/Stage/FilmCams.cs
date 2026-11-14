using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// The camera of every stage shot of the film (shot list v5), as a function of film time. Positions come from the camera kinds that
/// passed the void check of the scenery (front of the proscenium, inside low, the gap between the two houses, behind the far row),
/// moved by small amounts; every move is eased and ends on the bar. Shots in the room (type R) and the push into the television
/// (S07) are rendered in the room scene and are not here.
public partial class FilmClock
{
    [System.NonSerialized] public Volume camVolume; StageClock sclock; float eyeY, camT; Light fillLight;
    static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }
    static Vector3 Mix(Vector3 a, Vector3 b, float u) { return Vector3.LerpUnclamped(a, b, u); }
    /// leaves late and arrives hard, so that the camera is "there" on the beat
    static float Snap(float u) { u = Mathf.Clamp01(u); return u < 0.5f ? 16f * u * u * u * u * u : 1f - Mathf.Pow(-2f * u + 2f, 5f) * 0.5f; }
    /// a short surge forward that starts at t0 and stays (metres)
    static float Surge(float t, float t0, float d) { float w = t - t0; return w <= 0 ? 0 : d * (1f - Mathf.Exp(-w * 14f)); }
    /// a small dip when a piece of scenery lands
    static float Nudge(float t, float t0) { float w = t - t0; return w <= 0 ? 0 : 0.012f * Mathf.Exp(-9f * w) * Mathf.Cos(18f * w); }

    void Aim(Camera cam, Vector3 p, Vector3 look, float focal, float fstop, Vector3 focus, float roll = 0f)
    {
        cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(look - p) * Quaternion.Euler(0, 0, roll); cam.focalLength = focal;
        DepthOfField dof = null; if (camVolume == null) camVolume = Object.FindFirstObjectByType<Volume>(); if (camVolume != null && camVolume.profile != null) camVolume.profile.TryGet(out dof);
        if (dof != null) { dof.focusDistance.value = Mathf.Max(0.12f, Vector3.Dot(focus - p, cam.transform.forward)); dof.aperture.value = fstop; dof.focalLength.value = focal; }
    }
    Vector3 Eyes() { if (eyeY <= 0f && singer != null && singer.animator != null) eyeY = 1.555f; return new Vector3(markNow.x + 0.01f, eyeY > 0 ? eyeY : 1.555f, markNow.z + 0.01f); }
    Vector3 PersonAt(int i, float y) { return PersonSmooth(i, camT, y); }                                    // cameras follow where things are now, not their last pose on twos
    Vector3 Win(int i) { if (sclock == null) sclock = Object.FindFirstObjectByType<StageClock>(); var p = sclock.panes[i].transform.position; float z = sclock.nearR != null ? sclock.nearR.position.z : 2.63f; return new Vector3(p.x, p.y, z); }
    Vector3 BirdNow() { return BirdSmooth(camT); }
    /// a small shadowless light of the shot's own, for the few close shots that the stage lighting leaves black (off unless the shot asks)
    void Fill(Vector3 at, Color c, float intensity, float range = 2.5f)
    {
        if (fillLight == null) { var t0 = transform.Find("ShotFill"); if (t0 == null) { var g = new GameObject("ShotFill"); g.transform.SetParent(transform, false); t0 = g.transform; fillLight = g.AddComponent<Light>(); fillLight.type = LightType.Point; fillLight.shadows = LightShadows.None; } else fillLight = t0.GetComponent<Light>(); }
        fillLight.transform.position = at; fillLight.color = c; fillLight.intensity = intensity; fillLight.range = range; fillLight.enabled = intensity > 0.001f;
    }
    Vector3 HandNow() { return palm != null ? palm.position : markNow + new Vector3(-0.16f, 1.28f, -0.31f); }

    /// place the camera for shot n (1-based, as in the shot list) at film time t; false when the shot is not a stage shot
    public bool ShotCamera(int n, float t, Camera cam)
    {
        if (!bound) Bind(); if (n < 1 || n > FilmShots.N) return false; camT = t; Fill(Vector3.zero, Color.white, 0f);
        float t0 = FilmShots.F0[n - 1] / 24f, t1 = FilmShots.F1[n - 1] / 24f, u = (t - t0) / (t1 - t0), e = S(u), eo = 1f - (1f - Mathf.Clamp01(u)) * (1f - Mathf.Clamp01(u));
        Vector3 E = Eyes(), PA = paper, p, look;
        switch (n)
        {
            // ---------------- the set is built
            case 8: p = Mix(V(0.45f, 1.35f, -2.25f), V(0.0f, 1.42f, -1.95f), e); p.y -= Nudge(t, B(10)) + Nudge(t, B(10, 1.5f)) + Nudge(t, B(10, 3)); Aim(cam, p, V(0.2f, 1.75f, 2.9f), 24, 5.6f, V(0.5f, 1.6f, 2.7f)); return true;
            case 9: Aim(cam, V(-1.6f, 0.16f, 1.0f), V(-0.6f, 1.15f, 2.32f), 50, 4f, V(-0.5f, 1.0f, 2.32f)); return true;
            case 10: { Fill(V(0.25f, 3.1f, 1.7f), new Color(1f, 0.86f, 0.66f), 1.6f * (1 - Ramp(t, FallT0 + 0.7f, FallT0 + 1.1f)), 3.2f); p = V(-0.55f, 1.05f, 0.35f); look = V(0.95f, 2.6f, 2.7f); p += (look - p).normalized * 0.12f * e; Aim(cam, p, look, 24, 4f, V(1.0f, 2.6f, 2.8f)); return true; }      /* from the street up the front of the house: the lamp comes on, and she goes from the roof */
            case 11: p = PA + V(0.22f, 0.17f, -0.56f); p += (PA - p).normalized * 0.06f * e; Aim(cam, p, PA + V(0, 0.005f, 0), 70, 2.8f, PA); return true;
            // ---------------- the paper people come in
            case 12: { Vector3 h = PersonAt(HYENA, 1.43f); p = Mix(V(0.35f, 1.1f, 0.75f), V(-0.05f, 1.1f, 0.75f), e); Aim(cam, p, h + V(0.22f, -0.10f, 0), 35, 2.8f, h); return true; }
            case 13: { Vector3 b = PersonAt(0, 0f); p = V(-1.6f + 0.6f * (b.x + 1.6f) + 0.25f, Mathf.Lerp(0.20f, 0.14f, e), GROOVE_Z - 0.62f); Fill(b + V(0.5f, 0.5f, -0.7f), new Color(0.55f, 0.68f, 1f), 1.6f); Aim(cam, p, b + V(0.05f, 0.06f, 0), 50, 5.6f, b); return true; }
            case 14: Aim(cam, V(0.15f, 0.98f, 1.42f), V(0.8f, 1.15f, 3.0f), 24, 4f, V(0.5f, 1.0f, 2.6f)); return true;
            case 15: p = V(0.78f, 1.40f, 0.80f + 0.03f * e); Aim(cam, p, V(0.74f, 1.38f, GROOVE_Z), 50, 2.8f, V(0.48f, 1.43f, GROOVE_Z)); return true;
            case 16: { float sg = 0; for (int i = 0; i < 6; i++) sg += Surge(t, B(18, 1f + 0.5f * i), 0.02f); p = Mix(V(0.08f, 1.22f, -1.75f), V(0.08f, 1.22f, -1.57f), e) + V(0, 0, sg); Aim(cam, p, V(0.05f, 1.12f, GROOVE_Z), 32, 8f, V(0, 1.2f, GROOVE_Z)); return true; }
            case 17: p = V(1.04f, 1.08f, 0.52f + 0.05f * e); Aim(cam, p, V(1.04f, 1.06f, GROOVE_Z), 50, 4f, V(1.04f, 1.05f, GROOVE_Z)); return true;      /* one steady shot of three hands: the wolf, the crocodile and the boar turn their slips on the beats */
            case 18: { Vector3 c = PersonAt(2, 1.44f); p = V(-0.10f, 1.47f, 0.62f); p += (c - p).normalized * 0.03f * e; Aim(cam, p, c, 85, 2.8f, c + V(0, 0, -0.12f)); return true; }
            case 19: p = V(0.75f, 0.25f, 2.52f); p += (PA - p).normalized * 0.10f * e; Aim(cam, p, PA + V(0, 0.02f, 0), 35, 2.0f, PA); return true;
            case 20: p = Mix(V(-0.3f, 2.0f, -0.8f), V(-0.3f, 1.5f, -0.8f), e); Aim(cam, p, Mix(V(-0.2f, 2.6f, 1.9f), V(-0.1f, 1.45f, 2.1f), e), 35, 4f, V(-0.1f, 1.6f, 1.9f)); return true;
            case 21: p = Mix(V(0.4f, 1.0f, -1.2f), V(0.4f, 1.1f, -1.2f), e); Aim(cam, p, V(0.2f, 2.0f, 1.9f), 24, 4f, V(0.2f, 1.8f, 1.8f)); return true;
            case 22: Aim(cam, V(0.3f, 1.6f, 4.72f), V(0f, 1.5f, 0f), 35, 4f, V(0.2f, 1.5f, 3.6f)); return true;
            case 23: Aim(cam, V(-0.2f, 1.6f, -1.5f), V(-0.5f, 0.35f, 1.3f), 35, 4f, mark); return true;
            // ---------------- first chorus (the approved sample)
            case 24:
                {
                    if (sclock == null) sclock = Object.FindFirstObjectByType<StageClock>(); var bt = sclock.beat;
                    float uu = S((t - (bt[0] - 0.5f)) / (bt[4] + 0.1f - (bt[0] - 0.5f))); float surge = 0f; for (int b = 0; b <= 4; b++) surge += Surge(t, bt[b], 0.035f);
                    Vector3 dir = Quaternion.Euler(0, Mathf.Lerp(31f, 19f, uu), 0) * Vector3.back; p = E + dir * (Mathf.Lerp(2.35f, 2.22f, uu) - surge) + Vector3.up * Mathf.Lerp(-0.04f, 0.03f, uu);
                    Vector3 side = Quaternion.LookRotation(E - p) * Vector3.right; look = E + side * Mathf.Lerp(0.24f, 0.17f, uu) + Vector3.down * 0.125f;
                    Aim(cam, p, look, 70, 2.0f, E, Mathf.Lerp(-1.5f, 1.0f, uu)); return true;
                }
            case 25: case 26: case 27: case 28:
                {
                    int k = n - 25; float ee = 1f - (1f - Mathf.Clamp01(u)) * (1f - Mathf.Clamp01(u)) * (1f - Mathf.Clamp01(u));
                    Vector3[] from = { V(-0.80f, -0.10f, -1.50f), V(-0.95f, 0.34f, -1.70f), V(0.70f, -0.25f, -1.55f), V(-0.92f, 0.32f, -1.72f) };
                    Vector3[] aim = { V(0.03f, -0.05f, 0.12f), V(0f, 0.00f, 0.12f), V(0f, -0.06f, 0.12f), V(0f, 0.00f, 0.12f) }; float[] fl = { 70f, 50f, 70f, 50f };
                    Vector3 w = Win(2 + k); p = w + from[k] - from[k].normalized * 0.07f * ee; Aim(cam, p, w + aim[k], fl[k], 1.8f, w + V(0, 0, 0.12f)); return true;
                }
            case 29: p = V(0.95f, 1.45f, -2.3f) + V(-0.05f, 0.10f, 0.28f) * e; Aim(cam, p, V(0.55f, 1.55f, 3.0f), 24, 4f, V(0.55f, 1.55f, 3.0f)); return true;
            // ---------------- the red paper
            case 30: p = Mix(V(0.60f, 2.55f, 2.56f), V(0.74f, 1.95f, 2.50f), e); Fill(V(0.76f, 2.3f, 2.9f), new Color(0.55f, 0.68f, 1f), 0.9f, 2.2f); Aim(cam, p, PA + V(0, 0, 0.05f), 28, 4f, PA); return true;
            case 31: p = V(PA.x, Mathf.Lerp(2.3f, 1.9f, e), PA.z - 0.12f); Aim(cam, p, PA, 50, 4f, PA); return true;
            case 32: p = V(0.4f, 0.25f, -0.25f); p += (PA - p).normalized * 0.16f * e; Fill(V(0.7f, 1.0f, 1.75f), new Color(0.55f, 0.68f, 1f), 1.0f, 2.2f); Aim(cam, p, PA + V(0, 0.05f, 0), 85, 2.8f, PA); return true;
            case 33: p = V(-0.55f, 0.62f, 0.75f); look = V(-1.45f, 0.52f, 1.55f); p += (look - p).normalized * 0.03f * e; Fill(V(-1.9f, 1.1f, 2.0f), new Color(0.55f, 0.68f, 1f), 1.2f, 2.5f); Aim(cam, p, look, 50, 2.8f, V(-1.6f, 0.45f, 1.52f)); return true;
            case 34: p = PA + V(Mathf.Lerp(0.28f, 0.34f, e), 0.16f, -0.62f); Aim(cam, p, PA + V(0.03f, 0.09f, 0), 50, 2.8f, PA + V(0.1f, 0.05f, 0)); return true;
            case 35: { Vector3 m = (PersonAt(0, 1.30f) + PersonAt(1, 1.30f)) * 0.5f; Aim(cam, V(m.x + 0.10f, 1.3f, 0.62f), m, 60, 2.8f, PersonAt(1, 1.10f)); return true; }
            case 36: { Vector3 c = PA + V(0.105f, 0.02f, 0); float lift = Ramp(t, B(37, 1), B(37, 3)); p = c + V(0.18f, 0.10f, -0.35f); p += (c - p).normalized * 0.03f * e; float hang = Ramp(t, B(37, 2.5f), B(38)); Aim(cam, p, c + V(0, 0.04f * lift + 0.10f * hang, 0), 85, 4f, c + V(-0.03f, 0.03f * lift + 0.12f * hang, 0)); return true; }
            case 37: Aim(cam, V(PA.x, 1.45f, PA.z - 0.10f), PA, 50, 4f, PA); return true;
            case 38: p = Mix(V(0, 1.5f, -2.6f), V(0, 1.5f, -2.8f), e); Aim(cam, p, V(0, 1.5f, 0.1f), 24, 5.6f, V(0, 1.5f, 0.08f)); return true;
            case 39: { Vector3 c = PersonAt(5, 1.50f); Aim(cam, V(c.x - 0.30f, 1.45f, 0.62f), c + V(0.03f, 0, 0), 50, 2.8f, PersonAt(5, 1.5f)); return true; }
            case 40: { float ws = t - B(41, 1), pop = ws <= 0 ? 0f : 1 - Mathf.Exp(-7f * ws) * Mathf.Cos(11f * ws); float cx = (PX[3] + PX[4]) * 0.5f + 0.05f; Aim(cam, V(cx, 1.55f, 1.37f), V(cx, Mathf.LerpUnclamped(1.42f, 1.62f, pop), 2.4f), 35, 2.8f, V(cx, 1.7f, 2.4f)); return true; }
            case 41: p = V(Mathf.Lerp(-0.25f, 0.25f, e), 1.4f, -1.7f); Aim(cam, p, V(p.x * 0.5f + 0.1f, 1.45f, GROOVE_Z), 35, 5.6f, V(0, 1.5f, GROOVE_Z)); return true;
            // ---------------- far away; the paper day
            case 42: { p = Mix(V(0.05f, 1.1f, 2.5f), V(0.05f, 1.1f, 2.8f), e); look = V(-0.12f, 0.75f, 4.3f); Aim(cam, p, look, 35, 4f, Mix(V(-0.32f, 1.0f, 3.2f), look, S((u - 0.15f) / 0.5f))); return true; }
            case 43: { Vector3 w = farPanes != null && farPanes.Length > 0 ? farPanes[soloFar].transform.position : V(-0.2f, 0.9f, 4.3f); p = V(0.0f, 0.82f, 2.9f); p += (w - p).normalized * 0.03f * e; Aim(cam, p, w + V(0.03f, -0.05f, 0), 85, 2.8f, w); return true; }
            case 44: { p = Mix(V(0, 1.5f, -2.4f), V(0, 1.5f, -2.7f), e); float sy = Mathf.Clamp(Mathf.Lerp(-0.60f, 2.70f, Ramp(t, B(45), B(45, 1))) - 3.30f * Ramp(t, B(45, 1), B(47, 2)), 0.8f, 2.6f); Aim(cam, p, V(-0.1f, Mathf.Lerp(1.35f, 1.9f, (sy - 0.8f) / 1.8f), 4.7f), 28, 5.6f, V(0, 1.6f, 3.0f)); return true; }
            case 45: Aim(cam, V(-1.2f, 0.25f, 1.0f), V(1.2f, 0.4f, 2.9f), 35, 4f, V(0.2f, 0.1f, 2.2f)); return true;
            // ---------------- second chorus
            case 47:
                {
                    float sg = 0; for (int b = 0; b < 4; b++) sg += Surge(t, B(49, b), 0.03f);
                    Vector3 dir = Quaternion.Euler(0, Mathf.Lerp(-18f, -6f, e), 0) * Vector3.back; p = E + dir * (Mathf.Lerp(2.3f, 2.15f, e) - sg) + V(0, -0.22f, 0); Aim(cam, p, E + V(0.05f, -0.30f, 0), 50, 2.0f, E); return true;
                }
            case 48: p = V(0.0f, 0.75f, 2.65f + 0.1f * e); Aim(cam, p, V(-0.13f, 0.55f, 4.3f), 50, 2.8f, V(-0.13f, 0.6f, 4.3f)); return true;
            case 49: p = V(-0.10f, 0.74f, 2.50f + 0.12f * e); Aim(cam, p, V(-0.13f, 0.70f, 4.3f), 35, 2.8f, V(-0.13f, 0.7f, 4.3f)); return true;
            case 50: p = Mix(V(-0.6f, 1.4f, -2.2f), V(-0.5f, 1.5f, -1.9f), e); Aim(cam, p, V(0.4f, 1.5f, 3.0f), 24, 5.6f, V(0.4f, 1.5f, 2.7f)); return true;
            case 51: { Vector3 w4 = Win(4), w = Vector3.Lerp((w4 + Win(5)) * 0.5f, w4, e); p = w + V(Mathf.Lerp(-0.35f, -0.20f, e), -0.45f, -1.6f); Aim(cam, p, w, 40, 2.8f, w); return true; }      /* from the two windows to the one with the lamp */
            case 53: { Vector3 w = Win(4); p = w + V(-0.55f, -0.35f, -1.45f); p += (w - p).normalized * 0.05f * e; Aim(cam, p, w, 85, 2.0f, w + V(0, 0, 0.12f)); return true; }
            // ---------------- the struck stage
            case 54: Aim(cam, V(0, 1.5f, -2.4f), V(0, 1.8f, 3f), 24, 5.6f, V(0, 1.6f, 3f)); return true;
            case 55: p = E + V(0.55f, -0.15f, -2.55f); look = E + V(0.25f, -0.22f, 0); p += (look - p).normalized * 0.05f * e; Aim(cam, p, look, 50, 2.8f, E); return true;
            case 56: { Vector3 b = bulbGlow != null ? bulbGlow.bounds.center : V(2.4f, 1.535f, 5.02f); Vector3 d = (V(1.46f, 1.2f, 1.52f) - b).normalized, rt = Vector3.Cross(Vector3.up, d).normalized; p = b - d * (0.20f - 0.02f * e); Aim(cam, p, b + rt * 0.035f + V(0, -0.012f, 0), 35, 11f, b); return true; }      /* the bulb left of centre, the mark where the red sheet comes down right of it */
            case 57:
                {   // round the folding paper, a step of the turn with every fold
                    float turn = 0; foreach (float fb in new[] { 0f, 1f, 2f, 2.5f, 3f }) turn += S((t - B(61, fb)) / 0.22f); turn /= 5f;
                    p = PA + Quaternion.Euler(0, Mathf.Lerp(-20f, -60f, turn), 0) * V(0.0f, 0.22f, -0.75f);      /* round to the side away from the ghost lamp: its light on the boards stays behind the camera */ Aim(cam, p, PA + V(0, 0.04f, 0), 70, 2.8f, PA + V(0, 0.04f, 0)); return true;
                }
            // ---------------- the bird
            case 58: { Vector3 b = BirdNow(); p = V(b.x, Mathf.Max(0.35f, b.y * 0.8f + 0.1f), b.z) + Quaternion.Euler(0, Mathf.Lerp(12f, -8f, e), 0) * V(0, 0, -1.6f); Aim(cam, p, b + V(0, 0.08f, 0), 35, 2.8f, b); return true; }
            case 59: Aim(cam, V(0.2f, 1.7f, 1.4f), V(0.45f, 2.0f, 2.6f), 28, 2.8f, V(0.45f, 2.0f, 2.4f)); return true;
            case 60: { Vector3 b = BirdNow(); p = V(Mathf.Lerp(0.5f, 0.1f, e), 1.5f, -0.2f); Aim(cam, p, V(b.x * 0.6f, 1.9f, 2.7f), 35, 5.6f, V(0, 1.9f, 2.7f)); return true; }
            case 61: Fill(V(0.2f, 1.9f, 0.2f), new Color(0.55f, 0.68f, 1f), 0.8f, 3f); Aim(cam, V(0.33f, 1.52f, -1.0f), V(-0.7f, 1.72f, 1.6f), 40, 4f, V(-0.6f, 1.65f, 0.6f)); return true;
            case 62: { Vector3 b = BirdNow(); Aim(cam, V(0.0f, 1.5f, -2.6f), b, 50, 2.8f, b); return true; }
            case 63: { Vector3 b = BirdNow(); p = V(Mathf.Lerp(1.2f, 1.5f, e), 1.85f, 1.1f); Aim(cam, p, V(b.x * 0.5f + 0.75f, 2.0f, 2.63f), 50, 2.8f, b); return true; }
            case 64: { Vector3 h = HandNow(); Aim(cam, h + V(-0.35f, 0.15f, -0.85f), h + V(0.10f, 0.15f, 0.2f), 50, 2.8f, h); return true; }      /* the open hand low in the frame; the bird comes down into it from the top edge */
            case 65: { Vector3 h = HandNow(); look = Vector3.Lerp(E, h, 0.62f); p = E + V(-0.55f, -0.16f, -1.6f); p += (look - p).normalized * 0.05f * e; Aim(cam, p, look, 50, 2.8f, h); return true; }
            case 66: { Vector3 b = BirdNow(); p = b + V(-0.10f, 0.10f, -0.56f); look = b + V(0, -0.005f, 0); { Vector3 rt66 = Vector3.Cross(Vector3.up, look - p).normalized; p -= rt66 * 0.045f; look -= rt66 * 0.045f; } p += (look - p).normalized * 0.03f * e; Aim(cam, p, look, 50, 2.8f, b); return true; }      /* the whole bird from its side, on the fingers that hold it */
            // ---------------- they look up
            case 67: p = Mix(V(-0.1f, 1.3f, -1.7f), V(-0.1f, 1.3f, -1.5f), e); Aim(cam, p, V(0.0f, 1.25f, GROOVE_Z), 35, 5.6f, V(0.0f, 1.3f, GROOVE_Z)); return true;
            case 68: { Vector3 c = PersonAt(HYENA, 1.22f); p = V(c.x - 0.12f, 1.26f, 0.22f + 0.04f * e); Aim(cam, p, c, 50, 4f, c); return true; }      /* the hyena whole: its head, and the slip going down out of the light of its face */
            case 69: p = V(Mathf.Lerp(0.6f, 0.3f, e), 0.8f, -1.2f); Aim(cam, p, V(0.2f, 3.0f, 0.0f), 35, 4f, V(0, 2.9f, 0)); return true;
            case 70:
                {
                    Vector3 b = BirdNow(); float go = Ramp(t, B(75, 2), B(75, 3)); p = Mix(E + V(0.5f, -0.15f, -1.8f), E + V(0.6f, 0.05f, -2.2f), e);
                    Aim(cam, p, Mix(E + V(-0.05f, -0.2f, 0), (E + b) * 0.5f, go), 35, 2.8f, E); return true;
                }
            case 71: { Vector3 b = BirdNow(); Aim(cam, V(0.0f, 0.9f, -1.6f + 0.10f * e), V(0.0f, 2.6f, 0.10f), 35, 5.6f, V(0, 2.7f, 0.1f)); return true; }      /* from in front and below: the bird rises inside the opening to the line of bulbs over it */
            case 72: { Vector3 b = BirdNow(); p = Mix(V(0.95f, 1.35f, -1.75f), V(1.05f, 1.35f, -1.95f), e); Aim(cam, p, Vector3.Lerp(V(0.1f, 1.9f, 0.2f), b, 0.7f * (1 - S((u - 0.42f) / 0.36f))), 35, 5.6f, V(0.1f, 1.6f, 1.3f)); return true; }      /* the focus stays on the stage: when the bird has passed, the lit theatre is what is left */
            case 73: { float sg = 0; for (int b = 0; b < 4; b++) sg += Surge(t, B(78, b), 0.03f); p = Mix(V(0.38f, 1.5f, -3.0f), V(0.38f, 1.5f, -2.85f), e) + V(0, 0, sg); Aim(cam, p, V(0.2f, 1.5f, 2.5f), 24, 5.6f, V(0, 1.5f, 2.3f)); return true; }
            case 75: { Vector3 c = PersonAt(5, 1.42f); p = V(c.x - 0.98f, 1.12f, 1.50f); p += (c - p).normalized * 0.03f * e; Aim(cam, p, c + V(0, -0.14f, 0), 70, 4f, c + V(0, 0.05f, -0.12f)); return true; }      /* from a little below, so that what stands behind it is wall and sky, not a figure in a window */
            case 76: p = Mix(V(-0.28f, 2.0f, 3.65f), V(-0.22f, 2.1f, 3.65f), e); Aim(cam, p, V(0.14f, 1.15f, 1.3f), 35, 4f, V(0, 1.5f, GROOVE_Z)); return true;
            case 77: { Vector3 fw = singer != null ? singer.transform.forward : Vector3.back, rg = singer != null ? singer.transform.right : Vector3.right; Aim(cam, E + fw * 2.3f + rg * 0.06f + V(0, -0.02f, 0), E + V(0, -0.10f, 0), 70, 2.8f, E); return true; }
            case 78: Aim(cam, V(0.38f, 1.5f, -2.4f), V(0.4f, 1.7f, 3f), 28, 5.6f, V(0.5f, 1.7f, 2.7f)); return true;
            case 79: Fill(E + V(-0.2f, -0.5f, -0.9f), new Color(1f, 0.82f, 0.62f), 0.5f * Ramp(t, B(84), B(85)), 2.2f); p = E + V(-0.35f, 0.0f, -2.38f); look = E + V(-0.05f, -0.12f, 0); p += (look - p).normalized * 0.05f * e; Aim(cam, p, look, 70, 2.8f, E); return true;
            case 80: Aim(cam, V(0.35f, 0.30f, -1.55f), V(1.33f, 0.25f, -0.04f), 50, 4f, V(0.85f, 0.10f, -0.22f)); return true;
        }
        return false;
    }
}
