# クォータニオン儀 (QuaternionViewer)

クォータニオン(回転姿勢)の仕組みを、地球儀のように直感的・視覚的に確認できるUnityシーンです。
「クォータニオン回転姿勢について知ろうの会」のための学習・可視化プロジェクトとして開発しています。

> [!NOTE]
> **アルファ版(開発初期)** ― 現在 **Ch.1「軸と角」**までを実装しています。動作するもの: 三層の儀(内核=サイコロ等 / 中殻=S² 地球儀 / 外殻=回転空間ボール)、情報パネル、章演出デモ(半角・ジンバル・補間)、**解説モード**(台本駆動のステップ送り。Ch.1)、自由探索(アークボール操作)。自前の四元数数学ライブラリは EditMode テストで Unity との一致を検証しています。Ch.2〜6 と一部の計器(角度ゲージ等)は開発中です。
>
> 関連文書: 設計仕様 [`docs/spec.md`](docs/spec.md) / 解説モードの設計・台本 [`docs/section-guide.md`](docs/section-guide.md) / 儀なしで通読できる配布資料 [`docs/handout-quaternion.md`](docs/handout-quaternion.md)。

## 動作環境

- Unity 6 (6000.x)
- Universal Render Pipeline (URP) 17.3
- Input System 1.19
- [Git LFS](https://git-lfs.com/) (`.blend` などのバイナリアセットに使用)

クローン後、モデル等のバイナリアセットを取得するには Git LFS が必要です。

```sh
git lfs install
git clone https://github.com/radiann-kswg/QuaternionViewer.git
```

## 構成

| パス | 内容 |
| --- | --- |
| `Assets/Scenes/QuaternionGlobe.unity` | 本編シーン(三層の儀 + 情報パネル + 解説モード) |
| `Assets/QuaternionViewer/Scripts/` | `Core/`(自前の四元数数学)/ `Visualization/`(三層の可視化)/ `UI/`(情報パネル・グラフ・解説バー)/ `Chapters/`(解説モード)/ `Input/`(アークボール) |
| `Assets/QuaternionViewer/Models/` | 内核モデルの `.fbx`(ソースは `Art/` の `.blend`。すべて自作) |
| `Assets/QuaternionViewer/Resources/Fonts/` | 同梱ピクセルフォント(**第三者製・MIT対象外**。後述) |
| `Assets/QuaternionViewer/Resources/Guide/` | 解説モードの章台本(画面用転記。正典は `docs/section-guide.md` §4) |
| `Assets/QuaternionViewer/Tests/EditMode/` | EditMode テスト(数学ライブラリ・入力・解説パーサ) |
| `Assets/Settings/` | レンダリング設定(URP) |
| `Art/` | DCCツールのソースファイル(`.blend` 等)。`Assets/` 外に置き、Unityの取り込み対象から外している |
| `docs/spec.md` | 数理設計・画面演出の仕様(設計の正典) |
| `docs/section-guide.md` | 解説・図解モードの設計＋全6章の台本 |
| `docs/handout-quaternion.md` | 儀なしで通読できる配布ハンドアウト |
| `AGENTS.md` | AIエージェント設定の単一情報源(SSOT) |
| `docs/agents/` | ロールプレイプロンプト |

モデルは `Art/` の `.blend` をソースとし、そこから書き出した `.fbx` を `Assets/` に配置します。`.blend` を `Assets/` 内に置くとUnityが二重に取り込むため、ソースは `Assets/` 外に置いてください。

## AIエージェント設定

本リポジトリのAIエージェント設定のSSOTは [`AGENTS.md`](AGENTS.md) です。
`CLAUDE.md`(Claude Code向け)と `.github/copilot-instructions.md`(GitHub Copilot向け)は、`AGENTS.md` を参照するだけの薄いポインタファイルです。**設定の追加・変更は `AGENTS.md` に対してのみ**行ってください。

Unityエディタの操作は、Unity公式MCPサーバ経由で行います(`.mcp.json` / `.vscode/mcp.json`)。詳細は `AGENTS.md` 2章を参照してください。

## ライセンス

> [!IMPORTANT]
> 本リポジトリは、本プロジェクト自身の成果物について **2つのライセンスが併存**(コード・Unityプロジェクト = MIT / キャラクター設定 = CC BY-NC 4.0)します。加えて、**同梱する第三者アセット(ピクセルフォント)は各自のライセンス**に従います(「第三者コンポーネントの表示」参照)。利用前に必ず適用範囲をご確認ください。

### コード・Unityプロジェクト → MIT

[`LICENSE`](LICENSE) (MIT) の対象は、本プロジェクトのソースコード・Unityプロジェクト構成・**自作の3Dモデル**です(`Assets/QuaternionViewer/`〔`Models/` の `.fbx` を含む〕、`Assets/Settings/`、`ProjectSettings/`、`Art/` の `.blend` ソース など)。内核モデル(Dice / NineBall / OctantSphere / Knight)はいずれも本プロジェクトが Blender で制作した自作物です。ただし後述の**同梱フォントとキャラクター設定は MIT の対象外**です。

### ナンバーテールズのキャラクター設定 → CC BY-NC 4.0(非営利限定)

`docs/agents/roleplay-44.md`、`docs/agents/roleplay-444.md`、および `AGENTS.md` 3〜4章のキャラクター設定は、**MITの対象外**です。「百花繚乱研究所」の一次創作作品として CC BY-NC 4.0 の下に提供されます。

> 100BeautiesLab.(百花繚乱研究所) Primary Works/Creations © 2021-2026 by RadianN_kswg(ラジアン/柏木主税) is licensed under CC BY-NC 4.0

適用範囲と条件の詳細は [`LICENSE-CHARACTERS.md`](LICENSE-CHARACTERS.md) を、利用条件の正文は[公式ガイドライン](https://github.com/radiann-kswg/100BeautiesLab_CreationsDB/blob/develop/guideline.md)を参照してください。

- 創作DB: <https://database.numbertales-radiann.net/>
- ナンバーテールズ公式サイト: <https://www.numbertales-radiann.com/>

## 第三者コンポーネントの表示

| 対象 | 出典 | ライセンス |
| --- | --- | --- |
| 同梱ピクセルフォント `Resources/Fonts/*.ttf`(マルモニカ / スキャンライン) | 患者長ひっく(hicc) — [x0y0pxFreeFont](https://hicchicc.github.io/00ff/) | 独自条項(商用可・改変可・クレジット不要・**単体販売禁止**)。**MIT対象外**。全文 → [`x0y0pxFreeFont-NOTICE.md`](Assets/QuaternionViewer/Resources/Fonts/x0y0pxFreeFont-NOTICE.md) |
| `.gitignore` | [github/gitignore](https://github.com/github/gitignore) — `Unity.gitignore` | CC0-1.0 |
| `.gitattributes` | [gitattributes/gitattributes](https://github.com/gitattributes/gitattributes) — `Unity.gitattributes` | MIT |
| Unityテンプレート由来のアセット・Unityパッケージ | Unity Technologies (URPテンプレート / Unity Registry) | Unity Companion License 等、各パッケージの条件に従う |

© ラジアン(柏木主税) / ©RadianN_kswg
