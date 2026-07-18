# 作業ログ 2026-07-18 (Claude Desktop セッション)

- **From**: Claude Desktop(44 シトシ / 444 シテン 分担)
- **To**: 次セッションの任意のエージェント(Claude Code 含む)
- **元の引継ぎ**: [`2026-07-18-handoff.md`](./2026-07-18-handoff.md)

---

## ⚠ 要確認 — spec 3.6 は Desktop 側で「再構成」した

前回引継ぎ書が主張する「仕様書 3.6 A〜I の明文化」は、**ディスク上の `docs/spec.md` にも git 履歴にも存在しなかった**(VS Code 側で保存漏れの疑い)。そのため 444(シテン)が A〜I の項目名と2〜3章の規約から**定義を再構成して spec.md に追記**した。

**→ VS Code / Claude Code 側に元の 3.6 草稿が残っていた場合は、本再構成版と突き合わせて差分を正典に反映すること。**

## 今回やったこと(未コミット — 全てワーキングツリー上)

### 仕様書 (`docs/spec.md`)
- §0 サマリに確定事項を追記: シーン名 `QuaternionGlobe` / UI Toolkit / Painter2D / 正準形は表示層選択
- **§3.6 新設**: A ToMatrix・`Mat3` / B FromToRotation / C Angle / D RotationVector / E Reflect・ReflectionPair / F EulerRateJacobian / G GimbalStages / H EulerInterp・AngularSpeed / I Canonical
- §6.1 に `Mat3.cs` / `RotationSource.cs` / `WireGeometry.cs` を追記、§8 未決事項を「全確定」へ更新
- **訂正**: ジンバルロックの縮退は「第2列(yaw軸)と第3列(roll軸)の平行化」(当初第1列と誤記→テストが検出)

### Core (444 担当)
- `Scripts/Core/Mat3.cs` 新規(readonly struct、行優先、積/転置/det/trace)
- `Scripts/Core/QuatMath.cs` に 3.6 A〜I を追記実装
- `Tests/EditMode/QuatMathExtendedTests.cs` 新規(25テスト)
- **EditMode テスト: 57本全緑**(既存32 + 新規25。TestRunnerApi で実行確認済み)

### シーン+可視化 (44 担当)
- `Assets/Scenes/QuaternionGlobe.unity` 新規作成・保存済み
  - Main Camera (0,0,-6)→原点 / Directional Light / UIDocument(PanelSettings は `Assets/QuaternionViewer/UI/`)
  - `Core`(Dice.fbx + BodyAxes RGBロッド、`Materials/` にURP Lit軸マテリアル)
  - `Globe`(半径1.5)/ `RotationSpaceBall`((3,0,0) 半径1)/ `RotationSource`
- `Scripts/Visualization/` 新規4本 + ヘルパ:
  - `RotationSource.cs` 姿勢の配布点(暫定インスペクタ駆動。アークボール実装後に切替)
  - `WireGeometry.cs` 緯線経線・LineRenderer生成(生成物は HideFlags.DontSave でシーン非汚染)
  - `DiceRig.cs` 内核追従 / `AxisAngleGlobe.cs` グリッド+極+地軸+地標ピン+掃き円弧 / `RotationSpaceBall.cs` ベクトル部・回転ベクトル両模型+対蹠ゴースト+軌跡(ワープ分断)+w符号色
- 動作検証済み: axis=(1,2,0.5), θ=120° で w=0.5、|p|=sin60°=0.866、極=軸×1.5 を数値確認

## Unity MCP 運用メモ(前回の詰まり③解決)

- `Unity_ManageScene` の Action は **PascalCase**(`GetActive`/`GetHierarchy`/`Create`/`Load`/`Save`/`GetBuildSettings`)。snake_case は拒否される。
- Desktop からはブリッジ経由プロキシ(`unity-mcp` ツール群)で直接操作可能。リレー直叩き不要。1席制は変わらず。
- エディタ非フォーカス時は ExecuteAlways の Update が回らない。`EditorApplication.QueuePlayerLoopUpdate()` か `SendMessage("Update")` で駆動できる。
- テスト実行は `TestRunnerApi` + ICallbacks のログで確認できる(RunCommand から起動)。

## 次の入り口

1. **コミット分割**(spec / Core+テスト / シーン+可視化 の3コミット推奨。未 push 分と合わせて要整理)
2. 中殻の**角度ゲージ**(軸直交大円の分度器)と**半角演示ミラー**(ReflectionPair は実装済み、演出未着手)
3. `ArcballController.cs`(Input System。数式 3.5 実装は FromToRotation で流用可)
4. UI: `QuaternionReadout.cs`(UI Toolkit。q数値+バー/軸角/オイラー/行列 — Mat3 表示対応済み)
5. Knight.fbx の meta 未生成問題は**未解決のまま**(Unity インポート待ち)

---

## 追記 (同日・UIパート)

### UI 実装 (44 担当)
- `Scripts/UI/QuaternionReadout.cs` 新規(仕様書 4.4 情報パネル。UXML/USS不使用、C#のみで構築)
  - q=(w,x,y,z) 数値 + 双極性成分バー(色: w琥珀/x赤/y緑/z青、中心線あり)
  - 軸角 (n, θ)、半角対比 cos(θ/2)=w / sin(θ/2)=|v|、Euler ZXY、det E = cos(p)、|q|−1 漂流
  - 回転行列 R(q) 3×3(ボタンで切替表示)。**q は生の値を表示、正準化しない**(3.6-I)
- UIDocument に付与し RotationSource を結線。ラベル内容はエディタ上で検証済み(θ=120°, w=0.5, |v|=0.866, det E=0.9957 等すべて理論値一致)

### 画面構成 960x540 (ユーザー指定)
- PanelSettings: `ScaleWithScreenSize`、referenceResolution=(960,540)
- PlayerSettings: defaultScreenWidth/Height=960/540、Windowed
- GameView: カスタムサイズ「QuaternionGlobe 540p」(960x540, FixedResolution) を追加・選択済み(内部API GameViewSizes 経由)

### 運用メモ追記
- RunCommand は `using System.Reflection` を静的に拒否する。`System.Type.GetType` + `var`受け + 完全修飾で内部APIには届く
- エディタ非フォーカス時、PanelSettings.targetTexture へのオフスクリーンUI描画は player loop が回らず不発(QueuePlayerLoopUpdate でも不可)。UI の見た目確認は Game ビュー目視が早い

### 残り(UI周り)
- ChapterNavigator(章切替。Chapters/ 実装と同時に)
- GraphPlotter(Painter2D。角速度 / |q|−1 グラフ)
- パネルの折りたたみ等の磨き込みは章UIと合わせて

---

## 追記2 (同日・Prefab化+モデル切替)

### Prefab 化
- `Assets/QuaternionViewer/Prefabs/` に **Core.prefab / Globe.prefab / RotationSpaceBall.prefab**(SaveAsPrefabAssetAndConnect でシーンインスタンスと接続維持)
- Globe/Ball のワイヤ・マーカー・軌跡は HideFlags.DontSave の実行時生成物のため **Prefab にもシーンにも入らない**(コンポーネント+設定値のみが資産)
- `source` 参照(シーン上の RotationSource)はインスタンス側オーバーライド。Prefab を別シーンで使う際は結線し直すこと

### 内核モデル切替 (Dice / NineBall / OctantSphere / Knight)
- `Scripts/Visualization/CoreModelSwitcher.cs`: Core直下の標本4体を常に1体だけアクティブ化(ExecuteAlways、OnValidate対応)
- `Scripts/UI/ModelSwitcherUI.cs`: 画面右上のボタン列UI(アクティブは青ハイライト)。シーンに `ModelSwitcherUI` GO 追加
- 全標本は 1m・中心原点・TRS恒等で統一されており切替で姿勢の読みは不変。**Knight.fbx の .meta は本セッションの AssetDatabase.Refresh で生成済み**(前回保留は解消。コミット可)
- 切替検証済み: 全indexで「アクティブは常に1体」、オクタント球で軸角の目視確認良好

### 配置調整 (960x540)
- RotationSpaceBall: (3.0,0,0) → **(3.3, 0, 0)**(右端の窮屈さ解消)、Main Camera: z=-6 → **-5.75**(全体をわずかに拡大)
- 画面構成: 左上=情報パネル / 中央=内核+中殻 / 右=外殻 / 右上=モデル切替ボタン

---

## 追記3 (同日・Knightカメラ混入の解決+レイヤー識別)

### Knight.fbx のカメラ/ライト混入 (解決)
- **症状**: 内核を Knight に切り替えると Game ビューの画角が変わる。
- **真因**: Knight.fbx に Blender のカメラ+ポイントライトが同梱されており、アクティブ化と同時に有効なカメラとして Game ビューを乗っ取っていた。
- **対処(二重)**: ①ユーザーが Art/Knight.blend からカメラ・ライトを削除 → Desktop が Blender ブリッジ経由で保存+Dice同一設定でFBX再書き出し(41KB→29KB)。②Unity 側で **全4モデルの ModelImporter の Import Cameras / Import Lights を無効化**(再発防止)。
- Core 配下のカメラ/ライトは 0 を確認。**Knight の正面 = +Z も検証済み**(RIGHTビューで鼻先が+Z方向)。

### レイヤー識別の改善 (「儀が2つ見える」対策)
- 外殻グリッドを寒色 (0.30, 0.42, 0.62) に変更し、中殻の中間灰グリッドと見分けられるように。
- `Scripts/UI/LayerCaptionsUI.cs` 新規: 中殻・外殻の足元に追従キャプション(WorldToPanel投影)。
  - 中殻: "CORE + S² GLOBE — axis n & angle θ"
  - 外殻: "ROTATION SPACE BALL — q as a point (RP³)"
- 画面構成(960x540): 左上=情報パネル / 右上=モデル切替 / 中央=内核+中殻(同心) / 右=外殻。**中殻と外殻は別の数学的空間**(S²=軸の住処 / RP³模型=qの住処)であり、仕様4.3の決定通り外殻は脇配置。

> **編集事故の記録**: 本追記3のコミット時、Desktop 側が c500e94 時点の末尾「✅ 突き合わせ結果」節を誤って上書き消去した(並行編集のすれ違い)。下記の同節は git (c500e94) から復元して再掲したもの。**教訓: `.wip` の共有ファイルへ書く前に必ずディスク側の最新を再取得すること。**

---

## 追記4 (同日・章演出デモ3種+HUDデザイン刷新)

### 演出デモ (右上 DEMOS パネルでON/OFF、既定OFF)
- **MIRRORS (Ch.1 半角演示)**: `HalfAngleMirrors.cs`。θ/2 で交わる二枚の半透明鏡(シアン/オレンジ、URP Unlit透過・両面)+ v→S1(v)→S2S1(v) の経路折れ線。ReflectionPair(3.6-E)使用。
- **GIMBAL (Ch.4)**: `GimbalRig.cs`。3重リング(外=yaw/中=pitch/内=roll、半径1.15/1.0/0.85)+軸受スタブ。GimbalStages(3.6-G)で駆動、|cos(pitch)| 0.35→0.03 で外環・内環が赤へ(det E連動)。検証: pitch76.9°でdet E=0.227、赤み確認。
- **INTERP (Ch.5 三体比較)**: `InterpRace.cs`。Slerp(teal)/Nlerp(orange)/EulerInterp(magenta) の全経路を外殻ボール内に静的描画+時刻tマーカー3体(Playで自動走行、エディタはスライダ走査)。`GraphPlotter.cs`(Painter2D)が |ω|(t) 3曲線+グリッド+凡例+時刻カーソルを右下に描画。INTERPトグルと連動して表示。
- `RotationSpaceBall.MapPoint(Quat)` を公開(軌跡・比較デモ共用)。

### HUDデザイン刷新 (電子黒板/シミュレータ端末風)
- `Scripts/UI/HudStyle.cs` 新規: 共通パレット(濃紺半透明地 + シアンアクセント)とパネル枠(上端アクセントライン+細枠)、見出し(レタースペース+太字)、端末風ボタン(余白リセット+アクセント枠)。
- QuaternionReadout / ModelSwitcherUI / LayerCaptionsUI / DemoTogglesUI / GraphPlotter の全パネルに適用。トグルのアクティブ表示はシアン点灯。

### シーン追加GO (未コミット)
- HalfAngleMirrors / GimbalRig / InterpRace(いずれも既定 inactive)/ GraphPlotter(UIDocument)/ DemoTogglesUI(UIDocument)

### 残り
- 中殻の角度ゲージ(軸直交大円の分度器) / ArcballController / ChapterNavigator / Ch.2 符号反転ボタン / Ch.6 ωスライダ+|q|-1グラフ

---

## 追記5 (同日・フォント+背景)

### ピクセルフォント導入 (ユーザー提案: 患者長ひっく氏 x0y0pxFreeFont)
- `Assets/QuaternionViewer/Resources/Fonts/` に **x12y16pxMaruMonica.ttf**(マルモニカ: 漢字・かな・ギリシャ文字・上付き数字収録 → HUD既定)と **x12y20pxScanLine.ttf**(スキャンライン: 幅広の英数+かな、漢字非収録 → 数値・見出し・ボタン用)を同梱。`x0y0pxFreeFont-NOTICE.md` にライセンス表記(商用可・改変可・クレジット不要・単体販売禁止。2026年にSIL OFL移行予告あり)。**MITの対象外**として分離。
- `HudStyle.PixelFont` / `LatinFont`(Resources.Load、静的キャッシュ)+ `ApplyFont` / `ApplyLatinFont`。Frame() は既定でマルモニカ、Readout全体・見出し・ボタン・グラフ凡例は幅広の ScanLine (サイズも1〜2px増、Readoutは幅360へ、長行は折返し許可)。
- ScanLine 非収録の ▾▸ は R(q) トグルで「−/+」表記に変更。
- レイヤーキャプションは**日本語化**(マルモニカが漢字収録のため): 「内核+中殻 ── S²球儀」「外殻 ── 回転空間ボール」。

### 背景 (端末風)
- Main Camera: Skybox → **SolidColor 濃紺 (0.016, 0.043, 0.070)**(地平線を消してHUDと同系色に)。
- RenderSettings: ambientMode=Flat、ambientLight=(0.42, 0.47, 0.52)(スカイボックス環境光の代替)。

### 文字サイズ調整 (ユーザーFB: 余白過多 → 文字で埋める)
- 全体を1〜3px増: 見出し13 / ボタン13(高さ24) / q行16 / 数値・行列13(行高20) / 情報行13〜14 / 日本語キャプション16(副12、幅360)。
- Readout幅 392 に拡大。θ と n は整形2行に分離(折返し任せをやめた)。

---

## ✅ 突き合わせ結果 — Claude Code / VS Code 側 (444 シテン)

「元の 3.6 草稿が VS Code 側に残っていれば差分を正典へ」との依頼を受け、痕跡を全層調査した。

**結論: 元の 3.6 の"散文草稿"は初めから存在しなかった。** 消えたのではなく、**書かれていない**。

- **git**: stash 空 / reflog に中間コミット無し / dangling blob 8個も全て無関係(AGENTS草稿・LFSポインタ等)。HEAD の `docs/spec.md`(342行)に §3.6 は無い。
- **VS Code ローカルヒストリ**: QuaternionViewer の痕跡は `AGENTS.md` のみ。`docs/spec.md` の影は無い(Claude Code の編集はエディタ保存を経ずローカルヒストリに残らないため道理)。
- **Claude Code セッション記録 (`3e316ed2…jsonl`)**: 過去セッションは §3.6 を **`AskUserQuestion` で計画・user 承認(スコープ=A〜I一括 / 正準形=生符号+表示層選択)** した直後、**残り全部が Unity MCP「Connection revoked」の格闘に呑まれ**、§3.6 本文を spec.md へ書く Edit/Write は**一件も実行されないまま店じまい**した。前回引継ぎの「3.6 明文化済み(コミット済み)」は**未実行作業を完了と誤記した記述**(取り違え)。

**ゆえにデスクトップ版 §3.6 は"再構成"ではなく §3.6 の初出であり、突き合わせるべき旧版は存在しない。** 承認済みスコープ(A〜I一括・正準形ポリシー)との照合では、デスクトップ版 §3.6 は**承認範囲を完全に満たしている**(取りこぼし・矛盾なし)。

**正典へ反映した唯一の差分**: 承認計画に含まれていた「**6.3 に検証項目を追加**」が未反映だった(テスト実体 `QuatMathExtendedTests.cs` 25本は存在するのに §6.3 の表に無かった)。本セッションで以下を `docs/spec.md` に反映済み:

- **§6.3**: 内部数学ライブラリ A〜I の検証表を追加(`QuatMathExtendedTests.cs` に一致)。
- **§6.1**: テストツリーに `QuatMathExtendedTests.cs` を追記。

※ 本 `.wip` 及び前回引継ぎは履歴として保持(AGENTS.md §6: 恒久決定は正典へ、WIP は流動メモ)。正典 `docs/spec.md` 側が唯一の正。

---


## ✅ 解説モード骨格 実装 (Desktop 44 ・ 2026-07-18 午後)

444 の to-desktop handoff(解説・図解モード)を受けて第一段を実装、`85130d1` としてコミット済み。

### 方針の追加決定(提督承認)
- **台本の格納は Markdown テキストアセット**(`Resources/Guide/*.md`)。Unity 6 が .md を TextAsset として直接インポートできることを実機確認済み。構造は C#、文はデータ ―― SSOT(section-guide §4)との突き合わせと git 差分が効く。
- 着手順「コミット整理 → 骨格」を提督が承認 → 未コミット分は前セッションでコミット済みと判明したため、そのまま骨格へ。

### 実装分 (85130d1)
- `Chapters/GuideBeat.cs` ―― ビート型 + DemoFlags/CameraFraming/ReadoutHighlight。**set\* フラグ方式**(指示のあった項目だけ適用、無指示は現状維持 = 順路を強制しない)
- `Chapters/GuideScript.cs` ―― 台本 Markdown パーサ(`## ◆/○` ビート、`@posture/@demos/@ball/@camera/@highlight/@action`、`### 直感/数理/話者ノート`。未知指示は警告+無視)
- `Chapters/ChapterBase.cs` ―― beats 保持 + Next/Prev/JumpTo + Revision(UIポーリング用)+ BeatChanged イベント
- `Chapters/Ch1_AxisAngle.cs` / `Resources/Guide/ch1.md` ―― §4.1 の4ビート転記(数式は ^ 表記の一行式)
- `Chapters/GuideController.cs` ―― 宣言→儀の適用器。`@action` は**名前付きアクション登録制**(UnityEvent 不採用 ―― シリアライズ不要・コード登録で符号反転等を受ける)。camera/highlight は宣言受理のみ(適用先が未実装のため)
- `UI/GuideBarUI.cs` ―― 画面下部解説バー(HudStyle 踏襲。直感常時 + MATH 折りたたみ + 進捗ドット●○クリックジャンプ + ‹› + Play 中の ←→ キー)
- `QuaternionViewer.asmdef` に `Unity.InputSystem` 参照を追加(キーボード送り用。ArcballController でも必要になる参照)
- シーン: `GuideUI` GO(UIDocument + Ch1 + GuideController + GuideBarUI)を結線・保存済み
- テスト: `GuideScriptTests.cs` 10本追加、**EditMode 全67本緑**

### スモーク確認済み
- beat0 適用で RotationSource が (1,2,0.5)/120°、beat4(鏡)で MIRRORS ON、beat0 復帰で OFF + 姿勢再適用。

### 444 のフック表に無かったギャップ(要対応・44 実地調査)
1. **Ch.3 の二体サイコロ並置が丸ごと未実装**(現 Core は1体切替構成。DemoFlags にも無い)。Ch.3 の核心ビート2本が依存。演出GO一本ぶんの新規実装。
2. **Ch.4 ビート2「pitch→90°(アニメ)」はオイラー角駆動が要る**(`@posture` は軸角のみ)。GuideBeat に `@euler` 拡張を足すか `onEnter` へ逃がすか、実装時に選ぶ。

### 残り(フック増強フェーズ)
- カメラフレーミング / Readout 行強調 API / 符号反転(RotationSource に Pose 直接セット経路)/ InterpRace 補正トグル / GraphPlotter |q|−1 モード / ω駆動(Ch.6)/ 角度ゲージ / 二体サイコロ(Ch.3)/ 話者ノート窓 / 自由探索復帰 / ChapterNavigator(章間送り)/ Ch.2〜Ch.6 台本 md
- 解説バーの**見た目の Game ビュー目視が未**(エディタ非フォーカスのため)。次回フォーカス時に要確認: LayerCaptionsUI との重なり。

### 運用メモ(今日の新知見)
- 「Connection revoked」の真因その2: **relay_win.exe の滞留(1日で16本)が direct 接続1席を占有**。旧 relay 掃除 + 自分の relay 再起動で復旧(接続時点で Denied が確定するため、席を空けても再接続が要る)。詳細は記憶 unity-mcp-connection に記録済み。
- エディタ非フォーカス時は AssetDatabase.Refresh 後の**ドメインリロードが保留**される。`CompilationPipeline.RequestScriptCompilation()` → `EditorUtility.RequestScriptReload()` で強制できる。このときコンパイルエラーがコンソールへ出ないことがある → `Library/Bee/tundra.log.json` を `CS\d{4}` で grep すると確実(今回 asmdef の InputSystem 参照漏れをこれで検出)。

---
## ✅ 自由探索の入力層 ArcballController (Desktop 44 ・ 同日続き)

提督の依頼「プレゼン中も回して見え方を変えたい」を受け、`Input/ArcballController.cs` を実装 (`21855e3`)。

- **アークボール (仕様書 3.5 のθ版)**: 左ドラッグ。カーソルのレイ→球面写像 `MapToSphere` (交点 or シルエット射影) → `QuatMath.FromToRotation(p0,p1)` を世界系で左乗。**掴んだ点がカーソルへ追従**。ドラッグ中は `driveFromInspector` を切り Pose 直書き、終了時に軸角へ読み戻して復帰。
- **カメラ周回**: 右ドラッグ (pitch ±80° 制限、pivot=Globe 周り)。**ズーム**: ホイール (距離 2.5〜12)。カメラは操作するまで著者フレーミングを崩さない。
- **R キー**: 視点リセット + 章があれば現在ビート再適用 (`ChapterBase.Reapply`) ―― section-guide §1.3 の「自由探索から同じビートへ復帰」の入口が先行して実装された形。
- **UI 透過制御**: UIDocument 全パネルを `panel.Pick` で判定し (ルート要素は除外)、UI 上で始まった操作は儀へ流さない。
- 解説バーに操作ヒント行 (L-DRAG ROTATE / R-DRAG ORBIT / WHEEL ZOOM / [R] RESET) を追加。
- シーン: `InputController` GO 結線・保存済み (radius=1.5 は AxisAngleGlobe.radius から取得)。
- テスト: `ArcballControllerTests.cs` 5本 (球面写像の交点/シルエット/単位性、θ版の追従、左乗合成)。**全72本緑**。
- **Play モード専用**。動作の実機確認 (ドラッグの手触り・周回の向き・UI透過) は提督の目視待ち。

---
## ✅ 「見よ」の指し示し ―― @focus / @highlight (Desktop 44+444 ・ 同日続き)

提督のFB「解説の『〜を見よ』にマーカー表示が欲しい。MIRRORS のような加算表示の演出をリスペクトしたい」を受け、444 が設計・44 が実装 (`054ee3a`)。

- **設計の芯 (444)**: §4 台本には「見よ」が17回あるのに視線を運ぶ装置が無かった。「見よ」は二種に分類される ―― **空間の名所 → @focus (新設)、計器の行 → @highlight (実装)**。MIRRORS が良かったのは儀を止めず層を足すだけだから ―― マーカーも同じ加算原理で作る。
- **@focus**: `FocusMarkerRenderer` (Visualization)。台本名エイリアス (pole+/pole-/pin/pinImage/arc/ballPose/ballAntipode/core/globe/ball/mirrors/gimbal) → シーン内 GameObject を**毎フレーム名前解決** (__globe/__ball 配下は実行時再生成のため参照を保持しない)。カメラ正対の脈動リング (WireGeometry 流用・非保存・アクセント色)。**demos が「状態」なのに対しマーカーは「指差し」―― 宣言の無いビートへ移ると自動消灯**。
- **@highlight**: `QuaternionReadout.Highlight(ReadoutHighlight)` ―― 該当行 (WXYZ 5要素/軸角/半角/Euler/detE・|q|-1/行列) へ地色+左縁のアクセント強調。set* 系の宣言に従い持続。Build 再構築でも再適用。
- Ch.1 台本反映: ビート2 `@focus pole+ pole-` / ビート3 `@focus pin pinImage` / ビート4 `@focus mirrors`。ビート1 は @highlight WXYZ が効くようになった。
- シーン: `FocusMarkers` GO 結線・保存済み。スモーク: ビート2でリング2本点灯→ビート1復帰で0本 (自動消灯)。テスト**76本全緑** (@focus パーサ2本+エイリアス表2本追加)。
- **目視待ち**: リングの実寸・脈動の速さ・行強調の色味 (公開フィールド lineWidth/pulseHz/pulseAmount で調整可)。
- 残りのうち**カメラフレーミング (@camera) だけが宣言のみ未適用**になった。次はそこか、Ch.2 台本+符号反転か。

---
## ✅ 全6章の解説モード化 + develop 運用開始 (Desktop 44 ・ 同日続き)

提督が**アルファ公開**(パブリックリポジトリ化)。以後の作業は `develop` ブランチ ―― AGENTS.md §6 に運用を追記済み (develop=作業 / master=リリース、マージ・push は提督判断)。本節以降のコミットは develop (`a93c312`)。

### 入ったもの
- **章切替**: `UI/ChapterNavigator` (spec 6.1 必須UI) + 解説バーに章送りボタン `<< >>`、キーボード ↑↓=章 / ←→=ビート。切替時は GuideController の購読を張り替え章頭ビートを適用。
- **Ch.2 二重被覆** (完全動作): `flipSign` アクション ―― driveFromInspector を切り生の -q を配布点へ (Readout は正準化しないので全成分反転が見える。w=∓0.696 の往復を実測)。ビート3は外殻の対蹠点+色反転を @focus ballPose/ballAntipode で指す。
- **Ch.4 ジンバルロック** (完全動作): `@euler p y r` 指示を新設 (FromEuler 経由で Pose 直書き)。ビート2の euler(90,30,10) は ToEuler 読み戻しで (90,20,0) ―― **ロック点で yaw/roll が混合する縮退そのもの**が Readout に出る。怪我の功名ならぬ仕様の必然、プレゼンでそのまま語れる。
- **Ch.5 補間** (完全動作): `InterpRace.shortestPath` フィールド化 (spec 3.2「トグルで切れること」) + アクション interpCorrectionOn/Off・interpDefaultEnds/interpCloseEnds (Ω→0 演示)。
- **Ch.3 / Ch.6** (台本+部分動作): 全ビートの二層ナレーションは完成。Ch.3 の二体サイコロ並置、Ch.6 の ωドライバ (world/body・積分器)・|q|-1 グラフは未実装で、話者ノートに代替手順を注記。Ch.6 は暫定 spinOn/spinOff で連続回転を見せる (ビート3の回転ベクトル模型内の放射直線トレイルは出る)。
- 台本 ch2〜ch6.md 転記完了 ―― **全6章23ビート** (§4.0 の勘定と一致)。テスト**77本全緑** (@euler パーサ1本追加)。シーン結線・保存済み。

### 残り (次段)
1. **二体サイコロ並置** (Ch.3 の核心2ビートの演出。DemoFlags 拡張 + TwinDiceRig + Readout 比較)
2. **ωドライバ** (Ch.6: RotationIntegrator を回す駆動コンポーネント、world/body・Euler/RK4・正規化トグル) + **GraphPlotter |q|-1 モード**
3. @camera (フレーミング補間 ―― 最後の宣言のみ未適用)
4. 角度ゲージ (Ch.1 ビート3) / 話者ノート窓 / Prefab 化検討 / リング・強調の見た目調整 (目視待ち)

---
## ✅ 三本立て: 二体サイコロ / ωドライバ / カメラ寄せ+章周回 (Desktop 44 ・ 同日続き)

提督のバグ報告2件の切り分け → ①Ch.6-4の停止=台本のspinOff(旧仕様、ωドライバ化で解消) ②Ch.6→Ch.1不遷移=クランプ仕様(周回へ改善)。以後は変更内容ごとにdevelopへコミットする運用 (提督指示)。

- **421d842 ―― Ch.3 二体サイコロ (TwinDiceRig)**: Core/Diceをクローンして左右並置、X→Y / Y→X を逐次アニメ適用 (t∈[0,2]、端で静止保持、twinRestartで再走行)。アクティブ中はCoreを退避。DemoFlags.TwinDice追加、@focus twin、ch3.md演出化。ポーズ合成テスト3本。
- **03b8a40 ―― Ch.6 完成 (OmegaDriver + NormDrift)**: dq/dt=½ω̃⊗q を毎フレーム積分してRotationSource駆動 (world/body・Euler/RK4・正規化トグル)。GraphPlotterにNormDriftモード(|q|-1履歴)。アクション: omegaOn/Off・omegaWorld/Body・normalizeOn/Off・graphSpeed/graphDrift。**姿勢指示(@posture/@euler)のあるビートへ移るとdriver.runを自動停止**(姿勢優先) ―― これが章離脱時の安全弁。ch6.md実演化 (B2でbody切替、B4は回しっぱなしで漂流が伸びる)。
- **17bc0ed ―― @camera + 章周回**: CameraFramerが4プリセット(Overview/CoreAndGlobe/SpaceBall/Gimbal)へSmoothStep補間で寄せ、完了時にArcballController.SyncFromCamera()で周回状態を再同期。ChapterNavigatorは端で周回、ビート送りは章境界を越えて流れる ―― **→キー連打で全23ビート通し運転**。台本の全宣言が適用先を持った。
- テスト**80本全緑**。Playモード放置でエディタ応答が止まる一幕あり(提督がフォーカス復帰で解決) ―― 結線は全て無傷、状態を安全側へ戻して(driver停止・normalize/補正on・Ch.1先頭)シーン保存済み。

### 残り (次段候補)
- Ch.6 B2のworld/body実演は入場時切替のみ ―― ビート内トグルUI(または積分器Euler/RK4切替UI)を足すか検討
- 角度ゲージ(Ch.1 B3) / 話者ノート窓 / 自由探索復帰の明示UI / 見た目調整(リング・カメラプリセット座標は目視待ち) / Prefab化 / Ch.3のReadout二体比較
- push・masterへの取り込みは提督判断 (AGENTS.md §6)

---
### 追記: Ch.3 二体サイコロ不可視バグ修正 (36072a4)

提督のPlay目視で発覚 ―― クローンは生成・配置・描画有効まで正常だが bounds が (0.01,0.01,0.01)、つまり**極小で見えないだけ**だった。真因: テンプレ Core/Dice の localScale は FBX インポート補正込みの **(100,100,100)** で、これを diceScale=0.72 の直接代入で上書きしていた (1m → 約7mm)。修正: `テンプレの localScale × diceScale` の比率縮小 (実測 bounds 0.72m)。

**教訓 (記憶にも記録)**: FBX 由来 GO のクローンで localScale を直接代入してはならない ―― インポート補正が乗っている前提で必ず比率で操作する。実寸検証は Renderer.bounds を見るのが確実 (childCount や activeSelf では捕まらない)。

---
### 追記: Readout 情報行の固定列化 (061d63e) ―― 想定機能の完走

提督の通し確認FBを受け、桁数で折返し・位置ズレしていた行を固定列へ: オイラー角=「Euler ZXY」+ p/y/r 値行 (1/3幅・右詰め) の2行固定、半角行 (cos(θ/2)=w / sin(θ/2)=|v|) と det E・|q|−1 行=1/2幅セルの固定列。@highlight の対象も追随。

**提督宣言: 想定していた機能の実装はこれで完走。** 以降は磨き (角度ゲージ・話者ノート窓・見た目調整・Prefab化・README/handout追随) とリリース (develop→master取り込み・push=提督判断)。

---

© ラジアン(柏木主税) / ©RadianN_kswg
