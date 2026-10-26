using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoSlugEditor
{
    public static class DinoSlugBranding
    {
        [MenuItem("Dino Slug/Apply Dino Slug Branding")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(DinoSlugBuilder.ScenePath);
            var title = GameObject.Find("Game Canvas/Main Menu/Title");
            if (title == null) throw new InvalidOperationException("The existing menu title was not found.");
            var previous = title.GetComponent<UnityEngine.UI.Image>();
            if (previous != null) { previous.enabled = false; previous.sprite = null; }
            var wordmark = title.transform.Find("Wordmark");
            if (wordmark == null)
            {
                wordmark = new GameObject("Wordmark", typeof(RectTransform)).transform;
                wordmark.SetParent(title.transform, false);
            }
            var rectangle = (RectTransform)wordmark;
            rectangle.anchorMin = Vector2.zero;
            rectangle.anchorMax = Vector2.one;
            rectangle.offsetMin = rectangle.offsetMax = Vector2.zero;
            var text = wordmark.GetComponent<TextMeshProUGUI>() ?? wordmark.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/UI/Fonts/Arcade Bitmap.asset");
            if (text.font == null) throw new InvalidOperationException("The game's pixel font was not found.");
            text.text = "DINO\nSLUG";
            text.fontSize = 172;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.color = Color.white;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(new Color(1, .94f, .72f), new Color(1, .94f, .72f), new Color(.94f, .57f, .17f), new Color(.94f, .57f, .17f));
            PlayerSettings.productName = "Dino Slug";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.hw1.dinoslug");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(DinoSlugBuilder.ScenePath, true) };
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("The renamed scene could not be saved.");
            AssetDatabase.SaveAssets();
            Debug.Log("[DinoSlug] Branding applied; existing game references and artwork retained.");
        }
    }
}
