using System.Linq; using System.Text; using UnityEngine; using UnityEngine.Rendering; using UnityEngine.Rendering.Universal; using UnityEditor;
public static class GiDiag
{
    public static string Run()
    {
        var sb = new StringBuilder(); var prv = ProbeReferenceVolume.instance;
        sb.AppendLine("initialized " + prv.isInitialized + " scenario '" + prv.lightingScenario + "' set " + (prv.currentBakingSet != null ? prv.currentBakingSet.name : "null") + " dataLoaded " + prv.DataHasBeenLoaded());
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset; var so = new SerializedObject(urp);
        foreach (var n in new[] { "m_LightProbeSystem", "m_ProbeVolumeMemoryBudget", "m_ProbeVolumeSHBands", "m_SupportProbeVolumeScenarios" }) { var p = so.FindProperty(n); sb.AppendLine("  " + n + " = " + (p != null ? p.intValue.ToString() : "?")); }
        sb.AppendLine("ambient mode " + RenderSettings.ambientMode + " ambient " + RenderSettings.ambientLight);
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None); var was = lights.Select(l => l.enabled).ToArray();
        foreach (var l in lights) l.enabled = false;
        var ck = Object.FindFirstObjectByType<StageClock>(); ck.singer.Apply(57.9f); ck.Apply(57.9f, 16);
        var cam = ck.cam; var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Gen/capture_1920x1080.renderTexture");
        cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); RenderTexture.active = null; cam.targetTexture = null;
        System.IO.File.WriteAllBytes("Capture/s16_indirect_only.jpg", tex.EncodeToJPG(95)); Object.DestroyImmediate(tex);
        for (int i = 0; i < lights.Length; i++) lights[i].enabled = was[i];
        return sb.ToString();
    }
}
