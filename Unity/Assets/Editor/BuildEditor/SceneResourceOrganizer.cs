using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SceneResourceOrganizer : EditorWindow
{
    // 配置路径
    private readonly string SCENE_ROOT = "Assets/Scenes/";
    private readonly string TARGET_ROOT = "Assets/Art/Scenes/";
    private readonly string COMMON_DIR = "Common";

    // 场景依赖缓存
    private Dictionary<string, List<string>> _sceneToAssets = new();
    private Dictionary<string, int> _assetRefCount = new();
    private List<string> _allScenes = new();

    [MenuItem("Tools/场景资源整理工具", false, 10)]
    public static void ShowWindow()
    {
        GetWindow<SceneResourceOrganizer>("场景资源整理");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("功能：收集Scenes下所有场景引用的资源，按公用/单独场景分类移动到Art/Scenes", MessageType.Info);
        GUILayout.Space(10);

        if (GUILayout.Button("开始整理场景资源", GUILayout.Height(40)))
        {
            Execute();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("排除：Init场景、脚本、编辑器、代码相关文件\n" +
                                "公用资源 → Art/Scenes/Common\n" +
                                "单独资源 → Art/Scenes/[场景名]", MessageType.None);
    }

    private void Execute()
    {
        if (!Directory.Exists(SCENE_ROOT))
        {
            EditorUtility.DisplayDialog("错误", $"目录不存在：{SCENE_ROOT}", "确定");
            return;
        }

        EditorUtility.DisplayProgressBar("扫描场景", "查找场景文件...", 0);

        // 1. 找到所有场景（排除Init）
        FindAllScenes();

        if (_allScenes.Count == 0)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("提示", "没有找到可处理的场景", "确定");
            return;
        }

        // 2. 收集每个场景依赖的资源
        CollectSceneDependencies();

        // 3. 统计资源引用次数
        CalculateAssetRefCount();

        // 4. 移动资源
        MoveAssetsToTargetPath();

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", "场景资源整理完成！", "确定");
    }

    #region 1. 查找所有场景（排除Init）
    private void FindAllScenes()
    {
        _allScenes.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { SCENE_ROOT });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);

            // 排除 Init 场景
            if (name.IndexOf("Empty", StringComparison.OrdinalIgnoreCase) >= 0
                ||name.IndexOf("Init", StringComparison.OrdinalIgnoreCase) >= 0
                ||name.IndexOf("Login", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            _allScenes.Add(path);
            Debug.Log($"找到场景：{path}");
        }
    }
    #endregion

    #region 2. 收集场景依赖（只保留资源，排除代码）
    private void CollectSceneDependencies()
    {
        _sceneToAssets.Clear();

        for (int i = 0; i < _allScenes.Count; i++)
        {
            string scenePath = _allScenes[i];
            EditorUtility.DisplayProgressBar("收集依赖",
                $"[{i + 1}/{_allScenes.Count}] {scenePath}",
                (float)i / _allScenes.Count);

            // 获取所有依赖
            string[] dependencies = AssetDatabase.GetDependencies(scenePath, true);
            List<string> validAssets = new List<string>();

            foreach (var dep in dependencies)
            {
                // 跳过场景本身
                if (dep == scenePath) continue;

                // 只保留有效资源
                if (IsValidResource(dep))
                {
                    validAssets.Add(dep);
                }
            }

            _sceneToAssets[scenePath] = validAssets;
        }
    }
    #endregion

    #region 3. 判断是否是有效资源（排除代码）
    private bool IsValidResource(string path)
    {
        // 排除的类型
        string[] excludeExts =
        {
            ".cs", ".js", ".boo", ".dll", ".exe",
            ".meta", ".asmdef", ".asmref", ".asset"
        };

        string ext = Path.GetExtension(path).ToLower();

        // 代码相关直接排除
        foreach (var e in excludeExts)
        {
            if (ext == e) return false;
        }

        // 排除编辑器目录
        if (path.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        // 必须在 Assets 目录内
        if (!path.StartsWith("Assets/"))
            return false;

        return true;
    }
    #endregion

    #region 4. 统计资源被多少场景引用
    private void CalculateAssetRefCount()
    {
        _assetRefCount.Clear();

        foreach (var pair in _sceneToAssets)
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

    #region 5. 移动资源
    private void MoveAssetsToTargetPath()
    {
        int total = _assetRefCount.Count;
        int index = 0;

        foreach (var assetPair in _assetRefCount)
        {
            index++;
            string assetPath = assetPair.Key;
            int refCount = assetPair.Value;

            EditorUtility.DisplayProgressBar("移动资源",
                $"[{index}/{total}] {assetPath}",
                (float)index / total);

            // 目标目录
            string targetDir;

            if (refCount >= 2)
            {
                // 公共资源
                targetDir = Path.Combine(TARGET_ROOT, COMMON_DIR);
            }
            else
            {
                // 找到唯一使用它的场景
                string ownerScene = FindSceneOwner(assetPath);
                if (string.IsNullOrEmpty(ownerScene)) continue;

                string sceneName = Path.GetFileNameWithoutExtension(ownerScene);
                targetDir = Path.Combine(TARGET_ROOT, sceneName);
            }

            // 创建目录
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            // 移动
            MoveAsset(assetPath, targetDir);
        }
    }
    #endregion

    #region 找到资源归属场景
    private string FindSceneOwner(string assetPath)
    {
        foreach (var pair in _sceneToAssets)
        {
            if (pair.Value.Contains(assetPath))
                return pair.Key;
        }

        return null;
    }
    #endregion

    #region 移动文件（保持目录结构，避免冲突）
    private void MoveAsset(string srcPath, string targetDir)
    {
        try
        {
            string fileName = Path.GetFileName(srcPath);
            string targetPath = Path.Combine(targetDir, fileName);

            // 重名处理
            if (File.Exists(targetPath))
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                targetPath = Path.Combine(targetDir, $"{name}_{Guid.NewGuid():N}{ext}");
            }

            AssetDatabase.MoveAsset(srcPath, targetPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"移动失败：{srcPath} → {e.Message}");
        }
    }
    #endregion
}