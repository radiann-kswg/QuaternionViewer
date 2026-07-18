using QuaternionViewer.Chapters;
using QuaternionViewer.Core;
using QuaternionViewer.Visualization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace QuaternionViewer.Input
{
    /// <summary>
    /// 自由探索の入力層 (仕様書 3.5, 7章) ―― Play モード専用。
    /// 左ドラッグ: アークボールで儀の回転 / 右ドラッグ: カメラ周回 / ホイール: ズーム / R キー: 視点と姿勢のリセット。
    /// </summary>
    /// <remarks>
    /// アークボールは「掴んだ点がカーソルへ追従する」θ 版 q = normalize(1 + p0・p1, p0×p1) (仕様書 3.5)。
    /// 同形の <see cref="QuatMath.FromToRotation"/> (3.6-B) に委譲する。
    /// ドラッグ中は <see cref="RotationSource.driveFromInspector"/> を切って Pose を直接書き、
    /// ドラッグ終了時に軸角へ読み戻してインスペクタ駆動へ返す (配布点の規約を崩さない)。
    /// UI (UIDocument パネル) 上で始まった操作は儀へ流さない。
    /// リセットは章 (<see cref="ChapterBase"/>) が結線されていれば現在ビートを再適用する ――
    /// 解説モードの「自由探索から同じビートへ復帰」(section-guide §1.3) の入口。
    /// </remarks>
    public class ArcballController : MonoBehaviour
    {
        public RotationSource source;

        [Tooltip("アークボール球の中心 (未指定なら原点 = 中殻の位置)")]
        public Transform pivot;

        [Tooltip("アークボール球の半径 (中殻 Globe の半径に合わせる)")]
        public float arcballRadius = 1.5f;

        [Tooltip("未指定なら Camera.main")]
        public Camera cam;

        [Tooltip("R キーで現在ビートを再適用する章 (任意)")]
        public ChapterBase chapter;

        [Header("カメラ周回 / ズーム")]
        public float orbitDegPerPixel = 0.25f;
        public float pitchLimitDeg = 80f;
        public float zoomStep = 0.5f;
        public float minDistance = 2.5f;
        public float maxDistance = 12f;

        private bool _dragging;
        private Vector3 _p0;
        private Quat _poseStart = Quat.Identity;

        private bool _orbiting;
        private Vector2 _lastMouse;
        private float _yaw;
        private float _pitch;
        private float _distance;
        private float _homeDistance;
        private Vector3 _homeDir = Vector3.back;
        private Vector3 _homePos;
        private Quaternion _homeRot;

        private Vector3 PivotPoint => pivot != null ? pivot.position : Vector3.zero;

        private void Start()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                _homePos = cam.transform.position;
                _homeRot = cam.transform.rotation;
                Vector3 offset = _homePos - PivotPoint;
                _homeDistance = _distance = Mathf.Max(offset.magnitude, 0.01f);
                _homeDir = offset.normalized;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || source == null || cam == null) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) ResetView();

            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mp = mouse.position.ReadValue();

            // ── ホイール: ズーム ─────────────────────────────────────
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && !IsPointerOverUI(mp))
            {
                _distance = Mathf.Clamp(_distance - Mathf.Sign(scroll) * zoomStep, minDistance, maxDistance);
                ApplyCamera();
            }

            // ── 左ドラッグ: アークボール (儀の回転) ──────────────────
            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI(mp)) BeginArcball(mp);
            if (_dragging && mouse.leftButton.isPressed) UpdateArcball(mp);
            if (_dragging && !mouse.leftButton.isPressed) EndArcball();

            // ── 右ドラッグ: カメラ周回 ──────────────────────────────
            if (mouse.rightButton.wasPressedThisFrame && !IsPointerOverUI(mp))
            {
                _orbiting = true;
                _lastMouse = mp;
            }

            if (_orbiting && mouse.rightButton.isPressed)
            {
                Vector2 d = mp - _lastMouse;
                _lastMouse = mp;
                if (d.sqrMagnitude > 0f)
                {
                    _yaw += d.x * orbitDegPerPixel;
                    _pitch = Mathf.Clamp(_pitch + d.y * orbitDegPerPixel, -pitchLimitDeg, pitchLimitDeg);
                    ApplyCamera();
                }
            }

            if (_orbiting && !mouse.rightButton.isPressed) _orbiting = false;
        }

        private void BeginArcball(Vector2 mousePos)
        {
            Ray ray = cam.ScreenPointToRay(mousePos);
            _p0 = MapToSphere(ray.origin, ray.direction.normalized, PivotPoint, arcballRadius);
            _poseStart = source.Pose;
            source.driveFromInspector = false;
            source.spin = false;
            _dragging = true;
        }

        private void UpdateArcball(Vector2 mousePos)
        {
            Ray ray = cam.ScreenPointToRay(mousePos);
            Vector3 p1 = MapToSphere(ray.origin, ray.direction.normalized, PivotPoint, arcballRadius);
            if ((p1 - _p0).sqrMagnitude < 1e-12f)
            {
                source.Pose = _poseStart;
                return;
            }

            // p0 を掴んで p1 まで運ぶ最短回転を、ドラッグ開始時の姿勢へ世界系で追加適用する
            source.Pose = QuatMath.FromToRotation(_p0, p1) * _poseStart;
        }

        private void EndArcball()
        {
            _dragging = false;
            QuatMath.ToAxisAngle(source.Pose, out Vector3 axis, out float radians);
            if (axis.sqrMagnitude > 1e-8f)
            {
                source.axis = axis;
                source.angleDeg = radians * Mathf.Rad2Deg;
            }
            else
            {
                source.axis = Vector3.up;
                source.angleDeg = 0f;
            }

            source.driveFromInspector = true;
        }

        /// <summary>視点を初期フレーミングへ戻し、章があれば現在ビートを再適用する (R キー)。</summary>
        public void ResetView()
        {
            _yaw = 0f;
            _pitch = 0f;
            _distance = _homeDistance;
            _dragging = false;
            _orbiting = false;
            if (cam != null) cam.transform.SetPositionAndRotation(_homePos, _homeRot);

            if (chapter != null)
            {
                chapter.Reapply();
            }
            else if (source != null)
            {
                source.axis = Vector3.up;
                source.angleDeg = 0f;
                source.driveFromInspector = true;
            }
        }

        private void ApplyCamera()
        {
            Vector3 c = PivotPoint;
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pos = c + rot * (_homeDir * _distance);
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(c - pos, Vector3.up));
        }

        /// <summary>
        /// カーソルのレイを半径 <paramref name="radius"/> の球面へ写す (中心からの単位ベクトルを返す)。
        /// レイが球と交わるときは手前の交点、外れたときは最近接点をシルエットへ射影する。
        /// </summary>
        public static Vector3 MapToSphere(Vector3 rayOrigin, Vector3 rayDir, Vector3 center, float radius)
        {
            float t = Vector3.Dot(center - rayOrigin, rayDir);
            Vector3 closest = rayOrigin + rayDir * t;
            Vector3 d = closest - center;
            float d2 = d.sqrMagnitude;
            float r2 = radius * radius;
            if (d2 <= r2)
            {
                float dt = Mathf.Sqrt(r2 - d2);
                Vector3 hit = rayOrigin + rayDir * (t - dt);
                return (hit - center) / radius;
            }

            return d.sqrMagnitude > 1e-12f ? d.normalized : Vector3.back;
        }

        /// <summary>UIDocument のいずれかのパネル要素上にカーソルがあるか (ルート要素そのものは除く)。</summary>
        private static bool IsPointerOverUI(Vector2 mousePos)
        {
            foreach (UIDocument doc in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                VisualElement root = doc.rootVisualElement;
                IPanel panel = root != null ? root.panel : null;
                if (panel == null) continue;
                Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(
                    panel, new Vector2(mousePos.x, Screen.height - mousePos.y));
                VisualElement picked = panel.Pick(panelPos);
                if (picked != null && picked != root) return true;
            }

            return false;
        }
    }
}
