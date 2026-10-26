using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace DinoSlugEditor
{
    /// <summary>One-time removal of template packages unused by this small offline runner.</summary>
    public static class TrimTemplatePackages
    {
        private static AddAndRemoveRequest request;
        private static double deadline;

        // Run in batch mode without -quit; the async package request owns the exit.
        public static void Run()
        {
            request = Client.AddAndRemove(
                new[] { "com.unity.render-pipelines.universal@17.5.0" },
                new[] { "com.unity.2d.animation", "com.unity.2d.aseprite", "com.unity.2d.psdimporter",
                    "com.unity.2d.spriteshape", "com.unity.2d.tilemap", "com.unity.2d.tilemap.extras",
                    "com.unity.2d.tooling", "com.unity.ai.navigation", "com.unity.analytics", "com.unity.purchasing",
                    "com.unity.multiplayer.center", "com.unity.collab-proxy", "com.unity.timeline",
                    "com.unity.visualscripting", "com.unity.xr.legacyinputhelpers" });
            deadline = EditorApplication.timeSinceStartup + 600;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (!request.IsCompleted && EditorApplication.timeSinceStartup < deadline) return;
            EditorApplication.update -= Poll;
            if (!request.IsCompleted || request.Status != StatusCode.Success)
            {
                Debug.LogError("[DinoSlug] Template cleanup failed: " + request.Error?.message);
                EditorApplication.Exit(1);
                return;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[DinoSlug] Kept only rendering, sprites, input, UI, tests, and IDE support.");
            EditorApplication.Exit(0);
        }
    }
}
