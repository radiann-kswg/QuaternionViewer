# 2026-07-21 WebGLでモデル非表示バグの修正 (44/シトシ)

## 症状
unityroom公開版でHUDは出るが3Dモデル(ダイス/球/ワイヤー/ミラー等)が一切表示されない。

## 原因(確定)
ブラウザConsoleに `ArgumentNullException: Value cannot be null. Parameter name: shader` が多発。
`Shader.Find("Universal Render Pipeline/Unlit")` が **WebGLビルドで null** を返し、`new Material(null)` で例外→マテリアル生成失敗→非表示。
- 該当箇所: `WireGeometry.cs:18-20`、`HalfAngleMirrors.cs:42-43`。
- 理由: どのマテリアル/シーンからも直接参照されないシェーダはビルド時にストリップされる。`Shader.Find`はランタイム解決なのでビルド同梱の根拠にならない。

## 対策(コミット 37a4f3c)
`UnityroomBuildTools.cs` の `ApplySettings` で、**Always Included Shaders** に以下を登録:
- `Universal Render Pipeline/Unlit`(guid 650dd952…、GraphicsSettings.asset に永続化確認)
- `Unlit/Color`(組み込みフォールバック、fileID 10755)

### ハマりどころ(重要な知見)
- 当初 `AssetDatabase.LoadAssetAtPath("ProjectSettings/GraphicsSettings.asset")` を使っていたが、**AssetDatabaseは`Assets/`と`Packages/`しか扱えず`ProjectSettings/`配下はnull**を返す→黙って失敗していた。正しくは `GraphicsSettings.GetGraphicsSettings()` で実体を取得して `SerializedObject` 化する。
- `AssetDatabase.SaveAssets()` は ProjectSettings/*.asset をディスクにフラッシュしない。`EditorApplication.ExecuteMenuItem("File/Save Project")` を併用して確実に書き出す(ApplySettings末尾に追加済み)。

## 検証
- GraphicsSettings.asset に2シェーダが追記されたことをディスクで確認。
- `Tools > unityroom > Build WebGL` 成功(18.2MB / 323秒)。出力: `Builds/WebGL-unityroom/`。

## 残タスク(userへ申し送り)
- [ ] **再アップロード**: `Builds/WebGL-unityroom/Build/` の4ファイル(.loader.js / .data.gz / .framework.js.gz / .wasm.gz)を unityroom のゲーム編集画面から差し替える(ログインが要るためuser作業)。差し替え後、ブラウザConsoleに `ArgumentNullException ... shader` が消えていること・モデルが表示されることを確認。
- [ ] **File/Save Project の副産物**: 保存時にエディタが以下を再シリアライズした。修正本体とは無関係のため**未コミットで保留**。userの判断でコミット要否を決めること。
  - `Assets/Settings/PC_RPAsset.asset` / `UniversalRenderPipelineGlobalSettings.asset` / `DefaultVolumeProfile.asset`(URPのプリフィルタ/ランタイム設定の再生成)
  - `ProjectSettings/ProjectSettings.asset`(WebGLのScriptingDefine追加、Standaloneのバッチング)
  - `ProjectSettings/UnityConnectSettings.asset`(**Analytics/Unity Connect が m_Enabled 0→1**。公開設定に関わるので特に要判断)
- pushはuser指示があるまでしない(AGENTS.md 6章)。
