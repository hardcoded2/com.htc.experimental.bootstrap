using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class TestBuilder
    {
        [MenuItem("TestBuilder/TestBuild")]
        public static void Build()
        {
            //Application.unityVersion
            Debug.Log($"{Builder.EscapedUnityVersion()}");
            
        }

        [MenuItem("TestBuilder/TestResolve 1")]
        public static void TestResolve()
        {
            
        }
        [MenuItem("TestBuilder/TestResolve 2")]
        public static void TestResolve2()
        {
            
        }

        public class Builder
        {
            public string AppName = $"VRTestApp{EscapedUnityVersion()}";
            public BuildOptions BuildOptions = BuildOptions.None;
            public static string[] ScenesInApp()
            {
                List<string> scenes = new List<string>();
                foreach(var scene in EditorBuildSettings.scenes)
                {
                    if(scene.enabled)
                        scenes.Add(scene.path);
                }

                return scenes.ToArray();
            }
            public static string EscapedUnityVersion()
            {
                return Application.unityVersion.Replace(".", "_");
            }
            public void BuildAndroid()
            {
                //Application.identifier = $"com.htc.{appName}"
                if (Application.levelCount == 0)
                {
                    throw new InvalidOperationException("No levels set in player settings");
                }

                var bundleIdentifier = $"com.htc.{AppName}";
                //PlayerSettings.applicationIdentifier = bundleIdentifier;
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,bundleIdentifier);
                //Application.productName = AppName;
                var apkName = $"{bundleIdentifier.Replace(".", "_")}.apk";
                const string buildDirName = "Builds";
                if (!Directory.Exists(buildDirName))
                    Directory.CreateDirectory(buildDirName);
                BuildPipeline.BuildPlayer(new BuildPlayerOptions()
                {
                    target = BuildTarget.Android, scenes = ScenesInApp(), locationPathName = $"{buildDirName}/{apkName}",options = BuildOptions
                });
            }
        }
    }
}