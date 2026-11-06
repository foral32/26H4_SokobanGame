using UnityEngine;
using UnityEditor;

namespace PostProcessing.Outline
{
    [CustomEditor(typeof(OutlineController))]
    public class OutlineControllerEditor : Editor
    {
        private SerializedProperty enableOutline;
        private SerializedProperty outlineThickness;
        private SerializedProperty outlineColor;
        private SerializedProperty depthThreshold;
        private SerializedProperty normalThreshold;
        private SerializedProperty colorThreshold;
        private SerializedProperty depthSensitivity;
        private SerializedProperty normalsSensitivity;
        private SerializedProperty colorSensitivity;
        private SerializedProperty useDepthNormals;
        private SerializedProperty useColorDifference;
        private SerializedProperty enableAntiAliasing;
        private SerializedProperty useEnhancedDetection;
        private SerializedProperty robertsWeight;
        private SerializedProperty maxDiffWeight;
        private SerializedProperty useInternalEdgeDistanceFade;
        private SerializedProperty internalDepthThreshold;
        private SerializedProperty internalEdgeFadeStart;
        private SerializedProperty internalEdgeFadeEnd;
        private SerializedProperty edgeSoftness;
        private SerializedProperty outlineFeature;
        private SerializedProperty autoDetectFeature;
        private SerializedProperty targetRenderer;

        private void OnEnable()
        {
            enableOutline = serializedObject.FindProperty("enableOutline");
            outlineThickness = serializedObject.FindProperty("outlineThickness");
            outlineColor = serializedObject.FindProperty("outlineColor");
            depthThreshold = serializedObject.FindProperty("depthThreshold");
            normalThreshold = serializedObject.FindProperty("normalThreshold");
            colorThreshold = serializedObject.FindProperty("colorThreshold");
            depthSensitivity = serializedObject.FindProperty("depthSensitivity");
            normalsSensitivity = serializedObject.FindProperty("normalsSensitivity");
            colorSensitivity = serializedObject.FindProperty("colorSensitivity");
            useDepthNormals = serializedObject.FindProperty("useDepthNormals");
            useColorDifference = serializedObject.FindProperty("useColorDifference");
            enableAntiAliasing = serializedObject.FindProperty("enableAntiAliasing");
            useEnhancedDetection = serializedObject.FindProperty("useEnhancedDetection");
            robertsWeight = serializedObject.FindProperty("robertsWeight");
            maxDiffWeight = serializedObject.FindProperty("maxDiffWeight");
            useInternalEdgeDistanceFade = serializedObject.FindProperty("useInternalEdgeDistanceFade");
            internalDepthThreshold = serializedObject.FindProperty("internalDepthThreshold");
            internalEdgeFadeStart = serializedObject.FindProperty("internalEdgeFadeStart");
            internalEdgeFadeEnd = serializedObject.FindProperty("internalEdgeFadeEnd");
            edgeSoftness = serializedObject.FindProperty("edgeSoftness");
            outlineFeature = serializedObject.FindProperty("outlineFeature");
            autoDetectFeature = serializedObject.FindProperty("autoDetectFeature");
            targetRenderer = serializedObject.FindProperty("targetRenderer");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var controller = (OutlineController)target;

            EditorGUILayout.Space();

            // Feature Reference Section
            EditorGUILayout.LabelField("Feature Reference", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(autoDetectFeature);

            if (autoDetectFeature.boolValue)
            {
                EditorGUILayout.PropertyField(targetRenderer);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Find Feature Now"))
                {
                    controller.ManualFindFeature();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.PropertyField(outlineFeature);
            }

            // Status display
            if (controller.outlineFeature != null)
            {
                EditorGUILayout.HelpBox("✓ PostProcessOutlineFeature已连接", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("⚠ 请分配PostProcessOutlineFeature或启用自动检测", MessageType.Warning);
            }

            EditorGUILayout.Space();

            // Main Control
            EditorGUILayout.LabelField("Outline Control", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableOutline);

            if (!enableOutline.boolValue)
            {
                EditorGUILayout.HelpBox("描边已禁用", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Basic Settings
            EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(outlineThickness);
            EditorGUILayout.PropertyField(outlineColor);

            EditorGUILayout.Space();

            // Detection Settings
            EditorGUILayout.LabelField("Detection Thresholds", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(depthThreshold);
            EditorGUILayout.PropertyField(normalThreshold);
            EditorGUILayout.PropertyField(colorThreshold);

            EditorGUILayout.Space();

            // Sensitivity Settings
            EditorGUILayout.LabelField("Sensitivity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(depthSensitivity);
            EditorGUILayout.PropertyField(normalsSensitivity);
            EditorGUILayout.PropertyField(colorSensitivity);

            EditorGUILayout.Space();

            #region 内部边缘距离控制

            EditorGUILayout.LabelField("内部边缘距离控制", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useInternalEdgeDistanceFade);
            EditorGUILayout.PropertyField(internalDepthThreshold);
            if (useInternalEdgeDistanceFade.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(internalEdgeFadeStart);
                EditorGUILayout.PropertyField(internalEdgeFadeEnd);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(edgeSoftness);
            EditorGUILayout.HelpBox(
                useInternalEdgeDistanceFade.boolValue
                    ? "外轮廓始终保留；楼梯、折角和颜色细节会在起止距离之间逐渐淡出。"
                    : "距离衰减已关闭；内部描边不会因距离增加而变淡或消失。",
                MessageType.Info);

            EditorGUILayout.Space();

            #endregion

            // Feature Toggles
            EditorGUILayout.LabelField("Feature Toggles", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useDepthNormals);
            EditorGUILayout.PropertyField(useColorDifference);
            EditorGUILayout.PropertyField(enableAntiAliasing);

            EditorGUILayout.Space();

            // Enhanced Detection
            EditorGUILayout.LabelField("Enhanced Detection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useEnhancedDetection);

            if (useEnhancedDetection.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(robertsWeight);
                EditorGUILayout.PropertyField(maxDiffWeight);
                EditorGUI.indentLevel--;

                EditorGUILayout.HelpBox("增强检测会加入对角方向采样，提高斜线和曲线轮廓的连续性。", MessageType.Info);
            }

            EditorGUILayout.Space();

            // // Preset Buttons
            // EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);

            // EditorGUILayout.BeginHorizontal();
            // if (GUILayout.Button("Cartoon Style"))
            // {
            //     controller.ApplyCartoonStyle();
            // }
            // if (GUILayout.Button("Subtle Style"))
            // {
            //     controller.ApplySubtleStyle();
            // }
            // EditorGUILayout.EndHorizontal();

            // EditorGUILayout.BeginHorizontal();
            // if (GUILayout.Button("Technical Style"))
            // {
            //     controller.ApplyTechnicalStyle();
            // }
            // if (GUILayout.Button("Distance Style"))
            // {
            //     controller.ApplyDistanceStyle();
            // }
            // EditorGUILayout.EndHorizontal();

            // EditorGUILayout.Space();

            // Help Section
            if (GUILayout.Button("Show Help"))
            {
                ShowHelpDialog();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ShowHelpDialog()
        {
            EditorUtility.DisplayDialog("PostProcess Outline Help",
                "使用指南:\n\n" +
                "1. 确保已将PostProcessOutlineFeature添加到URP Renderer Asset\n" +
                "2. 启用自动检测或手动分配Feature引用\n" +
                "3. 调整阈值和敏感度参数以获得理想效果\n" +
                "4. 使用预设快速应用常见风格\n" +
                "5. 使用内部边缘距离控制清理远处楼梯等高频细节\n\n" +
                "参数说明:\n" +
                "• Depth Threshold: 深度差异阈值\n" +
                "• Normal Threshold: 法线差异阈值\n" +
                "• Color Threshold: 颜色差异阈值\n" +
                "• Internal Depth Threshold: 内部结构相对深度阈值\n" +
                "• Internal Edge Fade: 内部描边衰减距离\n" +
                "• Edge Softness: 亚像素软阈值宽度",
                "确定");
        }
    }
}
