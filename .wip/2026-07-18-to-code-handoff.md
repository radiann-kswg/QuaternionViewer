# 引継ぎ 2026-07-18 — Desktop → Claude Code (444 シテン宛)

- **From**: Claude Desktop (44 シトシ / 一部 444 シテン)
- **To**: Claude Code / VS Code (444 シテン、および次セッションの任意のエージェント)
- **経緯の詳細**: [`2026-07-18-desktop-session.md`](./2026-07-18-desktop-session.md)(追記1〜5+突き合わせ結果)
- **spec 3.6 の確定経緯**: [`2026-07-18-spec36-handoff.md`](./2026-07-18-spec36-handoff.md)(貴君の調査に感謝する——3.6 は Desktop 版が初出で正典確定)

---

## ⚠ Unity MCP の席

例の1席制は健在だ。**本引継ぎ時点で席は Desktop 側が使用していた**。Code 側で Unity を触る前に、Desktop セッションが休止していることを確認してくれ(逆に Desktop を使う時は Code 側を閉じる)。詰まったら `relay_win.exe`(親が Unity でないもの)を全部 kill → 叩き直しで席が空く。

## 現在地

### コミット済み (master・未 push)
`7226e44` spec3.6+6.3 / `093ef8d` Core A〜I+テスト25 / `72df03b` シーン / `7541b3b` UI / `c500e94` 引継ぎログ。**EditMode テスト 57本全緑**。

### 未コミット (今回の Desktop 作業分。コミット分割の提案付き)

**① feat: 三層Prefab化+内核標本切替**
- `Assets/QuaternionViewer/Prefabs/` Core / Globe / RotationSpaceBall(SaveAsPrefabAssetAndConnect済み)
- `Scripts/Visualization/CoreModelSwitcher.cs` + `Scripts/UI/ModelSwitcherUI.cs`(右上ボタン列)
- `Models/*.fbx.meta` 4体: **ModelImporter の Import Cameras/Lights を無効化**(Knight にBlenderカメラ+ライトが同梱されGameビューを乗っ取った事故の再発防止)
- `Models/Knight.fbx` 再書き出し版(41KB→29KB、カメラ・ライト除去済み。Blenderブリッジ経由で書き出し。正面=+Z検証済み)
- `Art/Knight.blend` 保存済み(ユーザーがカメラ・ライト削除)

**② feat: 章演出デモ3種+グラフ**
- `Scripts/Visualization/HalfAngleMirrors.cs`(Ch.1 半角演示: θ/2二枚鏡+鏡映経路。ReflectionPair 3.6-E)
- `Scripts/Visualization/GimbalRig.cs`(Ch.4: 3重リング、GimbalStages駆動、|cos(pitch)|→0で外環・内環が赤)
- `Scripts/Visualization/InterpRace.cs`(Ch.5: Slerp/Nlerp/Euler の3軌道を外殻内に描画+tマーカー)
- `Scripts/UI/GraphPlotter.cs`(Painter2D。|ω|(t) 3曲線+凡例+時刻カーソル。Slerpだけ水平線になる)
- `Scripts/UI/DemoTogglesUI.cs`(右上 DEMOS: MIRRORS/GIMBAL/INTERP トグル+外殻模型切替 BALL: sin(θ/2)n ⇄ θn/π)
- `RotationSpaceBall.cs` 改: `MapPoint(Quat)` 公開+グリッド寒色化

**③ feat: HUDデザイン(端末風)+ピクセルフォント**
- `Scripts/UI/HudStyle.cs`(共通スタイル集約: Frame/Header/Button/ApplyFont/ApplyLatinFont)
- `Resources/Fonts/` に患者長ひっく氏 x0y0pxFreeFont 2種+`x0y0pxFreeFont-NOTICE.md`
  - マルモニカ(漢字あり)=既定 / ScanLine(幅広英数、**▾▸非収録**→−/+表記に置換済み)
  - **ライセンス**: 商用可・改変可・クレジット不要・単体販売禁止。**MIT対象外**として NOTICE で分離(LICENSE-CHARACTERS.md と同じ扱い。READMEへの追記は未実施→やってくれると助かる)
- `QuaternionReadout.cs` / `LayerCaptionsUI.cs`(吹き出し3層+Painter2D引き出し線) 刷新
- カメラ背景 SolidColor 濃紺+Flat環境光 / PlayerSettings 960x540 Windowed / GameViewカスタムサイズ「QuaternionGlobe 540p」

**④ シーン+ログ**
- `Assets/Scenes/QuaternionGlobe.unity`(全結線・デモGOは既定inactive)
- `.wip/` 3ファイル

### レイアウト規約 (960x540基準。変更時はここを更新)
Readout 幅336/左上 (中殻左縁x≈363に不干渉) / CORE MODEL top12 / DEMOS top84 / 吹き出し中心=画面幅比17.5%・46%・79%、下端-86px。文字: 見出し13・ボタン13・q行15・数値13・和文キャプション16。**数式行は圧縮表記で折返しを出さない**のが規約。

## 次の入り口 (優先度はユーザーと要相談)

1. **未コミット分の整理**(上記①〜④の分割を提案)
2. 中殻の**角度ゲージ**(軸直交大円の分度器。仕様4.2、ĝ0=退化時カメラ右方向退避)
3. **ArcballController**(Input System。数式3.5は `QuatMath.FromToRotation` で流用可。導入時は RotationSource.driveFromInspector を切る)
4. ChapterNavigator / Ch.2 符号反転ボタン / Ch.6 ωスライダ+|q|−1グラフ(GraphPlotter を流用可能な作りにしてある)

## 運用注意 (Desktop で踏んだ地雷)

- **`.wip` 共有ファイルは書く直前にディスク側を再取得**(貴君の追記を上書きした事故が1件。c500e94から復元済み・再発防止をログ化)
- エディタ非フォーカス時は ExecuteAlways の Update が回らない → `SendMessage("Update")` / `QueuePlayerLoopUpdate()` で駆動
- RunCommand: `using System.Reflection` 静的拒否 / ネスト private クラス不可 / TestRunnerApi+ICallbacks でテスト実行可
- 新フォントで文字を使う前に cmap を確認(▾▸の前例)

> 理屈は貴君が固め、オレが画面に出した。ここから先の演出も、正典 §3.6 と §6.3 に従う限り食い違いは起きねぇはずだ。あとは頼んだぜ。 ―― 44

© ラジアン(柏木主税) / ©RadianN_kswg
