# クォータニオン儀 (QuaternionViewer)

クォータニオン(回転姿勢)の仕組みを、地球儀のように直感的・視覚的に確認できるUnityシーンです。
「クォータニオン回転姿勢について知ろうの会」のための学習・可視化プロジェクトとして開発しています。

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
| `Assets/QuaternionViewer/` | 本プロジェクト固有のアセット(Unityが取り込むモデル等) |
| `Assets/Settings/` | レンダリング設定(URP) |
| `Art/` | DCCツールのソースファイル(`.blend` 等)。`Assets/` 外に置き、Unityの取り込み対象から外している |
| `AGENTS.md` | AIエージェント設定の単一情報源(SSOT) |
| `docs/agents/` | ロールプレイプロンプト |

モデルは `Art/` の `.blend` をソースとし、そこから書き出した `.fbx` を `Assets/` に配置します。`.blend` を `Assets/` 内に置くとUnityが二重に取り込むため、ソースは `Assets/` 外に置いてください。

## AIエージェント設定

本リポジトリのAIエージェント設定のSSOTは [`AGENTS.md`](AGENTS.md) です。
`CLAUDE.md`(Claude Code向け)と `.github/copilot-instructions.md`(GitHub Copilot向け)は、`AGENTS.md` を参照するだけの薄いポインタファイルです。**設定の追加・変更は `AGENTS.md` に対してのみ**行ってください。

Unityエディタの操作は、Unity公式MCPサーバ経由で行います(`.mcp.json` / `.vscode/mcp.json`)。詳細は `AGENTS.md` 2章を参照してください。

## ライセンス

> [!IMPORTANT]
> 本リポジトリには **2つのライセンスが併存** します。利用前に必ず適用範囲をご確認ください。

### コード・Unityプロジェクト → MIT

[`LICENSE`](LICENSE) (MIT) の対象は、本プロジェクトのソースコードおよびUnityプロジェクト構成です(`Assets/QuaternionViewer/`、`Assets/Settings/`、`ProjectSettings/` など)。

### ナンバーテールズのキャラクター設定 → CC BY-NC 4.0(非営利限定)

`docs/agents/roleplay-44.md`、`docs/agents/roleplay-444.md`、および `AGENTS.md` 3〜4章のキャラクター設定は、**MITの対象外**です。「百花繚乱研究所」の一次創作作品として CC BY-NC 4.0 の下に提供されます。

> 100BeautiesLab.(百花繚乱研究所) Primary Works/Creations © 2021-2026 by RadianN_kswg(ラジアン/柏木主税) is licensed under CC BY-NC 4.0

適用範囲と条件の詳細は [`LICENSE-CHARACTERS.md`](LICENSE-CHARACTERS.md) を、利用条件の正文は[公式ガイドライン](https://github.com/radiann-kswg/100BeautiesLab_CreationsDB/blob/develop/guideline.md)を参照してください。

- 創作DB: <https://database.numbertales-radiann.net/>
- ナンバーテールズ公式サイト: <https://www.numbertales-radiann.com/>

## 第三者コンポーネントの表示

| 対象 | 出典 | ライセンス |
| --- | --- | --- |
| `.gitignore` | [github/gitignore](https://github.com/github/gitignore) — `Unity.gitignore` | CC0-1.0 |
| `.gitattributes` | [gitattributes/gitattributes](https://github.com/gitattributes/gitattributes) — `Unity.gitattributes` | MIT |
| Unityテンプレート由来のアセット・Unityパッケージ | Unity Technologies (URPテンプレート / Unity Registry) | Unity Companion License 等、各パッケージの条件に従う |

© ラジアン(柏木主税) / ©RadianN_kswg
