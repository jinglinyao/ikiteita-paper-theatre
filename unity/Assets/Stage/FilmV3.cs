using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Version 3: what the song is about, shown. A paper doll of the girl stands on the roof of the right-hand house when the set is built,
/// falls when the street lamp clicks on (bar 12), lies on the crossing with red paper coming out from under her head, and is dragged off
/// when the black hand takes the red sheet away (bar 38). Also: beams in the air under the lanterns, the singer's key and floor bounce,
/// and the pictures on the speech cards and in the far windows (they are children of the cards and panes; nothing to drive).
/// </summary>
public partial class FilmClock
{
    [System.NonSerialized] bool v3Bound; Transform girl, blood; Transform[] scraps; readonly List<KeyValuePair<Light, Renderer>> beams = new List<KeyValuePair<Light, Renderer>>(); Light linKey, poolBounce; Transform stageT;
    static readonly Vector3 ROOF = new Vector3(1.25f, 2.76f, 2.84f), LIE = new Vector3(2.14f, 0.012f, 1.64f);      // her feet: on the roof edge; on the street (she lies head towards the red sheet)
    public static float FallT0 => FilmBars.At(12) + 0.12f;
    const float FALL = 0.92f;

    public int SoloFar => soloFar; public Renderer[] FarPanes => farPanes; Renderer[] paneArt;
    void V3Bind()
    {
        if (farPanes != null) { paneArt = new Renderer[farPanes.Length]; for (int j = 0; j < farPanes.Length; j++) { var a = farPanes[j] != null ? farPanes[j].transform.Find("PaneArt") : null; paneArt[j] = a != null ? a.GetComponent<Renderer>() : null; } }
        v3Bound = true; var stage = GameObject.Find("Stage"); if (stage == null) return; stageT = stage.transform; beams.Clear();
        foreach (var tr in stage.GetComponentsInChildren<Transform>(true)) { if (tr.name == "PaperGirl") girl = tr; else if (tr.name == "GirlBlood") blood = tr; }
        if (blood != null) { scraps = new Transform[blood.childCount]; for (int k = 0; k < scraps.Length; k++) scraps[k] = blood.GetChild(k); }
        foreach (var l in stage.GetComponentsInChildren<Light>(true))
        {
            if (l.name == "LinKey") linKey = l; else if (l.name == "PoolBounce") poolBounce = l;
            var b = l.transform.Find("Beam"); if (b != null) beams.Add(new KeyValuePair<Light, Renderer>(l, b.GetComponent<Renderer>()));
        }
    }

    void V3(float t, float tq, float her)
    {
        if (!v3Bound || stageT == null) V3Bind(); if (stageT == null) return;
        // ---------------- the girl
        if (girl != null)
        {
            float up = B(10, 3), t0 = FallT0, t1 = t0 + FALL, gone0 = B(37, 2.5f), gone1 = B(38, 1.3f);
            bool on = t >= up && t < gone1; Show(girl, on); Show(blood, on && t >= t1);
            if (on)
            {
                if (tq < t0)
                {   // on the edge of the roof, her back to the street lamp that is not lit yet; she sways very little
                    girl.localPosition = ROOF; girl.localRotation = Quaternion.Euler(0, 0, 1.5f * Mathf.Sin(tq * 1.1f));
                }
                else if (tq < t1)
                {   // the fall, on twos like all the paper: out from the wall, turning over once
                    float u = (tq - t0) / FALL, y = Mathf.Lerp(ROOF.y, 0.55f, u * u);
                    Vector3 p = Vector3.Lerp(ROOF, new Vector3(1.50f, 0, 2.05f), Mathf.Pow(u, 0.8f)); p.y = y;
                    girl.localPosition = p; girl.localRotation = Quaternion.Euler(-12f * u, 10f * u, -165f * u * u);      /* she turns over in the plane of the paper, so the card never goes edge-on to the house */
                }
                else
                {   // on the crossing, face up, head towards the red sheet; dragged off to the wings when the hand takes the red away
                    float drag = In2((t - gone0) / (gone1 - gone0));      /* she goes as the red sheet is lifted, not in a blink */
                    girl.localPosition = LIE + new Vector3(4.2f * drag, 0, 0.1f * drag); girl.localRotation = Quaternion.Euler(0, -90f, 0) * Quaternion.Euler(90f, 0, 0) * Quaternion.Euler(0, 0, 4f);
                    if (scraps != null)
                    {
                        Vector3 head = LIE + new Vector3(-1.36f, -0.006f, 0);
                        Vector3[] dir = { new Vector3(-0.16f, 0, 0.10f), new Vector3(-0.05f, 0, -0.17f), new Vector3(0.10f, 0, 0.17f), new Vector3(-0.25f, 0, -0.06f), new Vector3(0.16f, 0, -0.15f) };
                        for (int k = 0; k < scraps.Length; k++)
                        {   // one scrap on each beat after she lands, each sliding a little further out (on twos)
                            float a = t1 + 0.25f + 0.54f * k, w = Mathf.Clamp01((tq - a) / 0.5f); w = 1 - (1 - w) * (1 - w);
                            Show(scraps[k], tq >= a); scraps[k].localPosition = head + dir[k % dir.Length] * (0.35f + 0.65f * w) + new Vector3(4.2f * drag, 0.0006f * k, 0);
                        }
                    }
                }
            }
        }
        // ---------------- the pictures in the far windows burn with their windows
        for (int j = 0; paneArt != null && j < paneArt.Length; j++)
        {
            if (paneArt[j] == null) continue; float k = FarPane(j, paneArt.Length, t);
            paneArt[j].GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", Color.white * (1.25f * k)); mpb.SetColor("_BaseColor", Color.white * (0.04f + 0.25f * k)); paneArt[j].SetPropertyBlock(mpb);
        }
        // ---------------- the cloud that hangs in front of the roof is drawn aside while she stands there and falls (between two shots that do not show it)
        if (cloud != null)
        {
            float cw = Ramp(t, 22.9f, 24.6f) * (1 - Ramp(t, 27.3f, 29.0f));
            if (cw > 0.001f) foreach (var c in cloud) { if (c == null) continue; var lp = c.localPosition; if (lp.x > 0.2f && lp.x < 2.9f) { lp.x += 2.1f * cw; c.localPosition = lp; } }
        }
        // ---------------- light in the air
        foreach (var kv in beams)
        {
            Light l = kv.Key; Renderer r = kv.Value; if (l == null || r == null) continue;
            float b0 = BASE.TryGetValue(l.name, out float bb) ? bb : 1f, lv = l.enabled ? l.intensity / Mathf.Max(1e-4f, b0) : 0f;
            float k = l.name == "Pool" ? 0.026f : l.name == "FollowSpot" ? 0.028f : l.name == "LampLight" ? 0.030f : l.name == "BackCool" ? 0.024f : 0.008f;
            float gain = k * Mathf.Min(lv, 1.2f) * (l.name == "Pool" ? Mathf.Clamp01((l.spotAngle - 4f) / 8f) : 1f);      /* closed down to the size of a sheet of paper the pool is a pin of light, not a column of haze over what it shows */ bool onB = gain > 0.0015f; if (r.enabled != onB) r.enabled = onB; if (!onB) continue;
            if (l.name != "LampLight") { float len = r.transform.localScale.z, rad = len * Mathf.Tan(Mathf.Clamp(l.spotAngle, 2f, 80f) * 0.5f * Mathf.Deg2Rad); r.transform.localScale = new Vector3(rad, rad, len); }
            r.GetPropertyBlock(mpb); mpb.SetFloat("_Gain", gain); mpb.SetFloat("_Fall", 2.3f); mpb.SetFloat("_T", t); mpb.SetColor("_Color", l.color); r.SetPropertyBlock(mpb);
        }
        // ---------------- her key from the front, and the pool's bounce off the boards
        if (linKey != null)
        {
            linKey.transform.rotation = Quaternion.LookRotation(stageT.TransformPoint(markNow + new Vector3(0, 1.30f, 0)) - linKey.transform.position);
            linKey.intensity = 26f * her; linKey.enabled = her > 0.01f;
        }
        if (poolBounce != null && lights != null && lights.TryGetValue("Pool", out var pool))
        {
            Vector3 hit = pool.transform.position + pool.transform.forward * (pool.transform.position.y - stageT.position.y) / Mathf.Max(0.05f, -pool.transform.forward.y); hit.y = stageT.position.y + 0.55f;
            poolBounce.transform.position = hit; poolBounce.intensity = pool.enabled && pool.spotAngle > 12f ? Mathf.Min(0.45f, pool.intensity * 0.006f) : 0f;      /* only under the wide pool she stands in: on the paper alone it bleached the red */ poolBounce.enabled = poolBounce.intensity > 0.01f;
        }
    }
}
