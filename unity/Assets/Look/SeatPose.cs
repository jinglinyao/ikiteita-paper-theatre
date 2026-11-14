using UnityEngine;

/// The gesture rig (SingDriver) poses her standing. This sits her down afterwards: pelvis onto the chair, thighs forward,
/// shins down, feet flat, the trunk leaning in a little, the head turned to what she is watching.
/// In play mode it runs right after SingDriver.Update and before the VRM spring bones (hair, coat), so they react to it.
[DefaultExecutionOrder(30001)]
public class SeatPose : MonoBehaviour
{
    public SingDriver driver;
    public Transform lookTarget;
    [Range(0, 1)] public float headBlend = 0.7f;
    public static string lastNote = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void EnsureLap()
    {
        if (SingDriver.Library.ContainsKey("lap")) return;            // hands resting on her thighs (the library only has standing rest poses)
        float[] soft = { 0.14f, 0.18f, 0.22f, 0.27f, 0.32f };
        var g = new SingDriver.Gesture { beat = 0f, snap = 0.3f };
        g.R = new SingDriver.HandPose(SingDriver.Anchor.Hips, new Vector3(0.088f, 0.035f, 0.235f), new Vector3(0.9f, -0.5f, -0.5f), new Vector3(-0.16f, -0.16f, 0.97f), new Vector3(0f, -1f, 0.05f), soft, 0.1f);
        g.L = new SingDriver.HandPose(SingDriver.Anchor.Hips, new Vector3(0.080f, 0.040f, 0.205f), new Vector3(0.9f, -0.5f, -0.5f), new Vector3(-0.22f, -0.16f, 0.96f), new Vector3(0f, -1f, 0.05f), soft, 0.1f);
        SingDriver.Library["lap"] = g;
    }

    void Update() { if (Application.isPlaying && driver != null) Seat(driver, lookTarget != null ? lookTarget.position : driver.transform.position + driver.transform.forward * 1.3f + Vector3.up * 0.8f, headBlend); }

    static void Aim(Transform bone, Vector3 childPos, Vector3 dir)
    {
        Vector3 cur = (childPos - bone.position).normalized; bone.rotation = Quaternion.FromToRotation(cur, dir.normalized) * bone.rotation;
    }

    public static void Seat(SingDriver d, Vector3 look, float headBlend = 0.7f)
    {
        var a = d.animator; var root = d.transform; Vector3 fwd = root.forward, up = Vector3.up, right = root.right;
        var hips = a.GetBoneTransform(HumanBodyBones.Hips);
        var uL = a.GetBoneTransform(HumanBodyBones.LeftUpperLeg); var lL = a.GetBoneTransform(HumanBodyBones.LeftLowerLeg); var fL = a.GetBoneTransform(HumanBodyBones.LeftFoot);
        float L1 = Vector3.Distance(uL.position, lL.position), L2 = Vector3.Distance(lL.position, fL.position);
        const float ANKLE = 0.085f, SLOPE = 5f, SHIN = 9f;
        float hipJoint = ANKLE + L2 * Mathf.Cos(SHIN * Mathf.Deg2Rad) + L1 * Mathf.Sin(SLOPE * Mathf.Deg2Rad);
        float hipToJoint = hips.position.y - uL.position.y;                                        // the pelvis bone sits a little above the hip joints
        hips.position = new Vector3(root.position.x, root.position.y + hipJoint + hipToJoint, root.position.z) - fwd * 0.02f;
        hips.rotation = Quaternion.AngleAxis(7f, right) * hips.rotation;                           // leaning toward the screen
        for (int side = 0; side < 2; side++)
        {
            bool r = side == 0; float sg = r ? 1f : -1f;
            var u = a.GetBoneTransform(r ? HumanBodyBones.RightUpperLeg : HumanBodyBones.LeftUpperLeg); var l = a.GetBoneTransform(r ? HumanBodyBones.RightLowerLeg : HumanBodyBones.LeftLowerLeg);
            var f = a.GetBoneTransform(r ? HumanBodyBones.RightFoot : HumanBodyBones.LeftFoot); var toe = a.GetBoneTransform(r ? HumanBodyBones.RightToes : HumanBodyBones.LeftToes);
            Aim(u, l.position, fwd * Mathf.Cos(SLOPE * Mathf.Deg2Rad) - up * Mathf.Sin(SLOPE * Mathf.Deg2Rad) + right * sg * 0.05f);
            Aim(l, f.position, -up * Mathf.Cos(SHIN * Mathf.Deg2Rad) + fwd * Mathf.Sin(SHIN * Mathf.Deg2Rad) * (r ? 1f : 0.4f) - right * sg * 0.03f);   // one foot a little ahead of the other
            if (toe != null) Aim(f, toe.position, fwd - up * 0.04f + right * sg * 0.10f);
        }
        var head = a.GetBoneTransform(HumanBodyBones.Head);
        if (head != null) { Vector3 to = look - head.position; head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(to, up), headBlend); }
        lastNote = $"thigh {L1:F3} shin {L2:F3} hip joint height {hipJoint:F3} head {head.position:F2}";
    }
}
