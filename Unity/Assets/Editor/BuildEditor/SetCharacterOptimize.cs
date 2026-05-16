using UnityEngine;
using UnityEditor;
using System.IO;

public class SetCharacterOptimize : AssetPostprocessor
{
    // 目标角色目录
    private static readonly string TARGET_FOLDER = "Assets/Bundles/Unit";

    // ======================================================================
    // 【核心功能】模型导入时自动执行优化
    // ======================================================================
    void OnPostprocessModel(GameObject go)
    {
        // 只对目标目录下的模型生效
        if (!assetPath.StartsWith(TARGET_FOLDER))
            return;

        SkinnedMeshRenderer[] smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in smrs)
        {
            SetOptimizeSetting(smr);
        }
        MeshRenderer[] mmrs = go.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var smr in mmrs)
        {
            SetOptimizeSetting_2(smr);
        }

        Debug.Log($"✅ 模型导入自动优化：{assetPath}");
    }

    // ======================================================================
    // 【手动批量】给已有预制体一键设置
    // ======================================================================
    [MenuItem("Tools/批量优化角色性能设置")]
    public static void DoSetCharacterOptimize()
    {
        string[] files = Directory.GetFiles(TARGET_FOLDER, "*.prefab", SearchOption.AllDirectories);

        foreach (string file in files)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(file);
            if (prefab == null) continue;

            SkinnedMeshRenderer[] smrs = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer smr in smrs)
            {
                SetOptimizeSetting(smr);
            }

            MeshRenderer[] mmrs  = prefab.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var smr in mmrs)
            {
                SetOptimizeSetting_2(smr);
            }

            
            EditorUtility.SetDirty(prefab);
            Debug.Log($"已优化 → {file}", prefab);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", "所有角色已自动设置为极致性能模式", "OK");
    }

    static void SetOptimizeSetting_2(MeshRenderer smr)
    {
        // 阴影全关
        smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        smr.receiveShadows = false;

        // 探针全关
        smr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        smr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        // 运动向量关
        smr.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
        // 动态遮挡开启
        smr.allowOcclusionWhenDynamic = true;
    }
    
    // ======================================================================
    // 统一优化规则（你截图的那套最省性能设置）
    // ======================================================================
    static void SetOptimizeSetting(SkinnedMeshRenderer smr)
    {
        // 阴影全关
        smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        smr.receiveShadows = false;

        // 探针全关
        smr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        smr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        // 运动向量关
        smr.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
        smr.skinnedMotionVectors = false;

        // 动态遮挡开启
        smr.allowOcclusionWhenDynamic = true;

        // 皮肤品质最低（最省性能）
        smr.quality = SkinQuality.Bone4;
    }
}