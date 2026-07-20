# 2026-07-21 unityroom向けWebGLビルド設定セットアップ (44/シトシ)

## やったこと

- **unityroom要件の調査**: Compression Format=**Gzip** / Decompression Fallback=**無効**(公式ヘルプ: https://help.unityroom.com/unityroom-351dc3ed5de980eebd79eef3b153be31 )。画面サイズはuserと相談し **960×540 (16:9)** に決定。
- **エディタメニュースクリプト新設** (`Assets/QuaternionViewer/Editor/`):
  - `UnityroomBuildTools.cs` — メニュー3種:
    - `Tools > unityroom > Apply WebGL Settings` … Gzip/フォールバックOFF/960×540/runInBackground=ON/シーンリスト修正を適用
    - `Tools > unityroom > Build WebGL` … 設定適用+ `Builds/WebGL-unityroom` へビルド
    - `Tools > unityroom > Log Current Settings` … 現在値の確認ログ
  - `QuaternionViewer.Editor.asmdef` (Editor専用asmdef、本体`QuaternionViewer`を参照)
- **Scenes In Buildの不備を発見**: SampleSceneのみでQuaternionGlobeが未登録だった → Apply時に `[QuaternionGlobe.unity]` のみへ是正するようスクリプト化。
- `.gitignore` に `/[Bb]uilds/` を追加(ビルド出力の混入防止)。

## MCP接続まわりの新知見(重要)

- relay_win.exe はMCPモードで **`--project-path <path>` / `--instance-id <editor PID>`** を受け付ける。**無指定だと起動中の任意のエディタに自動接続する**(今回AstroScopeエディタに誤接続しかけた。pipe名末尾がエディタPID)。複数プロジェクト並走時は必ず指定すること。
- 「Connection revoked」の第3の真因: **Org接続枠1/1を別セッションが占有**(`Your MCP connections limit is reached (1/1)`)。今回は舞(AstroScope担当・VS Code Claude Code)のrelayが無指定起動でQV側エディタにも接続し枠を占有していた。
- 台帳は `Library/AI.MCP/connections-v2.asset`(読み取りで診断可能)。`claude-cowork-44` は auto-approve 状態を維持。

## 残タスク・申し送り

- [x] `.meta` はエディタの自動リフレッシュで即生成され、初回コミット(33ae0b2)に同梱済み。
- [ ] `Tools > unityroom > Apply WebGL Settings` を一度実行して設定を永続化(MCP席が空けばオレがMCP経由で実行・Console検証する。userが手動クリックでも可)。
- [ ] 実ビルド(`Build WebGL`)は初回プラットフォーム切替を伴うため時間がかかる点に留意。
- [ ] unityroomのゲーム登録フォームには画面サイズ **960×540** を入力する。
- pushはuser指示があるまでしない(AGENTS.md 6章)。
