using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Editor
{
    public class TestBuilder
    {

        public class SampleConfigBuilder
        {
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
            private static string EscapedUnityVersion()
            {
                return Application.unityVersion.Replace(".", "_");
            }

            private static string EscapePartialBundleID(string bundleid)
            {
                
                StringBuilder sb = new StringBuilder();
                foreach (var c in bundleid)
                {
                    if (char.IsLetterOrDigit(c))
                    {
                        sb.Append(c);
                    }
                }

                return sb.ToString();
            }
            public static BuildConfig GetSampleConfig()
            {
                var appName = $"VRTestApp{EscapedUnityVersion()}";
                return new BuildConfig()
                {
                    AppName = appName,
                    BundleID = $"com.htc.{EscapePartialBundleID(appName)}",
                    
                    BuildTarget = BuildTarget.Android,
                    BuildTargetGroup = BuildTargetGroup.Android,
                    Scenes = new List<string>(ScenesInApp()),
                };
            }
        }
        public class BuildConfig
        {
            public string AppName;
            public string BundleID; //not used on pc
            
            public BuildTarget BuildTarget;
            public BuildTargetGroup BuildTargetGroup;
            public List<string> Scenes;
            
            public BuildOptions BuildOptions = BuildOptions.None;
        }
        public class Builder
        {
            
            public void Build(BuildConfig buildConfig)
            {
                if (SceneManager.sceneCountInBuildSettings == 0)
                {
                    throw new InvalidOperationException("No levels set in player settings");
                }
                
                PlayerSettings.SetApplicationIdentifier(buildConfig.BuildTargetGroup,buildConfig.BundleID);
                
                const string buildDirName = "Builds";
                if (!Directory.Exists(buildDirName))
                    Directory.CreateDirectory(buildDirName);
                
                var apkName = $"{buildConfig.BundleID.Replace(".", "_")}.apk";
                BuildPipeline.BuildPlayer(new BuildPlayerOptions()
                {
                    target = BuildTarget.Android, scenes = buildConfig.Scenes.ToArray(), locationPathName = $"{buildDirName}/{apkName}",options = buildConfig.BuildOptions
                });
            }
        }
    }
}