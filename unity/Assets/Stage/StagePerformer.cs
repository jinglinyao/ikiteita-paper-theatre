using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StageLipTrack
{
    public int fps; public int count; public float t0;
    public string[] names; public float[] w; public float[] open; public float[] openSlow; public float[] blinks;
}

/// The singer on the paper stage. One shot, one small pose: nothing here travels from pose to pose on screen, there is no
/// motion capture and no collision solving. A pose is a wrist target in front of the chest (never closer than a set clearance),
/// reached with two-bone IK, plus what a standing person never stops doing: breath, a slow shift of weight, a settle on arrival.
/// The face plays the baked lip track of the song (mouth, lids, brows); the eyes hold one target per shot.
/// Everything is a pure function of the film time, so any frame can be posed on its own.
[ExecuteAlways]
[DefaultExecutionOrder(30000)]
public class StagePerformer : MonoBehaviour
{
    public TextAsset lipJson; public Animator animator; public SkinnedMeshRenderer face;
    public float time; public bool live = true;
    [Tooltip("0 arms down, 1 right hand held in front of the chest")] public int pose = 1;
    [Tooltip("film time at which the pose is reached; the settle plays around it")] public float poseStart;
    public Transform gazeTarget;
    [Tooltip("the hand-keyed singing performance of the whole song (unity_mv). When set, it moves her; this script only adds what the film asks of her.")]
    public SingDriver driver;
    [Header("the model's look-at range (degrees of eye bone for a gaze 90 degrees off) and lids")]
    public float eyeIn = 8f, eyeOut = 12f, eyeDown = 12f, eyeUp = 13.25f, eyeReach = 2f, lidRest = 0.14f;
    [Header("right hand in front of the chest: wrist relative to the chest bone, in her own axes (x her right, z forward)")]
    public Vector3 chestHand = new Vector3(0.030f, 0.105f, 0.325f);
    public Vector3 chestElbow = new Vector3(0.90f, -0.46f, -0.06f);
    public Vector3 chestFingers = new Vector3(-0.82f, 0.44f, -0.36f), chestPalm = new Vector3(0.20f, -0.22f, -0.95f);
    [Range(0, 1)] public float curlRest = 0.38f, curlChest = 0.42f;

    /// One pose: where each wrist rests relative to the chest bone in her own axes (x her right, y up, z forward), which way the fingers
    /// and the palm point, how far the fingers curl; a turn and a nod of the head; how far the lids close; what she looks at.
    public struct Pose
    {
        public float rw, lw; public Vector3 rHand, rElbow, rFing, rPalm, lHand, lElbow, lFing, lPalm; public float rCurl, lCurl, yaw, pitch, tilt, lids; public int gaze;      // gaze: 0 the camera, 1 her right hand, 2 the bird, 3 away to her left
    }
    static Pose Down(float yaw = 0, float pitch = 0, float lids = 0, int gaze = 0) { return new Pose { rCurl = 0.38f, lCurl = 0.38f, yaw = yaw, pitch = pitch, lids = lids, gaze = gaze }; }
    public static readonly Pose[] POSES =
    {
        Down(),                                                                                                                                             // 0  arms down, looking at the camera
        new Pose { rw = 1, rHand = new Vector3(0.030f, 0.105f, 0.325f), rElbow = new Vector3(0.90f, -0.46f, -0.06f), rFing = new Vector3(-0.82f, 0.44f, -0.36f), rPalm = new Vector3(0.20f, -0.22f, -0.95f), rCurl = 0.42f, lCurl = 0.38f, tilt = 2.2f },   // 1  right hand on the chest
        new Pose { rw = 1, lw = 1, rHand = new Vector3(0.040f, -0.255f, 0.205f), rElbow = new Vector3(0.85f, -0.50f, -0.10f), rFing = new Vector3(-0.86f, -0.42f, 0.28f), rPalm = new Vector3(0.10f, 0.25f, -0.96f), rCurl = 0.50f,
                   lHand = new Vector3(-0.050f, -0.275f, 0.225f), lElbow = new Vector3(-0.85f, -0.50f, -0.10f), lFing = new Vector3(0.86f, -0.40f, 0.20f), lPalm = new Vector3(-0.10f, 0.25f, -0.96f), lCurl = 0.50f, pitch = 2f },             // 2  hands clasped below the waist line
        Down(-22f, 3f, 0, 3),                                                                                                                               // 3  arms down, the face turned a little aside
        new Pose { rw = 1, rHand = new Vector3(0.130f, -0.060f, 0.300f), rElbow = new Vector3(0.55f, -0.80f, -0.20f), rFing = new Vector3(-0.35f, 0.04f, 0.93f), rPalm = new Vector3(0.0f, 1.0f, 0.10f), rCurl = 0.14f, lCurl = 0.38f, pitch = 9f, gaze = 1 },      // 4  the open right hand held under the chest, palm up: the bird sits in it
        new Pose { rw = 1, rHand = new Vector3(0.150f, 0.050f, 0.365f), rElbow = new Vector3(0.55f, -0.78f, -0.15f), rFing = new Vector3(-0.25f, 0.22f, 0.94f), rPalm = new Vector3(0.0f, 0.98f, -0.18f), rCurl = 0.08f, lCurl = 0.38f, pitch = -7f, gaze = 2 },    // 5  the same hand a hand's breadth higher: the bird leaves it
        Down(),                                                                                                                                             // 6  arms down, straight at the camera
        Down(0f, 13f, 1f, 0),                                                                                                                               // 7  the head lowered, the eyes closed
    };
    // from this bar and beat on, this pose, reached over so many seconds (she is off stage between her scenes, so most changes are not seen)
    struct PK { public float t; public int id; public float fade; public PK(float bar, float beat, int id, float fade) { t = FilmBars.At(bar, beat); this.id = id; this.fade = fade; } }
    static readonly PK[] POSE_TRACK = { new PK(1, 0, 0, 0f), new PK(26, 0, 1, 0f), new PK(49, 0, 2, 0f), new PK(58, 0, 3, 0f), new PK(68, 0, 4, 0f), new PK(75, 2, 5, 0.5f), new PK(76, 2, 0, 1.6f), new PK(80, 0, 6, 0f), new PK(84, 2, 7, 1.8f) };
    public static void PoseAt(float t, out int a, out int b, out float mix, out float since)
    {
        int i = 0; while (i + 1 < POSE_TRACK.Length && t >= POSE_TRACK[i + 1].t) i++;
        b = POSE_TRACK[i].id; a = i > 0 ? POSE_TRACK[i - 1].id : b; since = t - POSE_TRACK[i].t; float u = POSE_TRACK[i].fade <= 0 ? 1f : Mathf.Clamp01(since / POSE_TRACK[i].fade); mix = u * u * (3 - 2 * u);
    }
    [NonSerialized] public int forcePose = -1;                                 // an editor still can ask for one pose outright
    [NonSerialized] Transform birdT;

    [SerializeField] List<int> restBone = new List<int>();
    [SerializeField] List<Quaternion> restLocal = new List<Quaternion>();
    [SerializeField] List<Quaternion> restRoot = new List<Quaternion>();        // each bone's rotation in the character's own frame, T-pose
    [SerializeField] Vector3 restHips;

    [NonSerialized] StageLipTrack track; [NonSerialized] int[] shapeIndex; [NonSerialized] int lidIndex = -1;
    readonly Dictionary<int, int> slot = new Dictionary<int, int>();

    public void CaptureRest()
    {
        restBone.Clear(); restLocal.Clear(); restRoot.Clear(); slot.Clear();
        foreach (HumanBodyBones b in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (b == HumanBodyBones.LastBone) continue;
            var t = animator.GetBoneTransform(b); if (t == null) continue;
            restBone.Add((int)b); restLocal.Add(t.localRotation); restRoot.Add(Quaternion.Inverse(transform.rotation) * t.rotation);
        }
        restHips = animator.GetBoneTransform(HumanBodyBones.Hips).localPosition;
    }

    void Load()
    {
        if (slot.Count == 0) for (int i = 0; i < restBone.Count; i++) slot[restBone[i]] = i;
        if (track == null && lipJson != null && face != null)
        {
            track = JsonUtility.FromJson<StageLipTrack>(lipJson.text);
            var mesh = face.sharedMesh; shapeIndex = new int[track.names.Length];
            for (int i = 0; i < shapeIndex.Length; i++) shapeIndex[i] = mesh.GetBlendShapeIndex(track.names[i]);
            lidIndex = mesh.GetBlendShapeIndex("Fcl_EYE_Close");
        }
    }

    void Update() { if (Application.isPlaying && live) ApplyBones(time); }
    void LateUpdate() { if (Application.isPlaying && live) ApplyFace(time); }      // after the VRM expression and look-at pass (order 11000)
    public void Apply(float t) { time = t; ApplyBones(t); ApplyFace(t); }

    Transform B(HumanBodyBones b) => animator.GetBoneTransform(b);
    Quaternion Rest(HumanBodyBones b) => slot.TryGetValue((int)b, out int i) ? restLocal[i] : Quaternion.identity;
    Quaternion RestRoot(HumanBodyBones b) => slot.TryGetValue((int)b, out int i) ? restRoot[i] : Quaternion.identity;
    Vector3 Dir(Vector3 own) => transform.TransformDirection(own.normalized);

    float Sample(float[] a, int stride, int ch, float t)
    {
        float f = Mathf.Clamp(t * track.fps, 0, track.count - 1);
        int i = Mathf.FloorToInt(f), j = Mathf.Min(i + 1, track.count - 1);
        return Mathf.Lerp(a[i * stride + ch], a[j * stride + ch], f - i);
    }

    void Aim(HumanBodyBones b, HumanBodyBones child, Vector3 worldDir)
    {
        var t = B(b); var c = B(child); if (t == null || c == null) return;
        t.rotation = Quaternion.FromToRotation((c.position - t.position).normalized, worldDir.normalized) * t.rotation;
    }

    void Curl(bool right, float amount, float thumb)
    {
        float sg = right ? -1f : 1f; float[] ang = { 34f, 46f, 30f };
        HumanBodyBones[] prox = right
            ? new[] { HumanBodyBones.RightIndexProximal, HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightRingProximal, HumanBodyBones.RightLittleProximal }
            : new[] { HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftLittleProximal };
        float[] more = { 0.80f, 0.95f, 1.08f, 1.20f };                          // the little finger closes more than the index: no flat, rake-like hand
        for (int f = 0; f < 4; f++)
            for (int k = 0; k < 3; k++)
            {
                var bone = (HumanBodyBones)((int)prox[f] + k); var t = B(bone); if (t == null) continue;
                t.localRotation = Rest(bone) * Quaternion.AngleAxis(sg * ang[k] * amount * more[f], Vector3.forward);
            }
        var th = right ? HumanBodyBones.RightThumbProximal : HumanBodyBones.LeftThumbProximal;
        for (int k = 0; k < 3; k++)
        {
            var bone = (HumanBodyBones)((int)th + k); var t = B(bone); if (t == null) continue;
            t.localRotation = Rest(bone) * Quaternion.AngleAxis(-sg * (k == 0 ? 16f : 12f) * thumb, Vector3.up);
        }
    }

    // ---- the film's own demands on top of the keyed performance: the eyes on the bird in her hand, after it when it leaves, shut at the end
    static float SS(float a, float b, float x) { x = Mathf.Clamp01((x - a) / Mathf.Max(1e-5f, b - a)); return x * x * (3 - 2 * x); }
    public static float HoldW(float t) { float b68 = FilmBars.At(68), rel = FilmBars.At(75, 2); return Mathf.Max(SS(b68 - 0.4f, b68, t) * (1 - SS(b68 + 5.0f, b68 + 6.5f, t)), SS(rel - 1.5f, rel - 0.9f, t)) * (1 - SS(rel, rel + 0.35f, t)); }      // the bird sits in her hand
    public static float AfterW(float t) { return SS(FilmBars.At(75, 2), FilmBars.At(75, 2) + 0.35f, t) * (1 - SS(FilmBars.At(76, 2), FilmBars.At(76, 2) + 1.6f, t)); }   // her eyes go after it
    public static float ShutW(float t) { return SS(FilmBars.At(84, 2), FilmBars.At(84, 2) + 1.8f, t); }
    void DriveBones(float t)
    {
        driver.stageOrigin = transform.position;
        if (gazeTarget != null) { driver.viewerW = gazeTarget.position; driver.hasViewer = true; }
        driver.ApplyBones(t);
        float hw = HoldW(t), aw = AfterW(t), fw = SS(175.2f, 176.2f, t) * (1 - SS(178.3f, 179.3f, t));
        if (fw > 0.001f && gazeTarget != null) { driver.eyeTgtW = Vector3.Lerp(driver.eyeTgtW, gazeTarget.position, fw); driver.micLookNow *= 1 - fw; }      /* the last line, straight at the lens */
        if (hw > 0.001f) { var h = B(HumanBodyBones.RightHand); if (h != null) { driver.eyeTgtW = Vector3.Lerp(driver.eyeTgtW, h.position + Vector3.up * 0.05f, hw); driver.micLookNow *= 1 - hw; } }
        if (aw > 0.001f)
        {
            if (birdT == null) { var g = GameObject.Find("Bird"); if (g != null) birdT = g.transform; }
            Vector3 tg = birdT != null && birdT.gameObject.activeInHierarchy ? birdT.position : transform.position + transform.TransformVector(new Vector3(0.2f, 2.4f, 1.5f));
            driver.eyeTgtW = Vector3.Lerp(driver.eyeTgtW, tg, aw); driver.micLookNow *= 1 - aw;
        }
    }

    public void ApplyBones(float t)
    {
        Load(); if (animator == null || restBone.Count == 0) return;
        if (driver != null) { DriveBones(t); return; }
        for (int i = 0; i < restBone.Count; i++) { var b = B((HumanBodyBones)restBone[i]); if (b != null) b.localRotation = restLocal[i]; }
        var hips = B(HumanBodyBones.Hips);
        float slow = track != null && track.openSlow != null && track.openSlow.Length >= track.count ? Sample(track.openSlow, 1, 0, t) : 0f;
        float breath = Mathf.Sin(t * 2f * Mathf.PI / 3.4f), sway = Mathf.Sin(t * 2f * Mathf.PI / 7.3f) + 0.45f * Mathf.Sin(t * 2f * Mathf.PI / 3.1f + 1.0f);

        // standing: the weight drifts a few millimetres from foot to foot, the spine answers, the chest rises with the breath and with the phrase
        hips.localPosition = restHips + new Vector3(0.005f * sway, -0.0015f * Mathf.Abs(sway), 0);
        hips.localRotation = Rest(HumanBodyBones.Hips) * Quaternion.Euler(0, 0, -0.7f * sway);
        var spine = B(HumanBodyBones.Spine); if (spine != null) spine.localRotation = Rest(HumanBodyBones.Spine) * Quaternion.Euler(0.4f, 0, 0.5f * sway);
        var chest = B(HumanBodyBones.Chest); if (chest != null) chest.localRotation = Rest(HumanBodyBones.Chest) * Quaternion.Euler(-0.55f * breath - 0.9f * slow, 0, 0.25f * sway);
        var upper = B(HumanBodyBones.UpperChest); if (upper != null) upper.localRotation = Rest(HumanBodyBones.UpperChest) * Quaternion.Euler(-0.35f * breath - 0.5f * slow, 0, 0);
        foreach (var sh in new[] { HumanBodyBones.LeftShoulder, HumanBodyBones.RightShoulder })
        { var s = B(sh); if (s != null) s.localRotation = Rest(sh) * Quaternion.Euler(0, 0, (sh == HumanBodyBones.LeftShoulder ? -1f : 1f) * (1.2f + 0.5f * breath)); }

        // arms down, a little away from the body and a little bent
        for (int side = 0; side < 2; side++)
        {
            bool r = side == 0; float sg = r ? 1f : -1f;
            Aim(r ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm, r ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm, Dir(new Vector3(sg * (r ? 0.10f : 0.13f), -0.99f, r ? 0.07f : 0.02f)));
            Aim(r ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm, r ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand, Dir(new Vector3(sg * (r ? 0.03f : 0.06f), -0.96f, r ? 0.27f : 0.20f)));
        }
        // the pose of this moment: a blend of the one she comes from and the one she goes to
        int pa, pb; float mix, since; PoseAt(t, out pa, out pb, out mix, out since); if (forcePose >= 0) { pa = pb = forcePose; mix = 1f; since = 10f; }
        Pose A = POSES[pa], Bp = POSES[pb];
        float settle = pb == 1 ? Mathf.Exp(-4.5f * (since + 0.5f)) * Mathf.Cos(5.5f * (since + 0.5f)) : 0f;      // the damped arrival of the hand on the chest
        Vector3 drift = new Vector3(0.003f * Mathf.Sin(t * 1.9f + 0.4f), 0.003f * Mathf.Sin(t * 1.3f + 2.1f), 0.002f * Mathf.Sin(t * 2.3f));
        for (int side = 0; side < 2; side++)
        {
            bool r = side == 0; float w0 = r ? A.rw : A.lw, w1 = r ? Bp.rw : Bp.lw, w = Mathf.Lerp(w0, w1, mix);
            Curl(r, Mathf.Lerp(r ? A.rCurl : A.lCurl, r ? Bp.rCurl : Bp.lCurl, mix), w > 0.5f ? 0.35f : 0.5f);
            if (w <= 0.001f || chest == null) continue;
            Vector3 h0 = r ? A.rHand : A.lHand, h1 = r ? Bp.rHand : Bp.lHand; Vector3 own = w0 > 0 && w1 > 0 ? Vector3.Lerp(h0, h1, mix) : w1 > 0 ? h1 : h0;
            Vector3 pole = w1 > 0 ? (r ? Bp.rElbow : Bp.lElbow) : (r ? A.rElbow : A.lElbow), fg = w1 > 0 ? (r ? Bp.rFing : Bp.lFing) : (r ? A.rFing : A.lFing), pm = w1 > 0 ? (r ? Bp.rPalm : Bp.lPalm) : (r ? A.rPalm : A.lPalm);
            if (w0 > 0 && w1 > 0) { fg = Vector3.Slerp(r ? A.rFing : A.lFing, fg, mix); pm = Vector3.Slerp(r ? A.rPalm : A.lPalm, pm, mix); pole = Vector3.Slerp(r ? A.rElbow : A.lElbow, pole, mix); }
            own += drift + (r ? settle * new Vector3(0.035f, -0.060f, 0.015f) : Vector3.zero);
            var ua = B(r ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm); var la = B(r ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm); var hd = B(r ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            Vector3 hang = hd.position; Quaternion hangRot = hd.rotation;
            Vector3 W = Vector3.Lerp(hang, chest.position + transform.TransformVector(own), w);
            Vector3 S = ua.position; float L1 = Vector3.Distance(ua.position, la.position), L2 = Vector3.Distance(la.position, hd.position);
            Vector3 n = W - S; float d = Mathf.Min(n.magnitude, L1 + L2 - 0.004f); n.Normalize(); W = S + n * d;
            float aa = (L1 * L1 - L2 * L2 + d * d) / (2f * d), hh = Mathf.Sqrt(Mathf.Max(0f, L1 * L1 - aa * aa));
            Vector3 pp = Dir(pole); pp = (pp - n * Vector3.Dot(pp, n)).normalized; Vector3 E = S + n * aa + pp * hh;
            Aim(r ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm, r ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm, E - S);
            Aim(r ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm, r ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand, W - E);
            Vector3 f = Dir(fg), pn = Dir(pm); pn = (pn - f * Vector3.Dot(pn, f)).normalized;
            Quaternion q0 = Quaternion.LookRotation(r ? transform.right : -transform.right, transform.up), q1 = Quaternion.LookRotation(f, -pn);
            Quaternion target = q1 * Quaternion.Inverse(q0) * (transform.rotation * RestRoot(r ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand));
            hd.rotation = Quaternion.Slerp(hangRot, target, w);
        }
        float poseYaw = Mathf.Lerp(A.yaw, Bp.yaw, mix), posePitch = Mathf.Lerp(A.pitch, Bp.pitch, mix), poseTilt = Mathf.Lerp(A.tilt, Bp.tilt, mix); poseLids = Mathf.Lerp(A.lids, Bp.lids, mix); poseGaze = mix > 0.5f ? Bp.gaze : A.gaze;

        // the head: part of the way to whoever she sings to, a small lift with the phrase, never still, never busy
        var neck = B(HumanBodyBones.Neck); var head = B(HumanBodyBones.Head);
        float yaw = 0, pitch = 0;
        if (gazeTarget != null && head != null)
        {
            Vector3 dl = transform.InverseTransformDirection(GazePoint() - head.position);
            yaw = Mathf.Clamp(Mathf.Atan2(dl.x, dl.z) * Mathf.Rad2Deg * 0.38f, -14f, 14f);
            pitch = Mathf.Clamp(-Mathf.Atan2(dl.y, new Vector2(dl.x, dl.z).magnitude) * Mathf.Rad2Deg * (poseGaze == 2 ? 0.55f : 0.30f), poseGaze == 2 ? -18f : -6f, 6f);      /* when the bird leaves her hand her face goes up after it */
        }
        float nod = -1.3f * slow + 0.5f * Mathf.Sin(t * 2f * Mathf.PI / 5.2f + 0.7f), turn = 0.9f * Mathf.Sin(t * 2f * Mathf.PI / 6.1f + 2.0f), tilt = poseTilt + 0.6f * sway; yaw += poseYaw; pitch += posePitch;
        if (neck != null) neck.localRotation = Rest(HumanBodyBones.Neck) * Quaternion.Euler(0.4f * (pitch + nod), 0.4f * (yaw + turn), 0.4f * tilt);
        if (head != null) head.localRotation = Rest(HumanBodyBones.Head) * Quaternion.Euler(0.6f * (pitch + nod), 0.6f * (yaw + turn), 0.6f * tilt);
    }

    [NonSerialized] float poseLids; [NonSerialized] int poseGaze;
    /// what she looks at in the pose of the moment
    Vector3 GazePoint()
    {
        if (poseGaze == 1) { var h = B(HumanBodyBones.RightHand); if (h != null) return h.position + Vector3.up * 0.04f; }
        if (poseGaze == 2) { if (birdT == null) { var g = GameObject.Find("Bird"); if (g != null) birdT = g.transform; } if (birdT != null && birdT.gameObject.activeInHierarchy) return birdT.position; return transform.position + transform.TransformVector(new Vector3(0.2f, 2.4f, 1.5f)); }
        if (poseGaze == 3) return transform.position + transform.TransformVector(new Vector3(-1.6f, 1.25f, 1.6f));
        return gazeTarget != null ? gazeTarget.position : transform.position + transform.forward * 2f + Vector3.up * 1.4f;
    }

    public void ApplyFace(float t)
    {
        Load();
        if (driver != null)
        {
            driver.ApplyFace(t);
            float sw = ShutW(t); if (sw > 0.001f && lidIndex >= 0 && face != null) face.SetBlendShapeWeight(lidIndex, Mathf.Max(face.GetBlendShapeWeight(lidIndex), 100f * sw));
            return;
        }
        if (track == null || face == null) return;
        int n = track.names.Length;
        for (int c = 0; c < n; c++) if (shapeIndex[c] >= 0) face.SetBlendShapeWeight(shapeIndex[c], 100f * Sample(track.w, n, c, t));
        if (lidIndex >= 0) face.SetBlendShapeWeight(lidIndex, Mathf.Max(face.GetBlendShapeWeight(lidIndex), 100f * Mathf.Max(lidRest, poseLids)));
        var hdT = B(HumanBodyBones.Head); if (hdT == null || gazeTarget == null) return;
        var le = B(HumanBodyBones.LeftEye); var re = B(HumanBodyBones.RightEye); if (le == null || re == null) return;
        Vector3 between = (le.position + re.position) * 0.5f;
        Vector3 dl = Quaternion.Inverse(hdT.rotation) * (GazePoint() - between);
        float ga = Mathf.Atan2(dl.x, dl.z) * Mathf.Rad2Deg, gp = -Mathf.Atan2(dl.y, new Vector2(dl.x, dl.z).magnitude) * Mathf.Rad2Deg;
        foreach (var eb in new[] { HumanBodyBones.LeftEye, HumanBodyBones.RightEye })
        {
            // gaze angle -> eye-bone angle by the model's own look-at range, taken twice as strongly so the look reads, never past 90 % of the limits
            bool outward = (ga > 0f) == (eb == HumanBodyBones.RightEye);
            float limY = (outward ? eyeOut : eyeIn) * 0.9f, limP = (gp > 0f ? eyeDown : eyeUp) * 0.9f;
            float ya = Mathf.Clamp(ga * (outward ? eyeOut : eyeIn) / 90f * eyeReach, -limY, limY), pa = Mathf.Clamp(gp * (gp > 0f ? eyeDown : eyeUp) / 90f * eyeReach, -limP, limP);
            B(eb).localRotation = Rest(eb) * Quaternion.Euler(pa, ya, 0);
        }
    }
}
