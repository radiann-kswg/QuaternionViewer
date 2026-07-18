using System.Collections.Generic;
using QuaternionViewer.Input;
using UnityEngine;

namespace QuaternionViewer.Chapters
{
    /// <summary>
    /// @camera のフレーミング適用器 (section-guide §2) ―― プリセット視点へ滑らかにカメラを寄せる。
    /// 遷移完了時に <see cref="ArcballController"/> の周回状態を再同期し、直後の右ドラッグが跳ねないようにする。
    /// </summary>
    /// <remarks>
    /// プリセットの座標は 960×540 の既定レイアウト前提の初期値で、インスペクタから調整できる。
    /// エディタ (非 Play) では即座に適用する。
    /// </remarks>
    [ExecuteAlways]
    public class CameraFramer : MonoBehaviour
    {
        [System.Serializable]
        public class Preset
        {
            public CameraFraming framing;
            public Vector3 position;
            public Vector3 lookAt;
        }

        public Camera cam;
        public ArcballController arcball;

        [Tooltip("遷移時間 (秒)")]
        public float duration = 0.7f;

        [Tooltip("フレーミングのプリセット (空なら既定表で埋める)")]
        public List<Preset> presets = new List<Preset>();

        private bool _moving;
        private float _t;
        private Vector3 _fromPos;
        private Quaternion _fromRot;
        private Vector3 _toPos;
        private Quaternion _toRot;

        private void OnEnable()
        {
            if (presets.Count == 0)
            {
                presets.Add(new Preset { framing = CameraFraming.Overview, position = new Vector3(0f, 0f, -5.75f), lookAt = Vector3.zero });
                presets.Add(new Preset { framing = CameraFraming.CoreAndGlobe, position = new Vector3(0f, 0.2f, -4.1f), lookAt = Vector3.zero });
                presets.Add(new Preset { framing = CameraFraming.SpaceBall, position = new Vector3(2.5f, 0.3f, -3.5f), lookAt = new Vector3(3.3f, 0f, 0f) });
                presets.Add(new Preset { framing = CameraFraming.Gimbal, position = new Vector3(0f, 0.9f, -4.7f), lookAt = Vector3.zero });
            }
        }

        /// <summary>指定フレーミングへ寄せる (GuideController が beat.setCamera で呼ぶ)。</summary>
        public void Frame(CameraFraming framing)
        {
            if (cam == null) return;
            Preset p = presets.Find(x => x.framing == framing);
            if (p == null) return;

            _toPos = p.position;
            _toRot = Quaternion.LookRotation(p.lookAt - p.position, Vector3.up);

            if (!Application.isPlaying)
            {
                cam.transform.SetPositionAndRotation(_toPos, _toRot);
                if (arcball != null) arcball.SyncFromCamera();
                return;
            }

            _fromPos = cam.transform.position;
            _fromRot = cam.transform.rotation;
            _t = 0f;
            _moving = true;
        }

        private void Update()
        {
            if (!_moving || cam == null) return;
            _t += Time.deltaTime / Mathf.Max(0.05f, duration);
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_t));
            cam.transform.SetPositionAndRotation(
                Vector3.Lerp(_fromPos, _toPos, s),
                Quaternion.Slerp(_fromRot, _toRot, s));
            if (_t >= 1f)
            {
                _moving = false;
                if (arcball != null) arcball.SyncFromCamera();
            }
        }
    }
}
