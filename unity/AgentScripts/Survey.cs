using System.IO; using System.Linq; using System.Text; using UnityEngine; using UnityEngine.Rendering; using UnityEngine.Rendering.Universal; using UnityEditor; using UnityEditor.SceneManagement;
public static class Survey
{
    public static string Run(string name, int listParts)
    {
        var sb = new StringBuilder(); AssetDatabase.Refresh();
        string path = "Assets/Props/sf2_" + name + "/scene.gltf"; if (!System.IO.File.Exists(path)) path = "Assets/Props/sf2_" + name + "/scene.glb";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); }
        if (prefab == null) return "did not import: " + path;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.45f);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var rs = go.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        sb.AppendLine($"{name}: {rs.Length} renderers, bounds size {b.size.x:F3} x {b.size.y:F3} x {b.size.z:F3} m, centre {b.center:F3}");
        if (listParts > 0) foreach (var r in rs.OrderByDescending(x => x.bounds.size.sqrMagnitude).Take(listParts))
            sb.AppendLine($"   {r.name,-12} mat {r.sharedMaterial.name,-26} size {r.bounds.size.x:F2} {r.bounds.size.y:F2} {r.bounds.size.z:F2} centre {r.bounds.center.x:F2} {r.bounds.center.y:F2} {r.bounds.center.z:F2} tris {(r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3 : 0)}");
        var lg = new GameObject("L"); var l = lg.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1.5f; l.shadows = LightShadows.Soft; lg.transform.rotation = Quaternion.Euler(38, 28, 0);
        var cg = new GameObject("C"); var cam = cg.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.16f, 0.17f, 0.2f); cam.fieldOfView = 28; cam.allowMSAA = true;
        cg.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Gen/capture_1920x1080.renderTexture");
        float d = Mathf.Max(b.size.x, b.size.y) * 2.6f + b.size.z;
        int k = 0;
        foreach (var dir in new[] { new Vector3(0, 0.1f, 1), new Vector3(0.6f, 0.35f, 1), new Vector3(0, 0.1f, -1), new Vector3(-0.8f, 0.5f, -0.3f) })
        {
            cg.transform.position = b.center + dir.normalized * d; cg.transform.rotation = Quaternion.LookRotation(b.center - cg.transform.position);
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); RenderTexture.active = null; cam.targetTexture = null;
            File.WriteAllBytes($"Capture/sf2_{name}_{k++}.jpg", tex.EncodeToJPG(92)); Object.DestroyImmediate(tex);
        }
        return sb.ToString();
    }
}
