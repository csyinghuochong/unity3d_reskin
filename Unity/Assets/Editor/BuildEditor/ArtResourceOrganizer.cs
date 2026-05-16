using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ArtResourceOrganizer : EditorWindow
{
    // 来源目录
    private const string EFFECT_BUNDLE_ROOT = "Assets/Bundles/Models/ZuoQi";
    
    // 目标目录
    private const string TARGET_ROOT = "Assets/Art/ZuoQi";
    private const string COMMON_FOLDER = "Common";

    // 数据结构
    private Dictionary<string, List<string>> _effectToAssets = new();
    private Dictionary<string, int> _assetRefCount = new();
    private List<string> _allEffectPrefabs = new();

    [MenuItem("Tools/通用资源整理工具", false, 11)]
    public static void ShowWindow()
    {
        GetWindow<ArtResourceOrganizer>("通用资源整理工具");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.HelpBox($"功能：把 {EFFECT_BUNDLE_ROOT} 里的特效及依赖，整理到 {TARGET_ROOT}", MessageType.Info);
        GUILayout.Space(10);

        if (GUILayout.Button("开始整理特效资源", GUILayout.Height(40)))
        {
            Execute();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "规则：\n" +
            "• 一个特效 = 一个文件夹\n" +
            "• 共用资源 → Art/Effects/Common\n" +
            "• 不移动任何脚本/代码/DLL",
            MessageType.None);
    }

    private void Execute()
    {
        if (!Directory.Exists(EFFECT_BUNDLE_ROOT))
        {
            EditorUtility.DisplayDialog("错误", $"目录不存在：\n{EFFECT_BUNDLE_ROOT}", "确定");
            return;
        }

        // 1. 找到所有特效预制体
        EditorUtility.DisplayProgressBar("扫描", "查找资源预制体...", 0);
        FindAllEffectPrefabs();

        if (_allEffectPrefabs.Count == 0)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("提示", "没有找到任何资源预制体", "确定");
            return;
        }

        // 2. 收集每个特效的依赖资源
        CollectEffectDependencies();

        // 3. 统计资源被多少特效共用
        CalculateAssetRefCount();

        // 4. 移动资源
        MoveAllAssets();

        // 结束
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", "资源整理完成！", "确定");
    }

    #region 查找所有特效预制体
    private void FindAllEffectPrefabs()
    {
        _allEffectPrefabs.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { EFFECT_BUNDLE_ROOT });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            _allEffectPrefabs.Add(path);
            Debug.Log($"找到资源：{path}");
        }
    }
    #endregion

    #region 收集每个特效依赖（只保留资源）
    private void CollectEffectDependencies()
    {
        _effectToAssets.Clear();

        for (int i = 0; i < _allEffectPrefabs.Count; i++)
        {
            string prefabPath = _allEffectPrefabs[i];
            EditorUtility.DisplayProgressBar(
                "收集依赖",
                $"[{i+1}/{_allEffectPrefabs.Count}] {prefabPath}",
                (float)i / _allEffectPrefabs.Count);

            string[] deps = AssetDatabase.GetDependencies(prefabPath, true);
            List<string> validAssets = new List<string>();

            foreach (var dep in deps)
            {
                if (dep == prefabPath) continue;
                if (IsValidResource(dep))
                {
                    validAssets.Add(dep);
                }
            }

            _effectToAssets[prefabPath] = validAssets;
        }
    }
    #endregion

    #region 判断是否是有效资源（排除脚本）
    private bool IsValidResource(string path)
    {
        string[] excludeExts =
        {
            ".cs", ".js", ".boo", ".dll", ".exe",
            ".meta", ".asmdef", ".asmref", ".asset"
        };

        string ext = Path.GetExtension(path).ToLower();
        foreach (var e in excludeExts)
        {
            if (ext == e) return false;
        }

        if (path.Contains("/Editor/")) return false;
        if (!path.StartsWith("Assets/")) return false;

        return true;
    }
    #endregion

    #region 统计资源引用次数
    private void CalculateAssetRefCount()
    {
        _assetRefCount.Clear();

        foreach (var pair in _effectToAssets)
        {
            foreach (var asset in pair.Value)
            {
                if (!_assetRefCount.ContainsKey(asset))
                    _assetRefCount[asset] = 0;

                _assetRefCount[asset]++;
            }
        }
    }
    #endregion

    #region 移动所有资源
    private void MoveAllAssets()
    {
        int total = _assetRefCount.Count;
        int index = 0;

        foreach (var pair in _assetRefCount)
        {
            index++;
            string assetPath = pair.Key;
            int refCount = pair.Value;

            EditorUtility.DisplayProgressBar(
                "移动资源",
                $"[{index}/{total}] {assetPath}",
                (float)index / total);

            string targetDir;

            if (refCount >= 2)
            {
                // 公共资源
                targetDir = Path.Combine(TARGET_ROOT, COMMON_FOLDER);
            }
            else
            {
                // 找到归属特效
                string ownerEffect = FindOwnerEffect(assetPath);
                if (string.IsNullOrEmpty(ownerEffect)) continue;

                string effectName = Path.GetFileNameWithoutExtension(ownerEffect);
                targetDir = Path.Combine(TARGET_ROOT, effectName);
            }

            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            MoveAsset(assetPath, targetDir);
        }

        // 最后移动特效预制体自己
        //MoveEffectPrefabs();
    }
    #endregion

    #region 移动特效预制体
    private void MoveEffectPrefabs()
    {
        foreach (var prefabPath in _allEffectPrefabs)
        {
            string effectName = Path.GetFileNameWithoutExtension(prefabPath);
            string targetDir = Path.Combine(TARGET_ROOT, effectName);
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
            MoveAsset(prefabPath, targetDir);
        }
    }
    #endregion

    #region 找到资源归属特效
    private string FindOwnerEffect(string assetPath)
    {
        foreach (var pair in _effectToAssets)
        {
            if (pair.Value.Contains(assetPath))
                return pair.Key;
        }
        return null;
    }
    #endregion

    #region 移动文件（安全、重名处理）
    private void MoveAsset(string srcPath, string targetDir)
    {
        try
        {
            string fileName = Path.GetFileName(srcPath);
            string targetPath = Path.Combine(targetDir, fileName);

            if (File.Exists(targetPath))
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                targetPath = Path.Combine(targetDir, $"{name}_{Guid.NewGuid():N}{ext}");
            }

            Debug.Log($"{srcPath}:  to  {targetPath}");
            AssetDatabase.MoveAsset(srcPath, targetPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"移动失败：{srcPath} → " + e.Message);
        }
    }
    #endregion
}