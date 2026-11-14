    /// Measurements of the stage for the blueprint: the bounds of every named piece and the plan's lights, in the stage's own frame.
    public static string BlueprintGeo()
    {
        var stage = GameObject.Find("Stage"); if (stage == null) return "no stage"; var st = stage.transform; var sb = new StringBuilder(); sb.Append("{\"pieces\":{");
        string V(Vector3 v) => $"[{v.x:F3},{v.y:F3},{v.z:F3}]";
        bool first = true;
        foreach (var n in new[] { "HouseR", "HouseL", "FarRow", "LampPost", "Street", "StreetProps", "Cyc", "PaperSun", "Clouds", "Proscenium", "GhostLamp", "Props", "Lin", "MicStand", "Crowd", "Person0", "Person1", "Person2", "Person3", "Person4", "Person5", "RedPaper", "Tape0", "Bird", "Folds", "PaperGirl", "Rain", "StageHand", "WingR0", "WingL0", "Lanterns", "BareBulbProp" })
        {
            var t = stage.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == n); if (t == null) continue;
            var rs = t.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) continue; var b = new Bounds(st.InverseTransformPoint(rs[0].bounds.center), Vector3.zero);
            foreach (var r in rs) { b.Encapsulate(st.InverseTransformPoint(r.bounds.min)); b.Encapsulate(st.InverseTransformPoint(r.bounds.max)); }
            sb.Append((first ? "" : ",") + $"\"{n}\":{{\"min\":{V(b.min)},\"max\":{V(b.max)},\"pos\":{V(st.InverseTransformPoint(t.position))}}}"); first = false;
        }
        sb.Append("},\"lights\":["); first = true;
        foreach (var l in stage.GetComponentsInChildren<Light>(true))
        {
            if (l.name.StartsWith("PaneLight") || l.name == "ShotFill") continue;
            sb.Append((first ? "" : ",") + $"{{\"name\":\"{l.name}\",\"type\":\"{l.type}\",\"pos\":{V(st.InverseTransformPoint(l.transform.position))},\"dir\":{V(st.InverseTransformDirection(l.transform.forward))},\"angle\":{l.spotAngle:F1},\"range\":{l.range:F1},\"col\":[{l.color.r:F2},{l.color.g:F2},{l.color.b:F2}],\"shadows\":\"{l.shadows}\"}}"); first = false;
        }
        int bulbs = stage.GetComponentsInChildren<Renderer>(true).Count(r => r.name == "bulb"), panes = stage.GetComponentsInChildren<Renderer>(true).Count(r => r.name == "FarPane");
        sb.Append($"],\"bulbs\":{bulbs},\"farPanes\":{panes},\"stageWorld\":{V(st.position)}}}");
        File.WriteAllText("../work/blueprint_geo.json", sb.ToString()); return "geo written, " + sb.Length + " chars";
    }
