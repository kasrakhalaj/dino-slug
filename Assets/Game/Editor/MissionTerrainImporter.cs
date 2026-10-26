using System.IO;
using DinoSlug;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace DinoSlugEditor
{
    public static class MissionTerrainImporter
    {
        private const string Art="Assets/Game/Art/MetalSlug/Terrain/";
        [MenuItem("Dino Slug/Use Original Mission 1 Terrain")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var file in Directory.GetFiles(Art,"*.png"))
            {
                var path=file.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                bool ground=path.Contains("_ground"),far=path.Contains("far_jungle"),forest=path.Contains("near_jungle");
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=ground || far || forest || path.Contains("backdrop") || path.Contains("skyline") || path.Contains("station") || path.Contains("factory_wall") || path.Contains("crash_coast") ? 16 : 32;importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
                importer.maxTextureSize=2048;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spriteMeshType=SpriteMeshType.FullRect;
                settings.spritePivot=new Vector2(.5f,ground ? 1 : 0);importer.SetTextureSettings(settings);importer.SaveAndReimport();
            }
            var scene=EditorSceneManager.OpenScene(DinoSlugBuilder.ScenePath);var session=Object.FindAnyObjectByType<GameSession>();
            var material=session.Player.transform.Find("Visual").GetComponent<SpriteRenderer>().sharedMaterial;
            const string terrainMaterialPath="Assets/Game/Materials/TerrainWipe.mat";
            var terrainMaterial=AssetDatabase.LoadAssetAtPath<Material>(terrainMaterialPath);
            if(terrainMaterial==null) { terrainMaterial=new Material(Shader.Find("DinoSlug/Terrain Wipe"));AssetDatabase.CreateAsset(terrainMaterial,terrainMaterialPath); }
            foreach(var name in new[]{"Dusk Sky","Setting Sun","Far Mountains","Near Mountains","Dunes","Ground"})
            { var item=GameObject.Find(name);if(item!=null) { Undo.RecordObject(item,"Retire placeholder scenery");item.SetActive(false);EditorUtility.SetDirty(item); } }
            foreach(var star in Object.FindObjectsByType<Transform>())
                if(star.name.StartsWith("Star ")) { Undo.RecordObject(star.gameObject,"Retire placeholder stars");star.gameObject.SetActive(false); }
            var root=GameObject.Find("Metal Slug Terrain");if(root==null) root=new GameObject("Metal Slug Terrain");
            var oldPanorama=root.transform.Find("Temple Ruins");if(oldPanorama!=null) Object.DestroyImmediate(oldPanorama.gameObject);
            var layers=new[]{Layer(root.transform,session,terrainMaterial,"Far Jungle","ms1_far_jungle",.06f,-20),
                Layer(root.transform,session,terrainMaterial,"Near Jungle","ms1_near_jungle",.18f,-10,18.5f),
                Layer(root.transform,session,terrainMaterial,"Stone Ground","ms1_stone_ground",1,3)};
            session.Presentation.ConfigureTerrain(System.Array.Empty<Sprite>(),layers);EditorUtility.SetDirty(session.Presentation);
            var progression=root.GetComponent<MapProgression>();if(progression==null) progression=root.AddComponent<MapProgression>();
            var maps=new[]{
                new MapProgression.Map { title="TEMPLE OUTSKIRTS",distant=Sprite("ms1_far_jungle"),backdrop=Sprite("ms1_temple_backdrop"),backdropOffset=10,ground=Sprite("ms1_stone_ground") },
                new MapProgression.Map { title="CRASH COAST",distant=Sprite("ms1_far_jungle"),backdrop=Sprite("ms1_crash_coast"),ground=Sprite("ms1_beach_ground"),backdropOffset=14 },
                new MapProgression.Map { title="REBEL HARBOR",distant=Sprite("ms2_harbor_skyline"),backdrop=Sprite("ms2_harbor_station"),backdropOffset=18,ground=Sprite("ms2_rail_ground") },
                new MapProgression.Map { title="IRON WORKS",distant=Sprite("ms2_factory_wall"),ground=Sprite("ms2_factory_ground") }
            };
            progression.Configure(session,layers[0],layers[1],layers[2],maps);session.ConfigureMaps(progression);
            // Serialize the opening environment too, for the scene view and immediate startup.
            foreach(var image in layers[1].GetComponentsInChildren<SpriteRenderer>()) image.sprite=maps[0].backdrop;
            for(int i=0;i<3;i++) layers[1].transform.GetChild(i).position=new Vector3((i-1)*maps[0].backdrop.bounds.size.x+10,-3.2f,0);
            layers[1].Configure(session,.18f,maps[0].backdrop.bounds.size.x,true);
            var hud=Object.FindAnyObjectByType<GameHud>();hud.ConfigureMaps(progression,MapBadge());
            var menu=GameObject.Find("Game Canvas").transform.Find("Main Menu");
            menu.Find("Description").GetComponent<TMP_Text>().text="Ruins. Wrecked coasts. Rebel strongholds.\nChoose Dino and fight through four maps.";
            menu.Find("Edition").GetComponent<TMP_Text>().text="4 MAPS / EVERY 3,000 POINTS";
            EditorUtility.SetDirty(hud);EditorUtility.SetDirty(progression);EditorUtility.SetDirty(session);
            session.Spawner.ConfigureGroundObstacle(Barricade(material));EditorUtility.SetDirty(session.Spawner);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            AssetDatabase.DeleteAsset(Art+"ms1_temple_ruins.png");
            Debug.Log("[Terrain] Original temple, harbor and factory environments installed; next map every 3,000 points.");
        }
        private static TMP_Text MapBadge()
        {
            var parent=GameObject.Find("Game Canvas").transform.Find("In Game HUD").gameObject;
            const string shadePath="Assets/Game/Art/Presentation/HudShade.asset";
            var shade=AssetDatabase.LoadAssetAtPath<Texture2D>(shadePath);
            if(shade==null)
            {
                shade=new Texture2D(1,128,TextureFormat.RGBA32,false);shade.name="HUD Contrast Gradient";shade.wrapMode=TextureWrapMode.Clamp;
                for(int y=0;y<128;y++) shade.SetPixel(0,y,new Color(0,0,0,.78f*Mathf.Pow(y/127f,.7f)));
                shade.Apply();AssetDatabase.CreateAsset(shade,shadePath);
            }
            var shadeChild=parent.transform.Find("HUD Contrast");
            var shadeObject=shadeChild!=null ? shadeChild.gameObject : new GameObject("HUD Contrast",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));
            shadeObject.transform.SetParent(parent.transform,false);shadeObject.transform.SetAsFirstSibling();
            var shadeRect=(RectTransform)shadeObject.transform;shadeRect.anchorMin=new Vector2(0,1);shadeRect.anchorMax=Vector2.one;shadeRect.pivot=new Vector2(.5f,1);
            shadeRect.anchoredPosition=Vector2.zero;shadeRect.sizeDelta=new Vector2(0,310);
            var shadeImage=shadeObject.GetComponent<UnityEngine.UI.RawImage>();shadeImage.texture=shade;shadeImage.raycastTarget=false;
            var found=parent.transform.Find("Map Badge");
            var badge=found!=null ? found.gameObject : new GameObject("Map Badge",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            badge.transform.SetParent(parent.transform,false);
            var rect=(RectTransform)badge.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(0,-207);rect.sizeDelta=new Vector2(460,62);
            var panel=badge.GetComponent<UnityEngine.UI.Image>();panel.color=new Color(.035f,.045f,.045f,.84f);panel.raycastTarget=false;
            var child=badge.transform.Find("Label");
            var label=child!=null ? child.GetComponent<TextMeshProUGUI>() : new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(badge.transform,false);var textRect=label.rectTransform;textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=textRect.offsetMax=Vector2.zero;
            label.font=parent.GetComponentInChildren<TMP_Text>(true).font;label.fontSize=19;label.color=new Color(1,.85f,.57f);label.alignment=TextAlignmentOptions.Center;
            label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.NoWrap;label.text="SECTOR 01 / TEMPLE OUTSKIRTS\n<size=13>NEXT MAP  3,000 POINTS</size>";
            return label;
        }
        private static ParallaxLayer Layer(Transform parent,GameSession owner,Material material,string name,string image,float factor,int order,float offset=0)
        {
            var existing=parent.Find(name);if(existing!=null) Object.DestroyImmediate(existing.gameObject);
            var root=new GameObject(name,typeof(ParallaxLayer));root.transform.SetParent(parent,false);
            var sprite=Sprite(image);float width=sprite.bounds.size.x;
            root.GetComponent<ParallaxLayer>().Configure(owner,factor,width,true);
            for(int i=-1;i<=1;i++)
            {
                var item=new GameObject("Tile "+(i+1),typeof(SpriteRenderer));item.transform.SetParent(root.transform,false);item.transform.position=new Vector3(i*width+offset,-3.2f,0);
                var renderer=item.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;renderer.sortingOrder=order;renderer.color=Color.white;
            }
            return root.GetComponent<ParallaxLayer>();
        }
        private static GroundObstacle Barricade(Material material)
        {
            var root=new GameObject("Sandbag Barricade");var visual=new GameObject("Visual",typeof(SpriteRenderer));visual.transform.SetParent(root.transform,false);
            var image=visual.GetComponent<SpriteRenderer>();image.sprite=Sprite("ms1_sandbag_barricade");
            image.sharedMaterial=material;image.sortingOrder=18;
            var obstacle=root.AddComponent<GroundObstacle>();var body=root.GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.interpolation=RigidbodyInterpolation2D.Interpolate;
            var box=root.GetComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(1.3f,1.1f);box.offset=new Vector2(0,.55f);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Game/Prefabs/SandbagBarricade.prefab");Object.DestroyImmediate(root);return prefab.GetComponent<GroundObstacle>();
        }
        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
    }
}
