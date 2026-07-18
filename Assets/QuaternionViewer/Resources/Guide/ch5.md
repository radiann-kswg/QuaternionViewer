# Ch.5 補間

> 台本の正典は docs/section-guide.md §4.5。本ファイルはその画面用転記 (SSOT は §4)。
> 儀の既定: demos=Interp|Graph (三体走行+角速度グラフ)。外殻=ベクトル部模型。

## ◆ 三体同時走行
@demos Interp|Graph
@ball VectorPart
@camera SpaceBall
@highlight None
@focus ball
@action interpCorrectionOn
@action interpDefaultEnds
@action graphSpeed

### 直感
姿勢Aから姿勢Bへ、三つのやり方で同時に補間する。Slerp (teal)、Nlerp (orange)、オイラー角補間 (magenta)。外殻ボールに三本の軌跡が描かれ、三体のマーカーが並走する。

### 数理
Slerp = sin((1-t)Ω)/sinΩ・q0 + sin(tΩ)/sinΩ・q1 (cosΩ = <q0,q1>, spec §3.2)。Nlerp は線形補間の正規化。Euler は成分ごと最短差分の線形補間を FromEuler に通す (EulerInterp, spec §3.6-H)。三体は InterpRace.Evaluate が同一の q0, q1 から生成。

### 話者ノート
軌跡の形の違い (Slerp は大円=直線的、他は歪む) をまず目で。数値は次のグラフで。

## ◆ 角速度グラフ: Slerp だけが一定
@demos Interp|Graph
@camera SpaceBall
@highlight None

### 直感
右下の角速度グラフ |ω(t)| を見よ。Slerp だけが真っ平ら (一定)。Nlerp と Euler は途中で速くなったり遅くなったりする。Slerp は「最短の道を等速で」進む唯一の補間だ。

### 数理
|ω| ≈ 2|log(q(t)* ⊗ q(t+Δt))| / Δt (AngularSpeed, spec §3.6-H) を InterpRace.SampleSpeeds が計測。Slerp は SO(3) の測地線 (定速大円) ゆえ |ω| = Ω 一定。Nlerp は弦を等速で刻むため中央で角速度が落ち、Euler は座標の歪みを負う。

### 話者ノート
「一定角速度」は次章 (Ch.6) の「Slerp=指数写像」への直接の伏線。

## ○ 最短経路補正を切る: 二重被覆の実害
@demos Interp|Graph
@camera SpaceBall
@highlight None
@focus ballPose ballAntipode
@action interpCorrectionOff

### 直感
ここで最短経路補正を切る。すると Slerp が突然遠回りを始める ―― 180°を超えて、ぐるりと長い方を回る。Ch.2 で見た q と -q の取り違えが、ここで実害として噴出する。

### 数理
補正は <q0,q1> < 0 のとき q1 ← -q1 とし、Ω ≤ π/2 側 (短弧) を選ぶ (spec §3.2)。切ると同じ姿勢を指す -q1 を選び損ね、Ω > π/2 の長弧を通る ―― RP^3 で近い二点が、S^3 では遠回りに繋がれる。二重被覆を無視した代償。

### 話者ノート
Ch.2 の「対蹠の貼り合わせを補間器が知らないと遠回り」の回収。ここが Ch.2 ↔ Ch.5 を結ぶ山場。

## ○ 見かけの特異点: Ω→0 の 0/0
@demos Interp|Graph
@camera SpaceBall
@highlight None
@action interpCorrectionOn
@action interpCloseEnds

### 直感
補正を戻し、姿勢Aと姿勢Bをほぼ一致させる。Slerp の式は sinΩ/sinΩ が 0/0 になりそうだが ―― 破綻しない。これは「割り切れる」見かけの特異点だ。Ch.4 の本物の特異点とは違う。

### 数理
Ω→0 で sin(tΩ)/sinΩ → t (テイラー sin x / x → 1 - x^2/6、spec §3.1)。除去可能特異点であり、極限は線形補間へ連続に繋がる。対して Ch.4 のジンバルロックは座標写像の階数が落ちる真の縮退。両者を混同してはならぬ ―― 「0/0 の顔をしていても、退避できる偽の特異点」と「退避できない真の特異点」の別こそ、本章の裏テーマだ。

### 話者ノート
本章の要。特異点の分類 (除去可能 vs 本質的/座標) を一言添えると、Ch.4 と Ch.5 が一本の糸で結ばれる。
(このビートを離れるときは、次の章頭ビートが interpDefaultEnds で両端を戻す)
