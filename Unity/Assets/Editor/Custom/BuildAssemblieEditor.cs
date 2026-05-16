using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor;

namespace ET
{
    public static class BuildAssemblieEditor
    {
        private const string CodeDir = "Assets/Bundles/Code/";

        [MenuItem("Tools/Build/BuildWoLongCodeForBundle")]
        public static void BuildCodeForBundle()
        {
            CompileDllCommand.CompileDll(EditorUserBuildSettings.activeBuildTarget);
            AfterCompiling_wolong(Define.HybridCLRBuildHotFixOutputDir);
        }


        private static void AfterCompiling()
        {
            while (EditorApplication.isCompiling)
            {
                Debug.Log("Compiling wait1");
                // 主线程sleep并不影响编译线程
                Thread.Sleep(1000);
                Debug.Log("Compiling wait2");
            }

            Debug.Log("Compiling finish");

            Directory.CreateDirectory(CodeDir);
            File.Copy(Path.Combine(Define.BuildOutputDir, "Code.dll"), Path.Combine(CodeDir, "Code.dll.bytes"), true);
            File.Copy(Path.Combine(Define.BuildOutputDir, "Code.pdb"), Path.Combine(CodeDir, "Code.pdb.bytes"), true);

            AssetDatabase.Refresh();
            Debug.Log("copy Code.dll to Bundles/Code success!");

            // 设置ab包
            AssetImporter assetImporter1 = AssetImporter.GetAtPath("Assets/Bundles/Code/Code.dll.bytes");
            assetImporter1.assetBundleName = "Code.unity3d";
            AssetImporter assetImporter2 = AssetImporter.GetAtPath("Assets/Bundles/Code/Code.pdb.bytes");
            assetImporter2.assetBundleName = "Code.unity3d";
            AssetDatabase.Refresh();
            Debug.Log("set assetbundle success!");

            Debug.Log("build success!");
            //反射获取当前Game视图，提示编译完成
            ShowNotification("Build Code Success");
        }

        private static void AfterCompiling_wolong(string hotDllPath)
        {
            while (EditorApplication.isCompiling)
            {
                Debug.Log("Compiling wait1");
                // 主线程sleep并不影响编译线程
                Thread.Sleep(1000);
                Debug.Log("Compiling wait2");
            }

            Debug.Log("Compiling finish");

            Directory.CreateDirectory(CodeDir);
            //File.Copy(Path.Combine(Define.BuildOutputDir, "Code.dll"), Path.Combine(CodeDir, "Code.dll.bytes"), true);
            //File.Copy(Path.Combine(Define.BuildOutputDir, "Code.pdb"), Path.Combine(CodeDir, "Code.pdb.bytes"), true);
            //File.Copy(Path.Combine(Define.HybridCLRBuildOutputDir, "Unity.Mono.dll"), Path.Combine(CodeDir, "Unity.Mono.dll.bytes"), true);
            //File.Copy(Path.Combine(Define.HybridCLRBuildOutputDir, "Unity.ThirdParty.dll"), Path.Combine(CodeDir, "Unity.ThirdParty.dll.bytes"), true);
            //File.Copy(Path.Combine(Define.HybridCLRBuildOutputDir, "mscorlib.dll"), Path.Combine(CodeDir, "mscorlib.dll.bytes"), true);
            //File.Copy(Path.Combine(Define.HybridCLRBuildOutputDir, "System.Core.dll"), Path.Combine(CodeDir, "System.Core.dll.bytes"), true);
            //File.Copy(Path.Combine(Define.HybridCLRBuildOutputDir, "System.dll"), Path.Combine(CodeDir, "System.dll.bytes"), true);

            //AssetDatabase.Refresh();
            //Debug.Log("copy Code.dll to Bundles/Code success!");

            //// 设置ab包
            //AssetImporter assetImporter1 = AssetImporter.GetAtPath("Assets/Bundles/Code/Code.dll.bytes");
            //assetImporter1.assetBundleName = "Code.unity3d";
            //AssetImporter assetImporter2 = AssetImporter.GetAtPath("Assets/Bundles/Code/Code.pdb.bytes");
            //assetImporter2.assetBundleName = "Code.unity3d";

            //List<string> allHotUpdateDllFiles = SettingsUtil.BuildAssemblieEditor;
            List<string> allHotUpdateDllFiles = SettingsUtil.HotUpdateAssemblyFilesExcludePreserved;
            foreach (var dll in allHotUpdateDllFiles)
            {
                string file = Path.Combine(hotDllPath, dll);
                if (!File.Exists(file))
                {
                    Debug.LogError($"不存在dll:{file}, 无法跑huatuo模式");
                    continue;
                }
                File.Copy(file, Path.Combine(CodeDir, $"{dll}.bytes"), true);
                AssetDatabase.Refresh();

                AssetImporter assetImporter1 = AssetImporter.GetAtPath($"Assets/Bundles/Code/{dll}.bytes");
                assetImporter1.assetBundleName = "Code.unity3d";
            }


            List<string> aotDllFiles = new List<string>()
            {
                "mscorlib.dll",
                "System.dll",
                "System.Core.dll",
                "Unity.Mono.dll",
                "Unity.ThirdParty.dll",
            };
            foreach (var dll in aotDllFiles)
            {
                string file = Path.Combine(Define.HybridCLRCutOutputDir, dll);
                if (!File.Exists(file))
                {
                    Debug.LogError($"不存在dll:{file}, 无法跑huatuo模式");
                    continue;
                }
                File.Copy(file, Path.Combine(CodeDir, $"{dll}.bytes"), true);
                AssetDatabase.Refresh();

                AssetImporter assetImporter1 = AssetImporter.GetAtPath($"Assets/Bundles/Code/{dll}.bytes");
                assetImporter1.assetBundleName = "Code.unity3d";
            }

            AssetDatabase.Refresh();
            Debug.Log("set assetbundle success!");

            Debug.Log("build success!");
            //反射获取当前Game视图，提示编译完成
            ShowNotification("Build Code Success");
        }

        public static void ShowNotification(string tips)
        {
            var game = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            game?.ShowNotification(new GUIContent($"{tips}"));
        }
    }

}