using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

//make sure wave sdks installed
//not setting up registries/etc, that will be in the PackageManagerHack
public class WaveBootstrap 
{
    private static EnsurePackagesInstalledEditorHelper EnsurePackagesExist;

    [MenuItem("BOOTSTRAP/MANUALLY RUN")]
    //[InitializeOnLoadMethod]
    public static void EnsureWaveInstalled()
    {
        var previousSavesStore = new EnsurePackagesInstalledEditorHelper.PreviousSavesStore();
        var previousAddAttempt = previousSavesStore.PreviousAttempt();
        if (previousAddAttempt != null)
        {
            Debug.Log("Not clearing state since we tried to load package before");
            return;
        }

        if (previousAddAttempt != null)
        {
            Debug.Log("not running WaveBootStrap again");
            return;
        }

        //var packagesToLookFor = new[] {"com.unity.ugui"};
        EnsurePackagesExist = new EnsurePackagesInstalledEditorHelper(PackagesToAdd());
    }

    private bool IgnorePreviousAttempt(EnsurePackagesInstalledEditorHelper.SerializedPackageAttempt previousAttempt)
    {
#if false //FIXME
            var thisEditorLaunchedAt = new DateTime( EnsurePackagesInstalledEditorHelper.SerializedPackageAttempt
                .ApproximateTimeThatTheEditorLaunchedAt());
            var previousEditorLaunchedAtAbout = new DateTime((long) previousAddAttempt.TimeThisEditorInstanceLaunched);
            var launchTimeDifference = (thisEditorLaunchedAt - previousEditorLaunchedAtAbout);
            //clock drift is real, so allow some give between estimated launches
            if ( launchTimeDifference.TotalSeconds > 5f)
            {
                Debug.Log("Cleared previous attempts to add packages as we probably relaunched the editor");
                //TODO: clear prevoius runs on new edtior load in a more consistent way
                previousSavesStore.Set(null);
                previousAddAttempt = null;
            }
#endif
        return false;
    }

    private static IEnumerable<string> PackagesToAdd()
    {
        var packagesToLookForNames = new[] {"com.htc.upm.wave.xrsdk", "com.htc.upm.wave.essence"};
        
        const string versionToUse = "@4.1.2-test.4"; 
        //const string versionToUse = "@4.1.1-r.3.1";
        //TODO: allow for uninstalling previous versions
        var packagesToLookFor = packagesToLookForNames.Select(packagesToLookForName => $"{packagesToLookForName}@{versionToUse}").ToList();
        return packagesToLookFor;
    }
    [MenuItem("BOOTSTRAP/TESTADD")]
    public static void TESTADD()
    {
        foreach (var package in PackagesToAdd())
        {
            Client.Add(package);
        }
#if UNITY_2020_1_OR_NEWER
        Client.Resolve();
#endif
    }

    [MenuItem("BOOTSTRAP/CLEAR")]
    public static void Clear()
    {
        new EnsurePackagesInstalledEditorHelper.PreviousSavesStore().Set(null);
        
    }

    public class EnsurePackagesInstalledEditorHelper
    {
        public ListRequest PackageList;
        private IEnumerable<string> packagesToHave;
        public EnsurePackagesInstalledEditorHelper(IEnumerable<string> packagesToHave)
        {
            this.packagesToHave = packagesToHave;
            PackageList = Client.List(false, true);

            EditorApplication.update += WaitForList;
        }

        private void WaitForList()
        {
            if (!PackageList.IsCompleted)
                return;
            EditorApplication.update -= WaitForList;
            if (PackageList.Error != null)
            {
                var error = PackageList.Error;
                var msg = $"Could not list requests due to error code {error.errorCode} message: {error.message}";
                Debug.LogError(msg);
                EditorUtility.DisplayDialog("FAIL",msg,"ok");
                return;
            }

            if (HasAllPackages())
            {
                Debug.Log("We have all the packages we expect");
                return;
            }

            AddMissingPackages(MissingPackages());
        }

        private HashSet<string> MissingPackages()
        {
            var allPackagesHashSet = new HashSet<string>(packagesToHave);
            foreach (var packageInfo in PackageList.Result)
            {
                //Support versions in this as well, so check versions as name@version as well
                if (allPackagesHashSet.Contains(packageInfo.name))
                {
                    allPackagesHashSet.Remove(packageInfo.name);
                }

                var versionFormattedPackageString = $"{packageInfo.name}@{packageInfo.version}";
                if (allPackagesHashSet.Contains(versionFormattedPackageString))
                {
                    allPackagesHashSet.Remove(versionFormattedPackageString);
                }
            }

            return allPackagesHashSet;
        }

        [Serializable]
        public class SerializedIndividualPackageAttempt
        {
            public string PackageTriedToAdd;
        }

        //this needs to be serialized as i would expect an add request to cause a domain reload, so we would have to save any state after this in player prefs if we want to do that, including number of previous tries
        [Serializable]
        public class SerializedPackageAttempt
        {
            public string ProjectPath; //verify this try is from the correct project 
            public string LastTimeTriedInUTC;
            public List<SerializedIndividualPackageAttempt> PackagesTried;
            public int FailuresDuringThisEditorLaunch; //some crude way to tell how often this has happened before
            public float TimeThisEditorInstanceLaunched;
            public static string GetProjectPath()
            {
                return Application.dataPath;
            }

            public DateTime DateTimeAdded()
            {
                //not sure this belongs here
                return new DateTime((long) TimeThisEditorInstanceLaunched);
            }

            public static long ApproximateTimeThatTheEditorLaunchedAt()
            {
                var dateTime = new DateTime((long) (DateTime.UtcNow.Ticks -
                                                    (TimeSpan.TicksPerSecond * EditorApplication.timeSinceStartup)));
                return dateTime.Ticks;
            }
        }
        //TODO: track how many times we've failed to add a pacakge to avoid infinite loop, and just hard out earlier if we've failed a bunch
        private void AddMissingPackages(IEnumerable<string> missingPackageIdentifiers)
        {
            var serializablePackageTry = new SerializedPackageAttempt()
            {
                PackagesTried = new List<SerializedIndividualPackageAttempt>(),
                ProjectPath = SerializedPackageAttempt.GetProjectPath(),
                TimeThisEditorInstanceLaunched = SerializedPackageAttempt.ApproximateTimeThatTheEditorLaunchedAt(), //time it started should be 
            };
            var serializedPackageTries = new List<SerializedIndividualPackageAttempt>();
            foreach (var missingPackage in missingPackageIdentifiers)
            {
                Client.Add(missingPackage);
                serializedPackageTries.Add(new SerializedIndividualPackageAttempt()
                {
                    PackageTriedToAdd = missingPackage,
                });
                break;//WORKAROUND: can only add one at a time
            }
            
            //TODO: HACK, this should be passed in, but cheating while building this code in
            //should have caller make sure that we're not callign this multiple times
            var savesStore = new PreviousSavesStore();
            var previousAttempt = savesStore.PreviousAttempt();
            if (previousAttempt != null)
            {
                serializablePackageTry.FailuresDuringThisEditorLaunch =
                    previousAttempt.FailuresDuringThisEditorLaunch++;
            }
            savesStore.Set(serializablePackageTry);
#if UNITY_2020_1_OR_NEWER
            Client.Resolve();
#endif
        }

        public class PreviousSavesStore
        {
            string EDITOR_KEY_WITH_PROJECT_PATH = $"{Application.dataPath}_EDITOR_TRIES";
            //string LAST_TIME_EDITOR_STARTED = $"{Application.dataPath}_EDITOR_STARTED_TIME";
            [CanBeNull]
            public SerializedPackageAttempt PreviousAttempt()
            {
                var previousRawValue = EditorPrefs.GetString(EDITOR_KEY_WITH_PROJECT_PATH);
                if(string.IsNullOrEmpty(previousRawValue))
                {
                    return null;
                }

                return JsonUtility.FromJson<SerializedPackageAttempt>(previousRawValue);
            }

            public void Set(SerializedPackageAttempt serializedPackageAttempt)
            {
                EditorPrefs.SetString(EDITOR_KEY_WITH_PROJECT_PATH,JsonUtility.ToJson(serializedPackageAttempt));
            }
        }
        
        private bool HasAllPackages()
        {
            var allPackagesHashSet = MissingPackages();
            bool hasAllPackages = allPackagesHashSet.Count == 0;
            if (!hasAllPackages)
            {
                foreach (var packageString in allPackagesHashSet)
                {
                    Debug.Log($"Did not find package:{packageString}");
                }
            }
            
            return hasAllPackages;
        }

    }

}
