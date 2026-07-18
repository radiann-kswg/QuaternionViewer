using QuaternionViewer.Core;
using UnityEngine;

namespace QuaternionViewer.Visualization
{
    /// <summary>
    /// 二体サイコロ並置 (仕様書 5.3) ―― 同じ二回転を順序だけ変えて適用し、出目の食い違いで非可換性を見せる。
    /// 左=「A が先 → B が後」、右=「B が先 → A が後」。適用は逐次アニメ (前半で1つ目、後半で2つ目)。
    /// </summary>
    /// <remarks>
    /// サイコロは diceTemplate (シーンの Core/Dice) をクローンし、生成物は非保存。
    /// アクティブ中は hideWhileActive (Core) を退避して単体サイコロと重ねない。
    /// 走行は t∈[0,2] を一度だけ進み、端で保持する (出目の食い違いを静止で読ませる)。再走行は Restart()。
    /// </remarks>
    [ExecuteAlways]
    public class TwinDiceRig : MonoBehaviour
    {
        [Tooltip("クローン元のサイコロ (シーンの Core/Dice)")]
        public GameObject diceTemplate;

        [Tooltip("アクティブ中に退避させる GO (Core)")]
        public GameObject hideWhileActive;

        [Header("二つの回転 (同角・順序違い)")]
        public Vector3 axisA = Vector3.right;
        public Vector3 axisB = Vector3.up;
        public float angleDeg = 90f;

        [Header("配置 / 走行")]
        public float separation = 1.7f;
        public float diceScale = 0.72f;
        public bool run = true;
        public float duration = 4f;

        [Range(0f, 2f)]
        [Tooltip("適用の進み (0〜1: 1つ目 / 1〜2: 2つ目)")]
        public float t = 2f;

        private Transform _left;
        private Transform _right;
        private bool _hiddenWasActive;

        /// <summary>
        /// 「first を先、second を後」に適用した途中経過 (t∈[0,2])。
        /// t≤1 で first を 0→θ、t>1 で first 全量の上へ second を 0→θ。
        /// 完了時は q_total = q_second ⊗ q_first (仕様書 2.4 の合成順)。
        /// </summary>
        public static Quat PoseFor(Vector3 first, Vector3 second, float angleRad, float t)
        {
            if (t <= 1f) return QuatMath.FromAxisAngle(first, angleRad * Mathf.Clamp01(t));
            return QuatMath.FromAxisAngle(second, angleRad * Mathf.Clamp01(t - 1f))
                   * QuatMath.FromAxisAngle(first, angleRad);
        }

        /// <summary>適用アニメを頭から再走行する (@action twinRestart)。</summary>
        public void Restart() => t = 0f;

        private void OnEnable()
        {
            if (diceTemplate != null && _left == null)
            {
                _left = Spawn("DiceFirstAB", -1f);
                _right = Spawn("DiceFirstBA", +1f);
            }

            if (hideWhileActive != null)
            {
                _hiddenWasActive = hideWhileActive.activeSelf;
                hideWhileActive.SetActive(false);
            }

            t = 0f;
        }

        private void OnDisable()
        {
            Despawn(ref _left);
            Despawn(ref _right);
            if (hideWhileActive != null && _hiddenWasActive) hideWhileActive.SetActive(true);
        }

        private Transform Spawn(string childName, float side)
        {
            GameObject go = Instantiate(diceTemplate, transform);
            go.name = childName;
            go.hideFlags = HideFlags.DontSave;
            go.SetActive(true);
            go.transform.localPosition = new Vector3(side * separation * 0.5f, 0f, 0f);
            go.transform.localScale = Vector3.one * diceScale;
            return go.transform;
        }

        private static void Despawn(ref Transform target)
        {
            if (target != null)
            {
                if (Application.isPlaying) Destroy(target.gameObject);
                else DestroyImmediate(target.gameObject);
            }

            target = null;
        }

        private void Update()
        {
            if (run && Application.isPlaying && t < 2f)
            {
                t = Mathf.Min(2f, t + Time.deltaTime * 2f / Mathf.Max(0.5f, duration));
            }

            if (_left == null || _right == null) return;
            float rad = angleDeg * Mathf.Deg2Rad;
            _left.localRotation = PoseFor(axisA, axisB, rad, t).ToUnity();
            _right.localRotation = PoseFor(axisB, axisA, rad, t).ToUnity();
        }
    }
}
