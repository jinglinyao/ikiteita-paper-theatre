using System.Collections;
using System.IO;
using UnityEngine;

/// Play-mode pass over a stretch of the film at a fixed 24 frames a second, from one fixed camera: the "television camera" that
/// sees the stage through its proscenium (40 mm on a 36 x 27 gate, from 4.444 m in front of the opening). What it writes is both
/// the preview of the stage timeline and the picture the television in the room shows.
public class FilmBaker : MonoBehaviour
{
    public FilmClock film; public StagePerformer singer; public Camera cam;
    public int fps = 24, f0 = 0, f1 = 4634, step = 1, width = 960, height = 720, warmup = 24; public string dir = "Capture/tv";
    public static int Written; public static bool Done; public static string Note = "";

    IEnumerator Start()
    {
        Written = 0; Done = false; Note = ""; Time.captureFramerate = fps;
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 }; var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        cam.enabled = false; var keep = cam.sensorSize; cam.sensorSize = new Vector2(36f, 27f); cam.transform.position = new Vector3(0, 1.5f, -4.444f); cam.transform.rotation = Quaternion.identity; cam.focalLength = 40f;
        var vol = Object.FindFirstObjectByType<UnityEngine.Rendering.Volume>(); UnityEngine.Rendering.Universal.DepthOfField dof = null; if (vol != null) vol.profile.TryGet(out dof);
        if (dof != null) { dof.focusDistance.value = 7.4f; dof.aperture.value = 8f; dof.focalLength.value = 40f; }
        if (singer != null) foreach (var r in singer.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { r.forceMatrixRecalculationPerRender = true; r.updateWhenOffscreen = true; }
        Directory.CreateDirectory(dir);
        for (int i = -warmup; f0 + Mathf.Max(0, i) * step < f1; i++)
        {
            int f = f0 + Mathf.Max(0, i) * step; float t = f / (float)fps;
            if (singer != null) { singer.live = true; singer.time = t; }
            film.Apply(t);
            yield return new WaitForEndOfFrame();
            if (i < 0) continue;
            var k = cam.targetTexture; cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); RenderTexture.active = null; cam.targetTexture = k;
            File.WriteAllBytes($"{dir}/f_{f:00000}.jpg", tex.EncodeToJPG(92)); Written++;
        }
        cam.sensorSize = keep; Note = $"frames {f0}..{f1 - 1} step {step} -> {dir}"; Done = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
