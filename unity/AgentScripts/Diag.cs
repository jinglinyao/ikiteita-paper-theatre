using UnityEngine; using UnityEditor; using System.Text;
public static class Diag {
    public static string Types() {
        var sb = new StringBuilder();
        foreach (var p in new[] { "Assets/MV/Characters/jinglin_ps2.vrm", "Assets/Props/television_02/television_02_2k.gltf", "Assets/Props/sf_bento/scene.gltf" }) {
            var imp = AssetImporter.GetAtPath(p);
            sb.AppendLine(p + " | importer " + (imp != null ? imp.GetType().Name : "null") + " | main " + AssetDatabase.GetMainAssetTypeAtPath(p));
        }
        return sb.ToString();
    }
    public static string Reimport() {
        AssetDatabase.ImportAsset("Assets/MV/Characters/jinglin_ps2.vrm", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Props", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
        return Types();
    }
}
public static class Diag2 {
    public static string RoomDebug() {
        var sb = new System.Text.StringBuilder();
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) sb.AppendLine($"{l.name} {l.type} pos {l.transform.position:F2} fwd {l.transform.forward:F2} int {l.intensity} range {l.range} en {l.enabled && l.gameObject.activeInHierarchy} mask {l.cullingMask}");
        var rc = GameObject.Find("RoomCam").GetComponent<Camera>();
        sb.AppendLine($"cam pos {rc.transform.position:F2} fwd {rc.transform.forward:F2} mask {rc.cullingMask} near {rc.nearClipPlane}");
        var ad = rc.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); ad.renderPostProcessing = false;
        var g = new GameObject("DbgLight"); var dl = g.AddComponent<Light>(); dl.type = LightType.Point; dl.transform.position = new Vector3(0, 1.6f, 0); dl.intensity = 6; dl.range = 8;
        var rt = RenderTexture.GetTemporary(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var keep = rc.targetTexture; rc.targetTexture = rt; rc.Render(); rc.targetTexture = keep;
        RenderTexture.active = rt; var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); RenderTexture.active = null;
        System.IO.File.WriteAllBytes("Capture/dbg_room.jpg", tex.EncodeToJPG(90)); RenderTexture.ReleaseTemporary(rt);
        Object.DestroyImmediate(g); ad.renderPostProcessing = true;
        return sb.ToString();
    }
}
