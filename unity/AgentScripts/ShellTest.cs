using System.IO; using System.Linq; using System.Text; using System.Collections.Generic; using UnityEngine; using UnityEngine.Rendering; using UnityEngine.Rendering.Universal; using UnityEditor; using UnityEditor.SceneManagement;
public static class ShellTest
{
    static readonly HashSet<string> SCENERY = new HashSet<string> { "material_2", "material_4", "material_5", "material_6", "material_7", "material_8", "material_9", "material_10", "material_11", "material_12", "material_13", "material_18", "material_19", "material_20", "material_21" };
    static readonly HashSet<string> STANDS = new HashSet<string> { "Object_114", "Object_131", "Object_278", "Object_89", "Object_267", "Object_285", "Object_86", "Object_121", "Object_208" };
    public static string Run()
    {
        var sb = new StringBuilder();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.45f);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/sf2_toy_theatre_1913/scene.gltf"));
        int off = 0; foreach (var r in go.GetComponentsInChildren<Renderer>()) if (SCENERY.Contains(r.sharedMaterial.name) || STANDS.Contains(r.name)) { r.gameObject.SetActive(false); off++; }
        sb.AppendLine("hidden " + off);
        // the opening of the proscenium: cast rays through the front sheet on a grid and see where they pass
        var front = go.GetComponentsInChildren<Renderer>(true).First(r => r.name == "Object_11"); var mf = front.GetComponent<MeshFilter>();
        var mc = front.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; Physics.SyncTransforms();
        float x0 = 9, x1 = -9, y0 = 9, y1 = -9;
        for (float x = -0.29f; x <= 0.29f; x += 0.004f) for (float y = 0.10f; y <= 0.62f; y += 0.004f)
            if (!Physics.Raycast(new Vector3(x, y, -1f), Vector3.forward, 2f)) { if (Mathf.Abs(x) < 0.24f && y > 0.12f && y < 0.50f) { x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y); } }
        sb.AppendLine($"opening x {x0:F3}..{x1:F3} y {y0:F3}..{y1:F3}  -> {(x1 - x0):F3} x {(y1 - y0):F3} m; front sheet z {front.bounds.center.z:F3}");
        var lg = new GameObject("L"); var l = lg.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1.5f; l.shadows = LightShadows.Soft; lg.transform.rotation = Quaternion.Euler(38, 160, 0);
        var cg = new GameObject("C"); var cam = cg.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.16f, 0.17f, 0.2f); cam.fieldOfView = 28; cam.allowMSAA = true;
        cg.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Gen/capture_1920x1080.renderTexture"); Vector3 c = new Vector3(0, 0.32f, 0);
        int k = 0;
        foreach (var dir in new[] { new Vector3(0, 0.05f, -1), new Vector3(-0.7f, 0.5f, -1), new Vector3(0.1f, 1.2f, 0.4f) })
        {
            cg.transform.position = c + dir.normalized * 2.0f; cg.transform.rotation = Quaternion.LookRotation(c - cg.transform.position);
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); RenderTexture.active = null; cam.targetTexture = null;
            File.WriteAllBytes($"Capture/shell_{k++}.jpg", tex.EncodeToJPG(92)); Object.DestroyImmediate(tex);
        }
        return sb.ToString();
    }
}
