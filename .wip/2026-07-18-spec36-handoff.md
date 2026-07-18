# 引継ぎ 2026-07-18 — 仕様書 §3.6 内部数学ライブラリ (A〜I) 確定

- **From**: Claude Code(444 シテン / 数理・画面演出ロール)
- **To**: Claude Desktop(44 シトシ / 444 シテン)
- **主題**: §3.6 A〜I の定義・検証の確定状態の申し送り。経緯編は [`2026-07-18-desktop-session.md`](./2026-07-18-desktop-session.md) 末尾「突き合わせ結果」を参照。
- **正典**: 定義そのものは [`docs/spec.md`](../docs/spec.md) §3.6 が唯一の正。本書はその要約と、実装・検証の対応関係の索引である。

---

## 0. 要旨(先に結論)

- **§3.6 A〜I は spec.md 上で確定**(コミット `7226e44`)。Desktop 版の §3.6 が §3.6 の**初出**であり、突き合わせるべき旧草稿は存在しなかった(消えたのではなく、前回セッションで書かれる前に Unity MCP 接続の格闘に呑まれた。詳細は経緯編)。
- 承認済みスコープ(A〜I 一括 / 正準形=生符号+表示層選択)を**完全充足**。矛盾・取りこぼし無し。
- 唯一の未反映点だった「**§6.3 に検証項目を追加**」を本セッションで正典へ反映済み(`QuatMathExtendedTests.cs` 25本と一致)。
- 実装(`Mat3.cs` / `QuatMath` 拡張)とテストも同時にコミット済み(`093ef8d`)。**EditMode 57本(既存32+新規25)全緑**。

---

## 1. §3.6 A〜I 定義の確定状態

すべて2章の規約(Unity 左手系 Y-up、左ねじ正、格納順 (x,y,z,w)、単位四元数)の下で成立。記号は spec.md に準拠。

| 項 | 名称 | 定義の要点 | 特異点・退化の扱い |
| --- | --- | --- | --- |
| **A** | ToMatrix / `Mat3` | 単位 $q=(w,\mathbf v)$、列ベクトル規約 $\mathbf v'=R\mathbf v$ の回転行列 $R(q)$。`Mat3` は行優先 `readonly struct`(積/転置/det/trace のみ) | $R\in SO(3)$($R^{\mathsf T}R=I,\ \det R=+1$)を保証 |
| **B** | FromToRotation | $q=\mathrm{normalize}(1+\mathbf a\!\cdot\!\mathbf b,\ \mathbf a\times\mathbf b)$。3.5 アークボールと同形 | $\mathbf a\approx-\mathbf b$ で $1+\mathbf a\!\cdot\!\mathbf b\to0$。軸直交の $\mathbf m$ を選び $q=(0,\mathbf m)$(180°)へ退避。**軸の選択が本質的に任意**な縮退 |
| **C** | Angle | 測地距離 $\theta=2\arccos(\lvert\langle q_0,q_1\rangle\rvert)\in[0,\pi]$ | 内積**絶対値**が二重被覆の折り畳み。$\theta(q,-q)=0$ |
| **D** | RotationVector | $\mathbf r=\theta\mathbf n,\ \theta=2\,\mathrm{atan2}(\lvert\mathbf v\rvert,w)\in[0,2\pi]$。逆は $\exp(\tilde{\mathbf r}/2)$ | $\theta>\pi$ の正準化は **Core では行わず**表示層が I で選ぶ。外殻回転ベクトル模型 $\theta\mathbf n/\pi$(4.3)はこの $\mathbf r$ を $\pi$ で割ったもの |
| **E** | Reflect / ReflectionPair | 鏡映 $\mathbf v'=\mathbf v-2(\mathbf v\!\cdot\!\mathbf m)\mathbf m$。$\theta/2$ で交わる二枚鏡 $\mathbf m_1\!\to\!\mathbf m_2$ の合成 $=$ 軸 $\mathbf m_1\times\mathbf m_2$・角 $\theta$ の回転(**半角の実体**) | ゼロ基準 $\hat{\mathbf g}_0$(4.2)が軸平行のとき $\mathbf m_1$ を軸直交へ退避 |
| **F** | EulerRateJacobian | ZXY 規約。$\boldsymbol\omega=E(\dot p,\dot y,\dot r)^{\mathsf T}$、$E=(R_y\hat{\mathbf x}\mid\hat{\mathbf y}\mid R_yR_x\hat{\mathbf z})$。$\det E=\cos(\text{pitch})$ | $\text{pitch}=\pm90°$ で**第2列(yaw)と第3列(roll)が平行** → $\mathrm{rank}\,E=2$。**当初「第1列」と誤記していたのをテストが検出・訂正済**(Desktop 側での修正、正典に反映済) |
| **G** | GimbalStages | $q_{\text{outer}}=q_y,\ q_{\text{middle}}=q_y\!\otimes\!q_x,\ q_{\text{inner}}=q_y\!\otimes\!q_x\!\otimes\!q_z$。$q_{\text{inner}}=\mathrm{FromEuler}(p,y,r)$ | 外環=yaw / 中環=pitch / 内環=roll。ロック点で外環と内環の軸が同一平面へ縮退($\det E\to0$ の幾何的実体) |
| **H** | EulerInterp / AngularSpeed | 成分ごと最短差分線形補間 $\mathbf e(t)=\mathbf e_0+t\,\mathrm{wrap}(\mathbf e_1-\mathbf e_0)$、$\mathrm{wrap}\to(-\pi,\pi]$。角速度 $\lvert\boldsymbol\omega\rvert\approx\frac{2\lvert\log(q(t)^{*}\!\otimes q(t{+}\Delta t))\rvert}{\Delta t}$ | 角速度は **Slerp でのみ一定**。$\langle q(t),q(t{+}\Delta t)\rangle<0$ で折り返し |
| **I** | Canonical | $w>0$:そのまま / $w<0$:$-q$ / $w=0$:v の先頭非零成分が正の側 | **Core は畳まない(決定)**。Ch.2 は生の $-q$、Ch.4 のオイラー角表示等は Canonical を通してよい |

### 正準形ポリシー(承認済み・再掲)

> Core は演算結果を勝手に畳まない。畳むか否かは**表示層が章ごとに選ぶ**。二重被覆(Ch.2)を見せるためには生の $-q$ が要る一方、通常表示の安定には Canonical が要る——この両立不能を「理論に手を入れず表示層の分岐へ隔離する」ことで解いた。UI(`QuaternionReadout`)は既に**生の値を表示・正準化しない**実装(3.6-I 準拠)。

---

## 2. 検証の担保(§6.3 へ反映済み)

`Assets/QuaternionViewer/Tests/EditMode/QuatMathExtendedTests.cs`(25本)が A〜I を検証。定義の主張を **Unity との突き合わせ** と **数値健全性(NaN を出さない)** で担保する方針は `QuatMathTests.cs`(2章の規約検証, 32本)と同格。

- **A**: `ToMatrix` == `Matrix4x4.Rotate` / 作用 == サンドイッチ積 / $R^{\mathsf T}R=I$・$\det R=+1$ / `Mat3` の結合律・$(AB)^{\mathsf T}=B^{\mathsf T}A^{\mathsf T}$
- **B**: a→b 一致・単位・`Quaternion.FromToRotation` 一致 / 対蹠で $w=0$ の180°退避
- **C**: `Quaternion.Angle` 一致 / $\theta(q,-q)=\theta(q,q)=0$
- **D**: 往復で回転保存 / $\lvert\mathbf r\rvert=\theta$
- **E**: 鏡映=ノルム保存の対合 / 二枚鏡合成 == `FromAxisAngle` / 退化ゼロ基準で軸直交退避
- **F**: $\det E=\cos p$ / 数値微分 $\boldsymbol\omega_{\text{num}}$ 一致 / ロック点で第2・第3列平行
- **G**: inner == `FromEuler`(outer=yaw, middle=yaw·pitch)
- **H**: 端点一致・yaw 350°→10° の最短経路 / `WrapAngle`∈$(-\pi,\pi]$ / 角速度 Slerp 一定・Nlerp 変動
- **I**: $w\ge0$ 代表元一意($q,-q$ 同一)/ $w=0$ の先頭非零成分正

正典 §6.3 に上記に対応する「検証項目 (3.6)」表を新設、§6.1 のテストツリーに `QuatMathExtendedTests.cs` を追記済み。

---

## 3. コミット参照(全て master、未 push)

| ハッシュ | コミット | 関連 |
| --- | --- | --- |
| `7226e44` | docs: 仕様書 3.6 + 6.3検証 | §3.6 定義 / §6.3・§6.1 検証反映 |
| `093ef8d` | feat: Core 3.6 A〜I 実装 + テスト25件 | `Mat3.cs` / `QuatMath` 拡張 / `QuatMathExtendedTests.cs` |
| `c500e94` | docs: 引継ぎログ | 経緯編(突き合わせ結果) |

(シーン `72df03b` / UI `7541b3b` は §3.6 の直接対象外だが同バッチ)

---

## 4. §3.6 に連なる次の入り口(Desktop への申し送り)

**定義と Core 実装・テストは済んだ。残るは "演出への接続" だ。**

1. **E 半角演示ミラー**: `ReflectionPair` は実装・テスト済だが、中殻の**二枚鏡の可視化(Ch.1)は未着手**。$\theta/2$ 交差 → 像が $\theta$ 回る絵を出す。
2. **F/G ジンバル演出**: `EulerRateJacobian` / `GimbalStages` は済。**3重リング(Ch.4)の GameObject 化と、$\det E\to0$ で外環・内環を赤くハイライトする演出**が未着手。`GimbalRig.cs`(§6.1)は未作成。
3. **H 三体比較(Ch.5)**: Slerp / Nlerp / オイラー角補間の**同時走行**と角速度グラフ(`GraphPlotter`, Painter2D)。`AngularSpeed` が計器の実体。
4. **D 外殻の模型切替**: ベクトル部模型 ⇄ 回転ベクトル模型のトグル(4.3)。`RotationVector` が後者の座標。

> いずれも Core 側の数理はテストで固めてある。**演出側は「表示層が Canonical をどう選ぶか」だけ 3.6-I の方針に従えば、理論と食い違うことはない。** 迷ったら正典 §3.6 と §6.3 の表へ戻れ。

---

© ラジアン(柏木主税) / ©RadianN_kswg
