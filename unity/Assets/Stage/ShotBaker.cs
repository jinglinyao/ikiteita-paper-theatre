using System.Collections;
using System.IO;
using UnityEngine;

/// Play-mode pass over a list of stage shots at a fixed 24 frames a second. Every shot is rendered from its own camera
/// (FilmClock.ShotCamera) with `handle` extra frames on each side for the soft cuts and dissolves, into <dir>/Snn/f_#####.jpg
/// (film frame numbers). The clock is applied before the camera, so cameras that follow the bird or a hand see where it is now.
public class ShotBaker : MonoBehaviour
{
    public FilmClock film; public StagePerformer singer; public Camera cam;
    public int[] shots; public int fps = 24, handle = 8, step = 1, width = 960, height = 540, warmup = 12, quality = 92; public string dir = "Capture/shots";
    public static int Written; public static bool Done; public static string Note = "";

    IEnumerator Start()
    {
        Written = 0; Done = false; Note = ""; Time.captureFramerate = fps;
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 }; var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        cam.enabled = false; var keep = cam.sensorSize; cam.sensorSize = new Vector2(36f, 20.25f);
        if (singer != null) foreach (var r in singer.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { r.forceMatrixRecalculationPerRender = true; r.updateWhenOffscreen = true; }
        foreach (int n in shots)
        {
            if (n < 1 || n > FilmShots.N) continue; int a = Mathf.Max(0, FilmShots.F0[n - 1] - handle), b = Mathf.Min(FilmShots.F1[FilmShots.N - 1], FilmShots.F1[n - 1] + handle);
            string d = $"{dir}/S{n:00}"; if (Directory.Exists(d)) Directory.Delete(d, true); Directory.CreateDirectory(d);
            for (int i = -warmup; a + Mathf.Max(0, i) * step < b; i++)
            {
                int f = a + Mathf.Max(0, i) * step; float t = f / (float)fps;
                if (singer != null) { singer.live = true; singer.time = t; }
                film.Apply(t); if (!film.ShotCamera(n, t, cam)) { Note += $"S{n:00} has no camera; "; break; }
                yield return new WaitForEndOfFrame();
                if (i < 0) continue;
                film.ShotCamera(n, t, cam);                                                   // again, now that her hand is where this frame's pose puts it
                var k = cam.targetTexture; cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); RenderTexture.active = null; cam.targetTexture = k;
                File.WriteAllBytes($"{d}/f_{f:00000}.jpg", tex.EncodeToJPG(quality)); Written++;
            }
        }
        cam.sensorSize = keep; Note += $"{shots.Length} shots -> {dir}"; Done = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
