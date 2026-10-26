using DinoSlug;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoSlugEditor
{
    public static class PresentationImporter
    {
        [MenuItem("Dino Slug/Install Scenery And Combat Feedback")]
        public static void Apply()
        {
            var scene=EditorSceneManager.OpenScene(DinoSlugBuilder.ScenePath);
            var session=Object.FindAnyObjectByType<GameSession>();
            var presentation=Object.FindAnyObjectByType<RunPresentation>();
            if (presentation==null) presentation=new GameObject("Run Presentation").AddComponent<RunPresentation>();
            Undo.RecordObject(presentation,"Configure original scenery");Undo.RecordObject(session,"Connect presentation");
            const string art="Assets/Game/Art/MetalSlug/Reinforcements/";
            var props=new[]{AssetDatabase.LoadAssetAtPath<Sprite>(art+"sandbags.png"),AssetDatabase.LoadAssetAtPath<Sprite>(art+"barrel.png"),AssetDatabase.LoadAssetAtPath<Sprite>(art+"wooden_barrel.png")};
            foreach(var prop in props) if(prop==null) throw new System.InvalidOperationException("Import reinforcement art before scenery.");
            presentation.Configure(session,Camera.main.transform,AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/WorldSprite.mat"),props,
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/cloud.png"));
            session.ConfigurePresentation(presentation);
            if(GameObject.Find("Metal Slug Terrain")!=null)
                presentation.ConfigureTerrain(System.Array.Empty<Sprite>());
            EditorUtility.SetDirty(presentation);EditorUtility.SetDirty(session);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            AssetDatabase.DeleteAsset("Assets/Game/Art/Presentation/Pixel Rock.asset");
            AssetDatabase.DeleteAsset("Assets/Game/Art/Presentation/Pixel Scrub.asset");
            Debug.Log("[Presentation] Fixed camera, original Metal Slug scenery and combat debris installed.");
        }
    }
}
