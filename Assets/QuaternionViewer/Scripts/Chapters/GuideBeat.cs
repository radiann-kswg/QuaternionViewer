using System;
using System.Collections.Generic;
using QuaternionViewer.Visualization;
using UnityEngine;

namespace QuaternionViewer.Chapters
{
    /// <summary>章演出デモの on/off 宣言 (section-guide §2)。</summary>
    [Flags]
    public enum DemoFlags
    {
        None = 0,
        Mirrors = 1,
        Gimbal = 2,
        Interp = 4,
        Graph = 8,
        TwinDice = 16,
    }

    /// <summary>カメラのフレーミング指示 (section-guide §2)。適用はフック増強フェーズで実装する。</summary>
    public enum CameraFraming
    {
        Overview,
        CoreAndGlobe,
        SpaceBall,
        Gimbal,
    }

    /// <summary>情報パネルの強調行 (section-guide §2)。適用は Readout 行強調 API の実装後。</summary>
    public enum ReadoutHighlight
    {
        None,
        WXYZ,
        AxisAngle,
        HalfAngle,
        Euler,
        DetE,
        QNormDrift,
        Matrix,
    }

    /// <summary>
    /// 解説モードの最小単位「ビート」―― ナレーション文 (二層) + 儀の状態指示 (section-guide §1.1, §2)。
    /// 台本 Markdown (Resources/Guide/*.md) から <see cref="GuideScript.Parse(string, out string)"/> が生成する。
    /// </summary>
    /// <remarks>
    /// 宣言は「そのビートで指示があった項目だけ適用」する。set* が false の項目は儀の現状を維持する
    /// (順路は推奨する視点であって檻ではない ―― section-guide §1.3)。
    /// </remarks>
    [Serializable]
    public class GuideBeat
    {
        public string title = "";

        /// <summary>◆核心ビートなら true、○発展ビートなら false (section-guide §4.0)。無印は核心扱い。</summary>
        public bool core = true;

        [TextArea] public string intuition = "";
        [TextArea] public string math = "";
        [TextArea] public string presenterNote = "";

        [Header("儀の状態 (宣言的に指示)")]
        public bool setPosture;
        public Vector3 axis = Vector3.up;
        public float angleDeg;

        /// <summary>オイラー角 (ZXY, 度) での姿勢指示 (@euler)。ジンバル章 (Ch.4) が使う。</summary>
        public bool setEulerPosture;
        public Vector3 eulerDeg;

        public bool setDemos;
        public DemoFlags demos = DemoFlags.None;

        public bool setBallModel;
        public BallModel ballModel = BallModel.VectorPart;

        public bool setCamera;
        public CameraFraming camera = CameraFraming.Overview;

        public bool setHighlight;
        public ReadoutHighlight highlight = ReadoutHighlight.None;

        /// <summary>宣言で表せない特殊操作 (符号反転・補正トグル等)。<see cref="GuideController"/> の登録済みアクション名。</summary>
        public List<string> actions = new List<string>();

        /// <summary>
        /// このビートで指し示す名所 (@focus)。set* 系と違い「状態」でなく「指差し」――
        /// 宣言の無いビートへ移ると自動消灯する (FocusMarkerRenderer)。
        /// </summary>
        public List<string> focus = new List<string>();
    }
}
