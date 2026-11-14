// generated from docs/film_v5.json by the film set-up: the start of every bar of the song, in seconds (index = bar number; the last entry is the end of the film)
public static class FilmBars
{
    public static readonly float[] T = { 0.000f, 1.184f, 3.344f, 5.503f, 7.616f, 9.822f, 11.981f, 14.141f, 16.300f, 18.460f, 20.643f, 22.802f, 24.961f, 27.121f, 29.280f, 31.440f, 33.599f, 35.782f, 37.941f, 40.078f, 42.260f, 44.420f, 46.579f, 48.739f, 50.898f, 53.058f, 55.240f, 57.377f, 59.536f, 61.719f, 63.878f, 66.038f, 68.197f, 70.356f, 72.516f, 74.675f, 76.835f, 79.018f, 81.177f, 83.336f, 85.496f, 87.655f, 89.815f, 91.974f, 94.134f, 96.316f, 98.453f, 100.635f, 102.795f, 104.977f, 107.114f, 109.273f, 111.433f, 113.592f, 115.775f, 117.934f, 120.094f, 122.253f, 124.436f, 126.572f, 128.731f, 130.891f, 133.074f, 135.233f, 137.392f, 139.552f, 141.711f, 143.894f, 146.030f, 148.190f, 150.349f, 152.532f, 154.691f, 156.851f, 159.010f, 161.170f, 163.329f, 165.489f, 167.648f, 169.831f, 171.990f, 174.150f, 176.239f, 178.260f, 180.326f, 182.439f, 184.506f, 193.090f };
    /// the time of a beat: bar (1-based) and beat within it (0 .. 4, fractions allowed)
    public static float At(float bar, float beat = 0f) { int b = UnityEngine.Mathf.Clamp((int)bar, 1, T.Length - 2); float f = (bar - b) * 4f + beat; return T[b] + (T[b + 1] - T[b]) * f / 4f; }
}
