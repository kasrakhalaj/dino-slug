using System.IO;
using System.Linq;
using DinoSlug;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoSlugEditor
{
    public static class CombatSoldierImporter
    {
        [System.Serializable] private sealed class PixelOrigin { public float x,y; }
        private const string Art="Assets/Game/Art/MetalSlug/Combat/";
        [MenuItem("Dino Slug/Install Rifle And Grenade Soldiers")]
        public static void Apply()
        {
            MyDinoImporter.Apply();
            foreach (string path in Directory.GetFiles("Assets/Game/Audio/MetalSlug/","*.wav"))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                if (importer==null) throw new System.InvalidOperationException("Audio is not imported: "+path);
                var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData=true;
                importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
            }
            foreach (string file in Directory.GetFiles(Art,"*.png"))
            {
                var path=file.Replace('\\','/');
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                bool person=path.Contains("rifle_walk_") || path.Contains("rifle_fire_") || path.Contains("rifle_death_") || path.Contains("grenadier_");
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                bool heldGun=path.EndsWith("hmg_held.png");
                importer.spritePixelsPerUnit=person ? 24 : path.EndsWith("grenade.png") || heldGun ? 16 : 32;
                importer.filterMode=FilterMode.Point; importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteAlignment=(int)SpriteAlignment.Custom;
                settings.spritePivot=person ? new Vector2(.5f,4f/64f) : heldGun ? new Vector2(8f/40f,.5f) : new Vector2(.5f,.5f);
                settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            }
            var scene=EditorSceneManager.OpenScene(DinoSlugBuilder.ScenePath);
            var session=Object.FindAnyObjectByType<GameSession>();
            var deaths=Enumerable.Range(1,6).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MetalSlug/soldier_death_"+i.ToString("D2")+".wav")).ToArray();
            if (deaths.Any(clip=>clip==null)) throw new System.InvalidOperationException("Prepare the six soldier death clips first.");
            session.Audio.ConfigureCombat(session,deaths,AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MetalSlug/enemy_grenade_explosion.wav"));
            EditorUtility.SetDirty(session.Audio);
            var material=session.Player.transform.Find("Visual").GetComponent<SpriteRenderer>().sharedMaterial;
            var gun=session.Player.transform.Find("Visual/Heavy Machine Gun").GetComponent<SpriteRenderer>();
            gun.sprite=Sprite("hmg_held");gun.sortingOrder=24;
            // Keep the trigger beside the chest so the little arms can reach it.
            gun.transform.localPosition=new Vector3(-.12f,.65f,0);gun.transform.localScale=Vector3.one*.75f;
            var barrel=gun.transform.Find("Barrel Tip");
            barrel.localPosition=new Vector3((34f-8f)/16f,4.5f/16f,0);
            barrel.GetComponentInChildren<SpriteRenderer>().sortingOrder=25;
            EditorUtility.SetDirty(gun);EditorUtility.SetDirty(gun.transform);EditorUtility.SetDirty(barrel);
            var blast=Frames("grenade_blast_",8);
            var rifle=Create("Rifle Soldier",SoldierAttack.Weapon.Rifle,Frames("rifle_walk_",12),Frames("rifle_fire_",4),Frames("rifle_death_",11),Sprite("enemy_bullet"),blast,material);
            var grenade=Create("Grenadier",SoldierAttack.Weapon.Grenade,Frames("grenadier_walk_",12),Frames("grenadier_throw_",15),Frames("grenadier_death_",11),Sprite("grenade"),blast,material);
            var spawner=Object.FindAnyObjectByType<ObstacleSpawner>();spawner.ConfigureAttackSoldiers(rifle,grenade);
            EditorUtility.SetDirty(spawner);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("[Combat] Imported user-polished laugh, held HMG and rifle/grenade soldiers.");
            ShotgunImporter.Apply();
        }
        private static RebelSoldier Create(string name,SoldierAttack.Weapon kind,Sprite[] walk,Sprite[] attack,Sprite[] death,Sprite projectile,Sprite[] blast,Material material)
        {
            var root=new GameObject(name);var visual=new GameObject("Visual",typeof(SpriteRenderer),typeof(FrameAnimation));
            visual.transform.SetParent(root.transform,false);
            var renderer=visual.GetComponent<SpriteRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=22;renderer.sprite=walk[0];
            var animation=visual.GetComponent<FrameAnimation>();animation.Configure(walk);
            var soldier=root.AddComponent<RebelSoldier>();soldier.Configure(animation,death);
            var body=root.GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.interpolation=RigidbodyInterpolation2D.Interpolate;
            var collider=root.GetComponent<BoxCollider2D>();collider.isTrigger=true;collider.size=new Vector2(.75f,1.45f);collider.offset=new Vector2(0,.75f);
            var attackBehavior=root.AddComponent<SoldierAttack>();
            attackBehavior.Configure(kind,animation,renderer,walk,attack,projectile,blast,
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MetalSlug/enemy_rifle_shot.wav"));
            SpriteRenderer flash=null;
            // Barrel opening in the aligned shoulder-aim cel; raised throwing
            // hand in the final held-grenade cel immediately before release.
            var measured=JsonUtility.FromJson<PixelOrigin>(File.ReadAllText("Tools/SourceArt/RunAndSoldiers/rifle-origin.json"));
            Vector2 origin=kind==SoldierAttack.Weapon.Rifle ? new Vector2(measured.x,measured.y) : new Vector2(3f/24f,36f/24f);
            if (kind==SoldierAttack.Weapon.Rifle)
            {
                var muzzle=new GameObject("Rifle Muzzle",typeof(SpriteRenderer));muzzle.transform.SetParent(root.transform,false);
                muzzle.transform.localPosition=origin;muzzle.transform.localScale=new Vector3(.5f,.6f,1);
                flash=muzzle.GetComponent<SpriteRenderer>();flash.sprite=projectile;flash.sharedMaterial=material;flash.sortingOrder=26;flash.flipX=true;flash.enabled=false;
            }
            attackBehavior.ConfigureAttackOrigin(origin,flash);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Game/Prefabs/"+(kind==SoldierAttack.Weapon.Rifle ? "RifleSoldier" : "Grenadier")+".prefab");
            Object.DestroyImmediate(root);return prefab.GetComponent<RebelSoldier>();
        }
        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
        private static Sprite[] Frames(string prefix,int count) => Enumerable.Range(0,count).Select(i=>Sprite(prefix+i.ToString("D2"))).ToArray();
        private static Sprite[] RebelFrames(string state,int count) => Enumerable.Range(0,count).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/MetalSlug/Rebel/rebel_"+state+"_"+i.ToString("D2")+".png")).ToArray();
    }
}
