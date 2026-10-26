using System;
using System.IO;
using System.Linq;
using DinoSlug;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoSlugEditor
{
    public static class AdventureImporter
    {
        private const string Mobile="Assets/Game/Art/MetalSlug/Mobile/";
        private const float BikerScale=.8625f;
        private static TMP_FontAsset font;
        private static readonly Color Gold=new Color(1,.79f,.32f),Cream=new Color(.96f,.94f,.84f),Ink=new Color(.045f,.06f,.065f);
        [Serializable] private sealed class Document { public Frame[] frames; }
        [Serializable] private sealed class Frame { public string state,file;public int frame,durationMs; }

        [MenuItem("Dino Slug/Install Smooth Maps And Dino Selection")]
        public static void Apply()
        {
            MissionTerrainImporter.Apply();
            foreach(var file in Directory.GetFiles(Mobile,"*.png"))
            {
                var path=file.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);bool bike=path.Contains("biker");
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=bike ? 24 : 32;
                importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;
                settings.spritePivot=new Vector2(.5f,bike ? .1f : .5f);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            }
            const string track="Assets/Game/Audio/MetalSlug/Music/main_theme.ogg";
            var audio=(AudioImporter)AssetImporter.GetAtPath(track);var audioSettings=audio.defaultSampleSettings;
            audioSettings.loadType=AudioClipLoadType.Streaming;audioSettings.compressionFormat=AudioCompressionFormat.Vorbis;audioSettings.quality=.75f;audioSettings.preloadAudioData=true;
            audio.defaultSampleSettings=audioSettings;audio.forceToMono=false;audio.SaveAndReimport();
            var scene=EditorSceneManager.OpenScene(DinoSlugBuilder.ScenePath);var session=UnityEngine.Object.FindAnyObjectByType<GameSession>();
            var material=session.Player.transform.Find("Visual").GetComponent<SpriteRenderer>().sharedMaterial;
            session.Spawner.ConfigureMobileEnemies(CreateMobile(MobileEnemy.EnemyKind.RebelBiker,material),CreateMobile(MobileEnemy.EnemyKind.FlyingTara,material));
            session.Audio.ConfigureMusic(AssetDatabase.LoadAssetAtPath<AudioClip>(track));
            ConfigureSelection(session);ConfigureMusicButton(session);
            EditorUtility.SetDirty(session);EditorUtility.SetDirty(session.Spawner);EditorUtility.SetDirty(session.Audio);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("[Adventure] Four flowing maps, Rebel Biker/Flying Tara, original main theme and animated Dino character selection installed.");
        }
        private static MobileEnemy CreateMobile(MobileEnemy.EnemyKind kind,Material material)
        {
            bool bike=kind==MobileEnemy.EnemyKind.RebelBiker;string name=bike ? "RebelBiker" : "FlyingTara";
            var root=new GameObject(name);if(bike) root.transform.localScale=Vector3.one*BikerScale;
            var visual=new GameObject("Visual",typeof(SpriteRenderer));visual.transform.SetParent(root.transform,false);
            var image=visual.GetComponent<SpriteRenderer>();image.sharedMaterial=material;image.sortingOrder=22;image.flipX=!bike;
            string prefix=bike ? "biker_drive_" : "tara_fly_";
            var frames=Enumerable.Range(0,bike ? 3 : 6).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>(Mobile+prefix+i.ToString("D2")+".png")).ToArray();image.sprite=frames[0];
            var alert=new GameObject("Attack Warning",typeof(SpriteRenderer));alert.transform.SetParent(root.transform,false);alert.transform.localPosition=Vector3.up*(bike ? 2.1f : 1.2f);
            var warning=alert.GetComponent<SpriteRenderer>();warning.sharedMaterial=material;warning.sortingOrder=27;warning.enabled=false;
            warning.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/MetalSlug/Shotgun/rifle_warning.png");
            var blast=Enumerable.Range(0,8).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/MetalSlug/Combat/grenade_blast_"+i.ToString("D2")+".png")).ToArray();
            var enemy=root.AddComponent<MobileEnemy>();enemy.Configure(kind,image,warning,frames,blast,
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/MetalSlug/Combat/enemy_bullet.png"),AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MetalSlug/enemy_rifle_shot.wav"));
            var body=root.GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.interpolation=RigidbodyInterpolation2D.Interpolate;
            var box=root.GetComponent<BoxCollider2D>();box.isTrigger=true;box.size=bike ? new Vector2(2.1f,1.55f) : new Vector2(2.25f,.85f);box.offset=bike ? new Vector2(0,.775f) : Vector2.zero;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Game/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);return prefab.GetComponent<MobileEnemy>();
        }
        [MenuItem("Dino Slug/Update Biker Size")]
        public static void ApplyBikerSize()
        {
            const string path="Assets/Game/Prefabs/RebelBiker.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try { root.transform.localScale=Vector3.one*BikerScale;PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void ConfigureSelection(GameSession session)
        {
            SoldierSelectImporter.Configure(session);
        }
        private static void ConfigureMusicButton(GameSession session)
        {
            font=GameObject.Find("Game Canvas").GetComponentInChildren<TMP_Text>(true).font;
            var hud=GameObject.Find("Game Canvas").transform.Find("In Game HUD");var previous=hud.Find("Music Toggle");if(previous!=null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            Button(hud,"Music Toggle","MUSIC ON / OFF",new Vector2(1090,812),new Vector2(160,32),false,session.Audio.ToggleMusic);
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var root=new GameObject(name,typeof(RectTransform));root.transform.SetParent(parent,false);var r=(RectTransform)root.transform;
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(position.x,-position.y);r.sizeDelta=size;return r;
        }
        private static RectTransform Block(Transform parent,string name,Vector2 position,Vector2 size,Color color)
        { var r=Rect(name,parent,position,size);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return r; }
        private static TMP_Text Label(Transform parent,string name,string text,int size,Vector2 position,Vector2 bounds,Color color)
        {
            var label=Rect(name,parent,position,bounds).gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.fontSize=size;label.text=text;
            label.color=color;label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.NoWrap;return label;
        }
        private static UnityEngine.UI.Button Button(Transform parent,string name,string text,Vector2 position,Vector2 size,bool primary,UnityEngine.Events.UnityAction action)
        {
            var rect=Block(parent,name,position,size,primary ? Gold : new Color(.14f,.19f,.2f));rect.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=rect.GetComponent<UnityEngine.UI.Image>();
            var label=Label(rect,"Label",text,primary ? 22 : 15,Vector2.zero,size,primary ? Ink : Cream);label.alignment=TextAlignmentOptions.Center;label.fontStyle=FontStyles.Bold;
            UnityEventTools.AddPersistentListener(button.onClick,action);return button;
        }
        [MenuItem("Dino Slug/Import Dino Selection Voice")]
        public static void ApplySelectionVoice()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            const string path="Assets/Game/Audio/Player/Dino-Selection.wav";
            if(!File.Exists(path)) throw new InvalidOperationException("Prepare the Dino recording with Tools/process_dino_voice.ps1 first.");
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            var scene=EditorSceneManager.OpenScene(DinoSlugBuilder.ScenePath);var session=UnityEngine.Object.FindAnyObjectByType<GameSession>();
            session.CharacterSelection.ConfigureVoice(AssetDatabase.LoadAssetAtPath<AudioClip>(path));EditorUtility.SetDirty(session.CharacterSelection);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
    }
}
