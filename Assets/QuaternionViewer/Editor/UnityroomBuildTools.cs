// -----------------------------------------------------------------------------
// UnityroomBuildTools.cs
// unityroom 公開向け WebGL ビルド設定の適用・ビルドを行うエディタメニュー。
//
// unityroom の要件 (2026-07 時点):
//   - Compression Format : Gzip
//   - Decompression Fallback : 無効 (unityroom 側サーバが Content-Encoding を設定)
//   - 参考: https://help.unityroom.com/unityroom-351dc3ed5de980eebd79eef3b153be31
//
// 使い方:
//   Tools > unityroom > Apply WebGL Settings … 設定のみ適用 (プラットフォーム切替なし)
//   Tools > unityroom > Build WebGL          … 設定適用 + WebGL ビルド (Builds/WebGL-unityroom)
//   Tools > unityroom > Log Current Settings … 現在値の確認ログ
// -----------------------------------------------------------------------------

using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace QuaternionViewer.Editor
{
    /// <summary>unityroom 向け WebGL ビルドのセットアップツール。</summary>
    public static class UnityroomBuildTools
    {
        private const string MenuRoot = "Tools/unityroom/";

        /// <summary>unityroom に登録する画面サイズ (キャンバス既定解像度)。</summary>
        private const int CanvasWidth = 960;

        private const int CanvasHeight = 540;

        /// <summary>ビルドに含めるメインシーン。</summary>
        private const string MainScenePath = "Assets/Scenes/QuaternionGlobe.unity";

        /// <summary>WebGL ビルドの出力先 (リポジトリ相対)。.gitignore 対象。</summary>
        private const string OutputPath = "Builds/WebGL-unityroom";

        [MenuItem(MenuRoot + "Apply WebGL Settings", priority = 0)]
        public static void ApplySettings()
        {
            // --- unityroom 必須設定 ---
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;

            // --- 画面サイズ (unityroom のゲーム登録フォームにも同値を入力する) ---
            PlayerSettings.defaultWebScreenWidth = CanvasWidth;
            PlayerSettings.defaultWebScreenHeight = CanvasHeight;

            // --- 推奨設定 ---
            PlayerSettings.runInBackground = true; // フォーカス喪失時も停止させない
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            EnsureBuildScenes();

            // ランタイムで Shader.Find するシェーダはビルドに同梱されないため明示的に含める。
            // (WireGeometry.cs / HalfAngleMirrors.cs が使用。未同梱だとWebGLでモデル非表示になる)
            EnsureAlwaysIncludedShader("Universal Render Pipeline/Unlit");
            EnsureAlwaysIncludedShader("Unlit/Color");

            AssetDatabase.SaveAssets();

            // GraphicsSettings/PlayerSettings など ProjectSettings/*.asset を disk へ確実に反映する。
            // (AssetDatabase.SaveAssets は ProjectSettings をフラッシュしないため)
            EditorApplication.ExecuteMenuItem("File/Save Project");

            Debug.Log(
                $"[unityroom] WebGL設定を適用: Gzip / DecompressionFallback=OFF / {CanvasWidth}x{CanvasHeight} / " +
                $"scenes=[{string.Join(", ", EditorBuildSettings.scenes.Select(s => s.path))}]");
        }

        [MenuItem(MenuRoot + "Build WebGL", priority = 1)]
        public static void BuildWebGL()
        {
            ApplySettings();

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log(
                    $"[unityroom] ビルド成功: {summary.outputPath} " +
                    $"({summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalTime.TotalSeconds:F0}秒)。" +
                    "Build フォルダ内の .loader.js / .data.gz / .framework.js.gz / .wasm.gz を unityroom にアップロードしてください。");
            }
            else
            {
                Debug.LogError($"[unityroom] ビルド失敗: {summary.result} (エラー {summary.totalErrors} 件)");
            }
        }

        [MenuItem(MenuRoot + "Log Current Settings", priority = 20)]
        public static void LogCurrentSettings()
        {
            Debug.Log(
                "[unityroom] 現在のWebGL設定:\n" +
                $"  compressionFormat      = {PlayerSettings.WebGL.compressionFormat} (要: Gzip)\n" +
                $"  decompressionFallback  = {PlayerSettings.WebGL.decompressionFallback} (要: False)\n" +
                $"  defaultWebScreenSize   = {PlayerSettings.defaultWebScreenWidth}x{PlayerSettings.defaultWebScreenHeight}\n" +
                $"  runInBackground        = {PlayerSettings.runInBackground}\n" +
                $"  template               = {PlayerSettings.WebGL.template}\n" +
                $"  dataCaching            = {PlayerSettings.WebGL.dataCaching}\n" +
                $"  exceptionSupport       = {PlayerSettings.WebGL.exceptionSupport}\n" +
                $"  activeBuildTarget      = {EditorUserBuildSettings.activeBuildTarget}\n" +
                $"  scenes                 = [{string.Join(", ", EditorBuildSettings.scenes.Select(s => $"{s.path}({(s.enabled ? "on" : "off")})"))}]");
        }

        /// <summary>
        /// GraphicsSettings の Always Included Shaders に指定シェーダを登録する (登録済みならスキップ)。
        /// Shader.Find でランタイム取得するシェーダの WebGL ビルド同梱漏れ対策。
        /// 注意: ProjectSettings/*.asset は AssetDatabase で読めないため、
        /// GraphicsSettings.GetGraphicsSettings() で実体を取得して SerializedObject 化する。
        /// 反映を disk へ確実に書き出すため、呼び出し側で File > Save Project を行うこと。
        /// </summary>
        private static void EnsureAlwaysIncludedShader(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[unityroom] シェーダが見つかりません: {shaderName}");
                return;
            }

            var graphicsSettings = GraphicsSettings.GetGraphicsSettings();
            var serialized = new SerializedObject(graphicsSettings);
            SerializedProperty list = serialized.FindProperty("m_AlwaysIncludedShaders");
            if (list == null)
            {
                Debug.LogError("[unityroom] m_AlwaysIncludedShaders プロパティを取得できませんでした。");
                return;
            }

            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                {
                    return; // 登録済み
                }
            }

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            serialized.ApplyModifiedProperties();
            Debug.Log($"[unityroom] Always Included Shaders に追加: {shaderName}");
        }

        /// <summary>ビルド対象シーンを QuaternionGlobe に揃える (SampleScene は含めない)。</summary>
        private static void EnsureBuildScenes()
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            bool alreadyCorrect =
                current.Length == 1 && current[0].enabled && current[0].path == MainScenePath;
            if (alreadyCorrect)
            {
                return;
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true) };
            Debug.Log($"[unityroom] Scenes In Build を [{MainScenePath}] に設定しました。");
        }
    }
}
