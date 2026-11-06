#if UNITY_EDITOR
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using static System.IO.Directory;
using static System.IO.Path;
using static UnityEditor.AssetDatabase;
using System.IO;

namespace MyTools
{
    public static class Setup
    {
        [MenuItem("Tools/Setup/Create Default Folders")]
        public static void CreateDefaultFolders()
        {
            // 创建默认文件夹
            Folders.CreateDefault(
             "",
                "Art/Shapes", "Art/Textures", "Art/Fonts", "Art/Illustrations", "Art/PostProcess", "Art/Animation", "Art/UI",
                 "Materials", "Materials/Shader", "Materials/Material",
                "Prefabs", "Prefabs/Managers", "Prefabs/UI",
                "Scenes/Levels", "Scenes/Test",
                "Scripts", "Scripts/Manager", "Scripts/Gameplay", "Scripts/UI",
                "Resources/Audio/SFX", "Resources/Audio/Loop", "Resources/SO", "Resources/Prefabs",
                "Settings/Configs");
            Refresh();
        }

        // [MenuItem("Tools/Setup/Import My Favorite Assets")]
        // public static void ImportMyFavoriteAssets()
        // {
        //     //从资源商店导入资源
        //     //导入 DOTween
        //     Assets.ImportAsset("DOTween HOTween v2.unitypackage", "Demigiant/Editor ExtensionsAnimation");
        //     //刷新资源数据库
        //     AssetDatabase.Refresh();
        // }

        // [MenuItem("Tools/Setup/Install Netcode for GameObjects")]
        // public static void InstallNetcodeForGameObjects()
        // {
        //     Packages.InstallPackages(new[] {
        //         "com.unity.multiplayer.tools",
        //         "com.unity.netcode.gameobjects"
        //     });
        // }

        // [MenuItem("Tools/Setup/Install Unity AI Navigation")]
        // public static void InstallUnityAINavigation()
        // {
        //     Packages.InstallPackages(new[] {
        //         "com.unity.ai.navigation"
        //     });
        // }

        [MenuItem("Tools/Setup/Install My Favorite Open Source")]
        public static void InstallOpenSource()
        {
            // 安装开源库
            Packages.InstallPackages(new[] {
                // "git+https://github.com/KyleBanks/scene-ref-attribute",
                // "git+https://github.com/starikcetin/Eflatun.SceneReference.git#3.1.1",
                "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
                //"https://github.com/ls9512/UEvent.git"
            });
        }
        [MenuItem("Tools/Setup/Clean Project Librarys And Temp")]
        public static void CleanProject()
        {
            // 删除 Library 文件夹
            var libraryPath = Path.Combine(Application.dataPath, "../Library");
            if (Directory.Exists(libraryPath))
            {
                Directory.Delete(libraryPath, true);
            }

            // 删除 Temp 文件夹
            var tempPath = Path.Combine(Application.dataPath, "../Temp");
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }

            Debug.Log("已清理项目缓存，请重新打开项目");
            EditorApplication.Exit(0);
        }

        static class Folders
        {
            public static void CreateDefault(string root, params string[] folders)
            {
                var fullpath = Path.Combine(Application.dataPath, root);
                if (!Directory.Exists(fullpath))
                {
                    Directory.CreateDirectory(fullpath);
                }
                foreach (var folder in folders)
                {
                    CreateSubFolders(fullpath, folder);
                }
            }

            private static void CreateSubFolders(string rootPath, string folderHierarchy)
            {
                var folders = folderHierarchy.Split('/');
                var currentPath = rootPath;
                foreach (var folder in folders)
                {
                    currentPath = Path.Combine(currentPath, folder);
                    if (!Directory.Exists(currentPath))
                    {
                        Directory.CreateDirectory(currentPath);
                    }
                }
            }
        }

        static class Packages
        {
            static AddRequest Request;
            static Queue<string> PackagesToInstall = new();
            static bool isInstalling = false;
            static int retryCount = 0;
            const int MAX_RETRIES = 3;
            const int RETRY_DELAY = 2000; // 2秒

            public static void InstallPackages(string[] packages)
            {
                if (isInstalling)
                {
                    Debug.LogWarning("已有包正在安装中，请等待完成...");
                    return;
                }

                foreach (var package in packages)
                {
                    PackagesToInstall.Enqueue(package);
                }

                // 开始安装第一个包
                if (PackagesToInstall.Count > 0)
                {
                    InstallNextPackage();
                }
            }

            private static async void InstallNextPackage()
            {
                if (PackagesToInstall.Count == 0)
                {
                    isInstalling = false;
                    Debug.Log("所有包安装完成！");
                    return;
                }

                try
                {
                    isInstalling = true;
                    string packageToInstall = PackagesToInstall.Peek(); // 只查看不移除

                    Debug.Log($"正在安装包: {packageToInstall}");
                    Request = Client.Add(packageToInstall);
                    EditorApplication.update += Progress;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"安装包时出错: {e.Message}");
                    await RetryOrSkip();
                }
            }

            private static async Task RetryOrSkip()
            {
                if (retryCount < MAX_RETRIES)
                {
                    retryCount++;
                    Debug.Log($"将在 {RETRY_DELAY / 1000} 秒后重试... (尝试 {retryCount}/{MAX_RETRIES})");
                    await Task.Delay(RETRY_DELAY);
                    InstallNextPackage();
                }
                else
                {
                    Debug.LogError($"包 {PackagesToInstall.Peek()} 安装失败，跳过...");
                    PackagesToInstall.Dequeue(); // 移除失败的包
                    retryCount = 0;
                    await Task.Delay(1000);
                    InstallNextPackage();
                }
            }

            static async void Progress()
            {
                if (Request.IsCompleted)
                {
                    EditorApplication.update -= Progress;

                    if (Request.Status == StatusCode.Success)
                    {
                        Debug.Log($"成功安装: {Request.Result.packageId}");
                        PackagesToInstall.Dequeue(); // 成功后移除
                        retryCount = 0;
                    }
                    else if (Request.Status >= StatusCode.Failure)
                    {
                        Debug.LogError($"安装失败: {Request.Error.message}");
                        await RetryOrSkip();
                        return;
                    }

                    // 添加延迟后继续安装下一个包
                    await Task.Delay(1000);
                    InstallNextPackage();
                }
            }
        }

        static class Assets
        {
            public static void ImportAsset(string asset, string subfolder,
                string rootFolder = "C:/Users/19118/AppData/Roaming/Unity/Asset Store-5.x")
            {
                ImportPackage(Combine(rootFolder, subfolder, asset), false);
            }
        }
    }
}
#endif