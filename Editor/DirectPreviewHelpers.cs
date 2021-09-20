#if UNITY_ANDROID
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using Wave.XR.DirectPreview;
using Wave.XR.DirectPreview.Editor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;

namespace Editor
{
    public class DirectPreviewHelpers
    {
        static void BlockForTime(float seconds)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed.Seconds < seconds)
            {
            	;
            }
            
        }
        [UnityEditor.MenuItem("ASINK/Example Start", priority = 801)]
        static void ExampleStart()
        {
            ExampleSetPath();

            ExampleStop();
            
            BlockForTime(0.3f);
            //DirectPreviewCore.EnableDirectPreview = true; //make sure we're enabled 
            //todo check manifests/etc? 
            //reachability test to device? ie ping 
            //check version?
            
            EditorApplication.ExecuteMenuItem("Wave/DirectPreview/Install Device APK"); //DirectPreviewAPK.InstallSimulator();
            
            StreamingServer.StartStreamingServer();
            
            BlockForTime(0.3f);
            EditorApplication.ExecuteMenuItem("Wave/DirectPreview/Start Device APK"); //DirectPreviewAPK.StartSimulator();
            
            BlockForTime(0.3f);
            EditorApplication.isPlaying = true; //causes a domain reload
        }
        
        [UnityEditor.MenuItem("ASINK/Example Stop")]
        static void ExampleStop()
        {
            ExampleSetPath();
            EditorApplication.ExecuteMenuItem("Wave/DirectPreview/Stop Device APK"); //StopSimulator();
            StreamingServer.StopStreamingServer();
            
            if (EditorApplication.isPlaying)
            {
            	EditorApplication.isPlaying = false; //causes a domain reload
            }
            BlockForTime(0.3f);
        }
        
        [UnityEditor.MenuItem("Wave/DirectPreview/set path")]
        static void ExampleSetPath()
        {
           var pathValue = Environment.GetEnvironmentVariable("PATH");
           if (pathValue.Contains("platform-tools")) return;
           //example value D:/env/unityembedded2019_4_19/SDK , but will default to a value that may be wonky that includes a space as it's installed under "C:\Program Files\" by default (or to be specific something like C:\Program Files\Unity\Hub\Editor\2019.4.26f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools )
           //the space really throws off everything in java-land, so I'm not sure what will work
           var androidSDKRoot = EditorPrefs.GetString("AndroidSdkRoot");
           if (string.IsNullOrEmpty(androidSDKRoot))
           {
              UnityEngine.Debug.LogError("unable to find sdk root for running adb commands");
              return;
           }
           UnityEngine.Debug.Log($"{androidSDKRoot}/platform-tools");
           Environment.SetEnvironmentVariable("PATH",$"{pathValue};{androidSDKRoot}/platform-tools");
        }

        
    }
}
#endif