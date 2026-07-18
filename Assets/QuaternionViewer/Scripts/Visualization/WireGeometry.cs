using UnityEngine;
using UnityEngine.Rendering;

namespace QuaternionViewer.Visualization
{
    /// <summary>
    /// 緯線・経線・円弧の LineRenderer 生成ヘルパ (仕様書 6.1)。中殻・外殻で共用する。
    /// </summary>
    /// <remarks>
    /// 生成物はすべて HideFlags.DontSave とし、シーンファイルを汚さない。
    /// 再構築は各コンポーネントが OnEnable で行う。
    /// </remarks>
    public static class WireGeometry
    {
        /// <summary>URP Unlit の単色マテリアルを生成する。シーンにもアセットにも保存しない。</summary>
        public static Material CreateUnlitMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var m = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            else m.color = color;
            return m;
        }

        /// <summary>親の下に非保存の空 GameObject を作る。生成物の置き場に使う。</summary>
        public static Transform CreateContainer(Transform parent, string name)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>ローカル空間で描く LineRenderer を作る。</summary>
        public static LineRenderer CreateLine(
            Transform parent, string name, Material material, float width, bool loop)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = loop;
            lr.widthMultiplier = width;
            lr.sharedMaterial = material;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.positionCount = 0;
            return lr;
        }

        /// <summary>コライダー無しの小球マーカーを作る。</summary>
        public static Transform CreateMarker(
            Transform parent, string name, Material material, float diameter)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.hideFlags = HideFlags.DontSave;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * diameter;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }

        /// <summary>中心 center、直交基底 (axisA, axisB) の張る平面上の円周点列を返す。</summary>
        public static Vector3[] Circle(
            Vector3 center, Vector3 axisA, Vector3 axisB, float radius, int segments)
        {
            var pts = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float t = i * (2f * Mathf.PI / segments);
                pts[i] = center + (axisA * Mathf.Cos(t) + axisB * Mathf.Sin(t)) * radius;
            }

            return pts;
        }

        /// <summary>LineRenderer へ点列を流し込む。</summary>
        public static void SetPositions(LineRenderer lr, Vector3[] points)
        {
            lr.positionCount = points.Length;
            lr.SetPositions(points);
        }

        /// <summary>
        /// S² の緯線・経線ワイヤ球を親の下に生成する (仕様書 4.2)。
        /// 緯線は ±(latStep × n)、経線は大円。ローカル空間・単位はローカル半径。
        /// </summary>
        public static void BuildWireSphere(
            Transform parent, Material material, float radius, float width,
            int latitudeCount, int longitudeCount, int segments)
        {
            // 緯線 (赤道を含む奇数本)
            for (int i = -latitudeCount; i <= latitudeCount; i++)
            {
                float lat = i * (Mathf.PI * 0.5f / (latitudeCount + 1));
                float r = Mathf.Cos(lat) * radius;
                float h = Mathf.Sin(lat) * radius;
                var lr = CreateLine(parent, $"Lat{i}", material, width, true);
                SetPositions(lr, Circle(new Vector3(0f, h, 0f), Vector3.right, Vector3.forward, r, segments));
            }

            // 経線 (両極を通る大円。180°で対になるため本数は半周分)
            for (int i = 0; i < longitudeCount; i++)
            {
                float lon = i * (Mathf.PI / longitudeCount);
                var radial = new Vector3(Mathf.Cos(lon), 0f, Mathf.Sin(lon));
                var lr = CreateLine(parent, $"Lon{i}", material, width, true);
                SetPositions(lr, Circle(Vector3.zero, radial, Vector3.up, radius, segments));
            }
        }
    }
}
