using System;
using System.Collections.Generic;
using QuaternionViewer.Core;
using QuaternionViewer.UI;
using QuaternionViewer.Visualization;
using UnityEngine;

namespace QuaternionViewer.Chapters
{
    /// <summary>
    /// ビートの宣言を「儀の状態」へ翻訳する薄い適用器 (section-guide §2)。
    /// 章 (<see cref="ChapterBase"/>) の BeatChanged を購読し、既存資産を叩くだけで新しい演出は作らない。
    /// </summary>
    /// <remarks>
    /// setCamera の適用先 (フレーミング補間) は未実装のため現状は無操作 (フック増強フェーズで実装)。
    /// 宣言で表せない特殊操作 (Ch.2 符号反転・Ch.5 補正トグル・Ch.6 ω設定) は
    /// <see cref="RegisterAction"/> で登録した名前付きアクションを @action 指示から呼ぶ。
    /// </remarks>
    [ExecuteAlways]
    public class GuideController : MonoBehaviour
    {
        public ChapterBase chapter;

        [Header("叩く先 (既存資産)")]
        public RotationSource source;
        public HalfAngleMirrors mirrors;
        public GimbalRig gimbal;
        public InterpRace race;
        public GraphPlotter graph;
        public RotationSpaceBall ball;

        [Header("「見よ」の適用先 (@focus / @highlight)")]
        public FocusMarkerRenderer focusMarkers;
        public QuaternionReadout readout;

        private readonly Dictionary<string, Action> _actions =
            new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

        private void OnEnable()
        {
            RegisterBuiltinActions();
            if (chapter != null)
            {
                chapter.BeatChanged += Apply;
                Apply(chapter.Current);
            }
        }

        private void OnDisable()
        {
            if (chapter != null) chapter.BeatChanged -= Apply;
        }

        /// <summary>@action 指示から呼べる操作を登録する (特殊操作の受け口)。同名は上書き。</summary>
        public void RegisterAction(string actionName, Action action)
        {
            _actions[actionName] = action;
        }

        private void RegisterBuiltinActions()
        {
            RegisterAction("clearTrail", () =>
            {
                if (ball != null) ball.ClearTrail();
            });

            // Ch.2: 符号反転 q ← -q。生の -q を配布点へ置く (Readout は正準化しない ―― spec 3.6-I)。
            // 再入場でもう一度反転する (往復可)。ドラッグ終了時の軸角読み戻しでも符号は保存される。
            RegisterAction("flipSign", () =>
            {
                if (source == null) return;
                source.driveFromInspector = false;
                source.spin = false;
                source.Pose = source.Pose * -1f;
            });

            // Ch.6: 自動回転 (現状は世界系固定軸の角度積算。ω ドライバ実装までの暫定)
            RegisterAction("spinOn", () =>
            {
                if (source == null) return;
                source.driveFromInspector = true;
                source.spin = true;
            });
            RegisterAction("spinOff", () =>
            {
                if (source != null) source.spin = false;
            });

            // Ch.5: 最短経路補正トグル (spec 3.2 / 5.5)
            RegisterAction("interpCorrectionOn", () =>
            {
                if (race != null) race.shortestPath = true;
            });
            RegisterAction("interpCorrectionOff", () =>
            {
                if (race != null) race.shortestPath = false;
            });

            // Ch.5: 両端の設定 (既定 ⇄ ほぼ一致 ―― Ω→0 の除去可能特異点演示)
            RegisterAction("interpDefaultEnds", () =>
            {
                if (race == null) return;
                race.startAxis = new Vector3(0f, 1f, 0f);
                race.startAngleDeg = 10f;
                race.endAxis = new Vector3(1f, 2f, 0.5f);
                race.endAngleDeg = 170f;
            });
            RegisterAction("interpCloseEnds", () =>
            {
                if (race == null) return;
                race.endAxis = race.startAxis;
                race.endAngleDeg = race.startAngleDeg + 8f;
            });
        }

        /// <summary>章を切り替える (ChapterNavigator が呼ぶ)。購読を張り替えて現在ビートを適用する。</summary>
        public void SetChapter(ChapterBase next)
        {
            if (chapter == next)
            {
                if (chapter != null) Apply(chapter.Current);
                return;
            }

            if (chapter != null) chapter.BeatChanged -= Apply;
            chapter = next;
            if (chapter != null)
            {
                chapter.BeatChanged += Apply;
                Apply(chapter.Current);
            }
        }

        /// <summary>ビートの宣言を儀へ適用する。set* が立っていない項目は現状維持 (section-guide §1.3)。</summary>
        public void Apply(GuideBeat beat)
        {
            if (beat == null) return;

            if (beat.setPosture && source != null)
            {
                source.driveFromInspector = true;
                source.spin = false;
                source.axis = beat.axis;
                source.angleDeg = beat.angleDeg;
            }

            if (beat.setEulerPosture && source != null)
            {
                source.driveFromInspector = false;
                source.spin = false;
                source.Pose = QuatMath.FromEuler(beat.eulerDeg * Mathf.Deg2Rad);
            }

            if (beat.setDemos)
            {
                SetActive(mirrors, (beat.demos & DemoFlags.Mirrors) != 0);
                SetActive(gimbal, (beat.demos & DemoFlags.Gimbal) != 0);
                SetActive(race, (beat.demos & DemoFlags.Interp) != 0);
                SetActive(graph, (beat.demos & DemoFlags.Graph) != 0);
            }

            if (beat.setBallModel && ball != null && ball.model != beat.ballModel)
            {
                ball.model = beat.ballModel;
                ball.ClearTrail();
            }

            // @focus は「指差し」―― 宣言の無いビートでは空リストが渡り自動消灯する
            if (focusMarkers != null) focusMarkers.SetTargets(beat.focus);

            if (beat.setHighlight && readout != null) readout.Highlight(beat.highlight);

            // beat.setCamera は適用先 (フレーミング補間) の実装待ち。台本側の宣言は既に立ててある。

            foreach (string actionName in beat.actions)
            {
                if (_actions.TryGetValue(actionName, out Action act))
                {
                    act();
                }
                else
                {
                    Debug.LogWarning($"[GuideController] 未登録のアクション '{actionName}'", this);
                }
            }
        }

        private static void SetActive(Component c, bool active)
        {
            if (c != null && c.gameObject.activeSelf != active)
            {
                c.gameObject.SetActive(active);
            }
        }
    }
}
