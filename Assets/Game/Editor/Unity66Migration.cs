using System;
using System.IO;
using System.Linq;
using DinoSlug;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoSlugEditor
{
    /// <summary>Explicit migration entry point; never runs during ordinary editor startup.</summary>
    public static class Unity66Migration
    {
        [Serializable] private sealed class Report
        {
            public string editor,scene,buildTarget,apiCompatibility;
            public string[] packages;
            public int maps;
            public bool selectionVoice,sceneSaved;
        }

        public static void ImportAndReport()
        {
            if(!Application.unityVersion.StartsWith("6000.6."))
                throw new InvalidOperationException("This migration entry point requires Unity 6.6.");
            var scene=EditorSceneManager.OpenScene(DinoSlugBuilder.ScenePath);
            var session=UnityEngine.Object.FindAnyObjectByType<GameSession>();
            if(session==null || session.Player==null || session.Spawner==null || session.Audio==null ||
               session.CharacterSelection==null || session.Maps==null || session.Maps.MapCount!=4)
                throw new InvalidOperationException("Migration validation found missing gameplay references.");
            if(!EditorBuildSettings.scenes.Any(item=>item.enabled && item.path==DinoSlugBuilder.ScenePath))
                throw new InvalidOperationException("The game scene is missing from the enabled build scenes.");
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved=EditorSceneManager.SaveScene(scene);
            if(!saved) throw new InvalidOperationException("Could not save the migrated gameplay scene.");
            var report=new Report {
                editor=Application.unityVersion,scene=scene.path,buildTarget=EditorUserBuildSettings.activeBuildTarget.ToString(),
                apiCompatibility=PlayerSettings.GetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone).ToString(),
                packages=UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                    .OrderBy(package=>package.name).Select(package=>package.name+"@"+package.version).ToArray(),
                maps=session.Maps.MapCount,selectionVoice=session.CharacterSelection.HasDinoVoice,sceneSaved=saved
            };
            Directory.CreateDirectory("Docs/Unity66-Review");
            File.WriteAllText("Docs/Unity66-Review/import-report.json",JsonUtility.ToJson(report,true));
            Debug.Log("[Unity66] Imported and saved "+report.scene+" with Unity "+report.editor);
        }
    }
}
