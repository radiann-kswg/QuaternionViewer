using System;
using System.Collections.Generic;
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
