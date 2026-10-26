using System.IO;
using System.Linq;
using DinoSlug;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoSlugEditor
{
    public static class ShotgunImporter
    {
        private const string Art="Assets/Game/Art/MetalSlug/Shotgun/";
        [MenuItem("Dino Slug/Install Shotgun And Fair Rifle Volley")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var file in Directory.GetFiles(Art,"*.png"))
            {
                string path=file.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                bool held=path.EndsWith("shotgun_held.png"),blast=path.Contains("shotgun_blast_");
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=blast ? 24 : 16;importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spriteMeshType=SpriteMeshType.FullRect;
                settings.spritePivot=held ? new Vector2(8f/40f,.5f) : blast ? new Vector2(0,.5f) : new Vector2(.5f,.5f);
                importer.SetTextureSettings(settings);importer.SaveAndReimport();
            }
            foreach (var name in new[]{"shotgun_pickup","shotgun_shot"})
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath("Assets/Game/Audio/MetalSlug/"+name+".wav");
                var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.preloadAudioData=true;
                importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
            }
            var scene=EditorSceneManager.OpenScene(DinoSlugBuilder.ScenePath);
            var session=Object.FindAnyObjectByType<GameSession>();
            var material=session.Player.transform.Find("Visual").GetComponent<SpriteRenderer>().sharedMaterial;
            session.Player.Weapon.ConfigureShotgun(Sprite("shotgun_held"),Enumerable.Range(0,12).Select(i=>Sprite("shotgun_blast_"+i.ToString("D2"))).ToArray(),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MetalSlug/shotgun_pickup.wav"),AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MetalSlug/shotgun_shot.wav"));
            EditorUtility.SetDirty(session.Player.Weapon);
            var root=new GameObject("Shotgun S Pickup");
            var visual=new GameObject("Visual",typeof(SpriteRenderer),typeof(FrameAnimation));visual.transform.SetParent(root.transform,false);
            var renderer=visual.GetComponent<SpriteRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=23;renderer.sprite=Sprite("s_pickup_0");
            visual.GetComponent<FrameAnimation>().Configure(Enumerable.Range(0,4).Select(i=>Sprite("s_pickup_"+i)).ToArray());
            root.AddComponent<ShotgunPickup>();root.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;
            root.GetComponent<Rigidbody2D>().interpolation=RigidbodyInterpolation2D.Interpolate;
            var collider=root.GetComponent<BoxCollider2D>();collider.isTrigger=true;collider.size=new Vector2(1.1f,1.2f);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Game/Prefabs/ShotgunPickup.prefab");Object.DestroyImmediate(root);
            var spawner=Object.FindAnyObjectByType<ObstacleSpawner>();spawner.ConfigureShotgunPickup(prefab.GetComponent<ShotgunPickup>());EditorUtility.SetDirty(spawner);
            var rifle=PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/RifleSoldier.prefab");
            var warning=rifle.transform.Find("Aim Warning");
            if (warning==null) { warning=new GameObject("Aim Warning",typeof(SpriteRenderer)).transform;warning.SetParent(rifle.transform,false); }
            warning.localPosition=new Vector3(0,2.75f,0);
            var alert=warning.GetComponent<SpriteRenderer>();alert.sharedMaterial=material;alert.sortingOrder=26;alert.sprite=Sprite("rifle_warning");alert.enabled=false;
            rifle.GetComponent<SoldierAttack>().ConfigureWarning(alert);
            PrefabUtility.SaveAsPrefabAsset(rifle,"Assets/Game/Prefabs/RifleSoldier.prefab");PrefabUtility.UnloadPrefabContents(rifle);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("[Shotgun] S pickup, 30 shells, held gun, blast, voice and rifle warning installed.");
        }
        private static Sprite Sprite(string name)
        {
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
            if (sprite==null) throw new System.InvalidOperationException("Missing shotgun sprite: "+name);
            return sprite;
        }
    }
}
