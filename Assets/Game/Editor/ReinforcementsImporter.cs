using System.IO;
using System.Linq;
using DinoSlug;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoSlugEditor
{
    public static class ReinforcementsImporter
    {
        [System.Serializable] private sealed class Origin { public float x,y; }
        private const string Art="Assets/Game/Art/MetalSlug/Reinforcements/";
        [MenuItem("Dino Slug/Install Reinforcements And Medicine")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var file in Directory.GetFiles(Art,"*.png"))
            {
                var path=file.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                bool soldier=path.Contains("shield_") || path.Contains("bazooka_");
                bool center=path.EndsWith("medicine.png") || path.EndsWith("rocket.png");
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=path.EndsWith("medicine.png") ? 16 : 24;
                importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spriteMeshType=SpriteMeshType.FullRect;
                settings.spritePivot=soldier ? new Vector2(.5f,4f/64) : new Vector2(.5f,center ? .5f : 0);
                importer.SetTextureSettings(settings);importer.SaveAndReimport();
            }
            var scene=EditorSceneManager.OpenScene(DinoSlugBuilder.ScenePath);
            var session=Object.FindAnyObjectByType<GameSession>();
            var material=session.Player.transform.Find("Visual").GetComponent<SpriteRenderer>().sharedMaterial;
            var shield=CreateSoldier("ShieldSoldier",Frames("shield_run",12),Frames("shield_death",11),material,false);
            var bazooka=CreateSoldier("BazookaSoldier",Frames("bazooka_run",11),Frames("bazooka_death",11),material,true);
            var health=new GameObject("Medicine +1 HP");
            var visual=new GameObject("Visual",typeof(SpriteRenderer));visual.transform.SetParent(health.transform,false);
            var image=visual.GetComponent<SpriteRenderer>();image.sprite=Sprite("medicine");image.sharedMaterial=material;image.sortingOrder=23;
            health.AddComponent<HealthPickup>();Physics(health,new Vector2(1.1f,1),Vector2.zero);
            var prefab=PrefabUtility.SaveAsPrefabAsset(health,"Assets/Game/Prefabs/HealthPickup.prefab");Object.DestroyImmediate(health);
            session.Spawner.ConfigureReinforcements(shield,bazooka,prefab.GetComponent<HealthPickup>());
            EditorUtility.SetDirty(session.Spawner);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            PresentationImporter.Apply();
            Debug.Log("[Reinforcements] Shield, bazooka, 30-percent medicine drops and original scenery ready.");
        }
        private static RebelSoldier CreateSoldier(string name,Sprite[] walk,Sprite[] death,Material material,bool rocket)
        {
            var root=new GameObject(name);
            var visual=new GameObject("Visual",typeof(SpriteRenderer),typeof(FrameAnimation));visual.transform.SetParent(root.transform,false);
            var image=visual.GetComponent<SpriteRenderer>();image.sharedMaterial=material;image.sortingOrder=22;image.sprite=walk[0];
            var animation=visual.GetComponent<FrameAnimation>();animation.Configure(walk);
            root.AddComponent<RebelSoldier>().Configure(animation,death);Physics(root,new Vector2(.8f,1.45f),new Vector2(0,.75f));
            if (rocket)
            {
                var attack=root.AddComponent<SoldierAttack>();
                var blast=Enumerable.Range(0,8).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/MetalSlug/Combat/grenade_blast_"+i.ToString("D2")+".png")).ToArray();
                attack.Configure(SoldierAttack.Weapon.Bazooka,animation,image,walk,Frames("bazooka_fire",4),Sprite("rocket"),blast,
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MetalSlug/shotgun_shot.wav"));
                var origin=JsonUtility.FromJson<Origin>(File.ReadAllText("Tools/SourceArt/MetalSlug/Reinforcements/bazooka-origin.json"));
                attack.ConfigureAttackOrigin(new Vector2(origin.x,origin.y),null);
                var warning=new GameObject("Aim Warning",typeof(SpriteRenderer));warning.transform.SetParent(root.transform,false);warning.transform.localPosition=new Vector3(0,2.6f,0);
                var alert=warning.GetComponent<SpriteRenderer>();alert.sharedMaterial=material;alert.sortingOrder=26;alert.enabled=false;
                alert.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/MetalSlug/Shotgun/rifle_warning.png");attack.ConfigureWarning(alert);
            }
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Game/Prefabs/"+name+".prefab");Object.DestroyImmediate(root);return prefab.GetComponent<RebelSoldier>();
        }
        private static void Physics(GameObject root,Vector2 size,Vector2 offset)
        {
            var body=root.GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.interpolation=RigidbodyInterpolation2D.Interpolate;
            var collider=root.GetComponent<BoxCollider2D>();collider.isTrigger=true;collider.size=size;collider.offset=offset;
        }
        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
        private static Sprite[] Frames(string prefix,int count) => Enumerable.Range(0,count).Select(i=>Sprite(prefix+"_"+i.ToString("D2"))).ToArray();
    }
}
