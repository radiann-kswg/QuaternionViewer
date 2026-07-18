namespace QuaternionViewer.Chapters
{
    /// <summary>Ch.6 角速度と微分方程式 (仕様書 5.6)。台本: Resources/Guide/ch6 (正典: docs/section-guide.md §4.6)。</summary>
    /// <remarks>ω ドライバ (world/body 切替・積分器) と |q|-1 グラフは未実装 ―― 暫定は spin による固定軸回転。</remarks>
    public class Ch6_AngularVelocity : ChapterBase
    {
        protected override string DefaultScriptResource => "Guide/ch6";
    }
}
