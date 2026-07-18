using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using QuaternionViewer.Visualization;
using UnityEngine;

namespace QuaternionViewer.Chapters
{
    /// <summary>
    /// 台本 Markdown → ビート列のパーサ (section-guide §2 のデータモデルを供給する)。
    /// 台本の正典は docs/section-guide.md §4 であり、Resources/Guide/*.md はその画面用転記。
    /// </summary>
    /// <remarks>
    /// 書式 (最小規則):
    /// <code>
    /// # 章タイトル
    /// ## ◆ ビート見出し      (◆=核心 / ○=発展 / 無印=核心扱い)
    /// @posture 1 2 0.5 120   (軸 x y z と角 deg)
    /// @demos Mirrors|Graph   (None で全消灯。宣言が無ければ現状維持)
    /// @ball VectorPart       (VectorPart / RotationVector)
    /// @camera CoreAndGlobe   (Overview / CoreAndGlobe / SpaceBall / Gimbal)
    /// @highlight WXYZ        (ReadoutHighlight の値)
    /// @action flipSign       (複数行可・記述順に実行)
    /// @focus pole+ pole-     (指し示す名所。ビート限りで自動消灯)
    /// @euler 90 0 0          (オイラー角 pitch yaw roll (度) で姿勢指示 ―― Ch.4 用)
    /// ### 直感 / ### 数理 / ### 話者ノート
    /// </code>
    /// 未知の指示・節は警告として報告し、無視する (台本の誤記でシーンを壊さない)。
    /// </remarks>
    public static class GuideScript
    {
        public static List<GuideBeat> Parse(string text, out string chapterTitle)
        {
            return Parse(text, out chapterTitle, out _);
        }

        public static List<GuideBeat> Parse(string text, out string chapterTitle, out List<string> warnings)
        {
            chapterTitle = "";
            warnings = new List<string>();
            var beats = new List<GuideBeat>();
            if (string.IsNullOrEmpty(text)) return beats;

            GuideBeat beat = null;
            string section = null;
            var buffers = new Dictionary<string, StringBuilder>();

            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                string trimmed = raw.Trim();

                if (trimmed.StartsWith("## ", StringComparison.Ordinal))
                {
                    Flush(beat, buffers);
                    beat = new GuideBeat();
                    string t = trimmed.Substring(3).Trim();
                    if (t.StartsWith("◆", StringComparison.Ordinal))
                    {
                        beat.core = true;
                        t = t.Substring(1).Trim();
                    }
                    else if (t.StartsWith("○", StringComparison.Ordinal))
                    {
                        beat.core = false;
                        t = t.Substring(1).Trim();
                    }

                    beat.title = t;
                    beats.Add(beat);
                    section = null;
                    continue;
                }

                if (trimmed.StartsWith("# ", StringComparison.Ordinal))
                {
                    if (beat == null && chapterTitle.Length == 0) chapterTitle = trimmed.Substring(2).Trim();
                    continue;
                }

                if (beat == null) continue; // 章タイトルと最初のビートの間の前書きは台本外

                if (trimmed.StartsWith("### ", StringComparison.Ordinal))
                {
                    section = trimmed.Substring(4).Trim();
                    if (section != "直感" && section != "数理" && section != "話者ノート")
                    {
                        warnings.Add($"未知の節 '### {section}' (ビート '{beat.title}') を無視した");
                        section = null;
                    }

                    continue;
                }

                if (trimmed.StartsWith("@", StringComparison.Ordinal))
                {
                    ParseDirective(trimmed, beat, warnings);
                    continue;
                }

                if (section != null)
                {
                    if (!buffers.TryGetValue(section, out StringBuilder sb))
                    {
                        buffers[section] = sb = new StringBuilder();
                    }

                    sb.Append(trimmed).Append('\n');
                }
            }

            Flush(beat, buffers);
            return beats;
        }

        private static void Flush(GuideBeat beat, Dictionary<string, StringBuilder> buffers)
        {
            if (beat != null)
            {
                beat.intuition = Body(buffers, "直感");
                beat.math = Body(buffers, "数理");
                beat.presenterNote = Body(buffers, "話者ノート");
            }

            buffers.Clear();
        }

        private static string Body(Dictionary<string, StringBuilder> buffers, string key)
        {
            return buffers.TryGetValue(key, out StringBuilder sb) ? sb.ToString().TrimEnd('\n') : "";
        }

        private static void ParseDirective(string line, GuideBeat beat, List<string> warnings)
        {
            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string key = parts[0].ToLowerInvariant();
            string arg = parts.Length > 1 ? string.Join(" ", parts, 1, parts.Length - 1) : "";

            switch (key)
            {
                case "@posture":
                    if (parts.Length == 5
                        && TryFloat(parts[1], out float x)
                        && TryFloat(parts[2], out float y)
                        && TryFloat(parts[3], out float z)
                        && TryFloat(parts[4], out float deg))
                    {
                        beat.setPosture = true;
                        beat.axis = new Vector3(x, y, z);
                        beat.angleDeg = deg;
                    }
                    else
                    {
                        warnings.Add($"@posture の書式不正 '{line}' (軸 x y z と角 deg の4値が要る)");
                    }

                    break;

                case "@euler":
                    if (parts.Length == 4
                        && TryFloat(parts[1], out float p)
                        && TryFloat(parts[2], out float yaw)
                        && TryFloat(parts[3], out float roll))
                    {
                        beat.setEulerPosture = true;
                        beat.eulerDeg = new Vector3(p, yaw, roll);
                    }
                    else
                    {
                        warnings.Add($"@euler の書式不正 '{line}' (pitch yaw roll の3値 (度) が要る)");
                    }

                    break;

                case "@demos":
                    DemoFlags flags = DemoFlags.None;
                    bool ok = arg.Length > 0;
                    foreach (string token in arg.Split('|'))
                    {
                        if (Enum.TryParse(token.Trim(), true, out DemoFlags f))
                        {
                            flags |= f;
                        }
                        else
                        {
                            warnings.Add($"@demos の未知フラグ '{token.Trim()}'");
                            ok = false;
                        }
                    }

                    if (ok)
                    {
                        beat.setDemos = true;
                        beat.demos = flags;
                    }

                    break;

                case "@ball":
                    if (Enum.TryParse(arg, true, out BallModel model))
                    {
                        beat.setBallModel = true;
                        beat.ballModel = model;
                    }
                    else
                    {
                        warnings.Add($"@ball の未知模型 '{arg}'");
                    }

                    break;

                case "@camera":
                    if (Enum.TryParse(arg, true, out CameraFraming framing))
                    {
                        beat.setCamera = true;
                        beat.camera = framing;
                    }
                    else
                    {
                        warnings.Add($"@camera の未知フレーミング '{arg}'");
                    }

                    break;

                case "@highlight":
                    if (Enum.TryParse(arg, true, out ReadoutHighlight highlight))
                    {
                        beat.setHighlight = true;
                        beat.highlight = highlight;
                    }
                    else
                    {
                        warnings.Add($"@highlight の未知行 '{arg}'");
                    }

                    break;

                case "@focus":
                    if (arg.Length > 0)
                    {
                        foreach (string token in arg.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            beat.focus.Add(token);
                        }
                    }
                    else
                    {
                        warnings.Add("@focus に対象名が無い");
                    }

                    break;

                case "@action":
                    if (arg.Length > 0)
                    {
                        beat.actions.Add(arg);
                    }
                    else
                    {
                        warnings.Add("@action にアクション名が無い");
                    }

                    break;

                default:
                    warnings.Add($"未知の指示 '{key}' を無視した");
                    break;
            }
        }

        private static bool TryFloat(string s, out float value)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
