using System.IO;
using System.Linq;
using DinoSlug;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoSlugEditor
{
    public static class VehicleImporter
    {
        [System.Serializable] private sealed class Origin { public float x,y; }
        private const string Art="Assets/Game/Art/MetalSlug/Vehicles/";
        [MenuItem("Dino Slug/Install Tank And Helicopter")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var file in Directory.GetFiles(Art,"*.png"))
            {
                string path=file.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                bool tank=path.Contains("tank_") && !path.Contains("shell");bool heli=path.Contains("heli_fly");
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=tank ? 20 : 24;importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spriteMeshType=SpriteMeshType.FullRect;
                settings.spritePivot=new Vector2(.5f,tank ? 16f/128 : .5f);importer.SetTextureSettings(settings);importer.SaveAndReimport();
            }
            var scene=EditorSceneManager.OpenScene(DinoSlugBuilder.ScenePath);var session=Object.FindAnyObjectByType<GameSession>();
            var material=session.Player.transform.Find("Visual").GetComponent<SpriteRenderer>().sharedMaterial;
            var tankPrefab=Create(VehicleEnemy.VehicleKind.Tank,material);var helicopter=Create(VehicleEnemy.VehicleKind.Helicopter,material);
            Undo.RecordObject(session.Spawner,"Wire vehicle encounters");session.Spawner.ConfigureVehicles(tankPrefab,helicopter);EditorUtility.SetDirty(session.Spawner);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("[Vehicles] Original Di-Cokka and R-Shobu installed; infantry scale and user dino artwork preserved.");
        }
        private static VehicleEnemy Create(VehicleEnemy.VehicleKind kind,Material material)
        {
            bool tank=kind==VehicleEnemy.VehicleKind.Tank;string name=tank ? "DiCokkaTank" : "RShobuHelicopter";
            var root=new GameObject(name);var visual=new GameObject("Visual",typeof(SpriteRenderer));visual.transform.SetParent(root.transform,false);
            var image=visual.GetComponent<SpriteRenderer>();image.sharedMaterial=material;image.sortingOrder=21;
            var move=Frames(tank ? "tank_drive" : "heli_fly",tank ? 6 : 28);image.sprite=move[0];
            var alert=new GameObject("Attack Warning",typeof(SpriteRenderer));alert.transform.SetParent(root.transform,false);
            alert.transform.localPosition=new Vector3(0,tank ? 3.4f : 1.8f,0);
            var warning=alert.GetComponent<SpriteRenderer>();warning.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/MetalSlug/Shotgun/rifle_warning.png");
            warning.sharedMaterial=material;warning.sortingOrder=27;warning.enabled=false;
            var explosion=new GameObject("Destruction Blast",typeof(SpriteRenderer));explosion.transform.SetParent(root.transform,false);
            explosion.transform.localPosition=new Vector3(0,tank ? 1.1f : 0,0);explosion.transform.localScale=Vector3.one*1.6f;
            var blast=explosion.GetComponent<SpriteRenderer>();blast.sharedMaterial=material;blast.sortingOrder=28;blast.enabled=false;
            var vehicle=root.AddComponent<VehicleEnemy>();
            var detonation=Enumerable.Range(0,8).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/MetalSlug/Combat/grenade_blast_"+i.ToString("D2")+".png")).ToArray();
            var measured=JsonUtility.FromJson<Origin>(File.ReadAllText("Tools/SourceArt/MetalSlug/Vehicles/tank-origin.json"));
            vehicle.Configure(kind,image,warning,blast,move,tank ? Frames("tank_fire",4) : move,tank ? Frames("tank_death",12) : move,detonation,
                Sprite(tank ? "tank_shell" : "heli_bomb"),AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MetalSlug/shotgun_shot.wav"),
                tank ? new Vector2(measured.x,measured.y) : new Vector2(-.25f,-1.2f));
            var body=root.GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.interpolation=RigidbodyInterpolation2D.Interpolate;
            var hitbox=root.GetComponent<BoxCollider2D>();hitbox.isTrigger=true;
            hitbox.size=tank ? new Vector2(3.2f,2.45f) : new Vector2(2.9f,2.05f);hitbox.offset=tank ? new Vector2(0,1.225f) : new Vector2(0,-.4f);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Game/Prefabs/"+name+".prefab");Object.DestroyImmediate(root);return prefab.GetComponent<VehicleEnemy>();
        }
        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
        private static Sprite[] Frames(string prefix,int count) => Enumerable.Range(0,count).Select(i=>Sprite(prefix+"_"+i.ToString("D2"))).ToArray();
    }
}
