using System;
using System.IO;
using System.Linq;
using DinoSlug;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace DinoSlugEditor
{
    /// <summary>One portrait in the original arcade selection layout.</summary>
    public static class SoldierSelectImporter
    {
        private const string Art="Assets/Game/UI/SoldierSelect/";
        [Serializable] private sealed class Document { public Frame[] frames; }
        [Serializable] private sealed class Frame { public string state,file;public int frame,durationMs; }
        [MenuItem("Dino Slug/Use Classic Dino Soldier Select")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var scene=EditorSceneManager.OpenScene(DinoSlugBuilder.ScenePath);
            Configure(UnityEngine.Object.FindAnyObjectByType<GameSession>());
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("[SoldierSelect] Original metal heading/frame, single Dino portrait, P1 marker and pixel controls installed.");
        }
        public static void Configure(GameSession session)
        {
            foreach(var file in Directory.GetFiles(Art,"*.png"))
            {
                var path=file.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=1;
                importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            }
            var font=PixelFont();var canvas=GameObject.Find("Game Canvas").transform;
            var found=canvas.Find("Character Select");
            var panel=found!=null ? found.gameObject : new GameObject("Character Select",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(CharacterSelection));
            panel.transform.SetParent(canvas,false);panel.SetActive(false);
            // Keep the serialized panel/controller; replace only its old presentation children.
            for(int i=panel.transform.childCount-1;i>=0;i--) UnityEngine.Object.DestroyImmediate(panel.transform.GetChild(i).gameObject);
            var full=(RectTransform)panel.transform;full.anchorMin=Vector2.zero;full.anchorMax=Vector2.one;full.offsetMin=full.offsetMax=Vector2.zero;
            panel.GetComponent<UnityEngine.UI.Image>().color=Color.black;
            var layout=new GameObject("Arcade Layout",typeof(RectTransform)).GetComponent<RectTransform>();layout.SetParent(panel.transform,false);
            layout.anchorMin=layout.anchorMax=layout.pivot=new Vector2(.5f,.5f);layout.anchoredPosition=Vector2.zero;layout.sizeDelta=new Vector2(320,224);
            Picture(layout,"Metal Shell","single_dino_shell",Vector2.zero,new Vector2(320,224));
            Picture(layout,"Soldier Select Heading","soldier_select_heading",new Vector2(8,8),new Vector2(304,34));
            Picture(layout,"P1 Marker","player_one_marker",new Vector2(139,42),new Vector2(37,26));
            var selector=panel.GetComponent<CharacterSelection>();
            var frame=Picture(layout,"Deploy Dino","portrait_frame",new Vector2(104,66),new Vector2(112,132));frame.raycastTarget=true;
            var confirm=frame.gameObject.AddComponent<UnityEngine.UI.Button>();confirm.targetGraphic=frame;
            var colors=confirm.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.85f,.35f);colors.selectedColor=colors.highlightedColor;colors.disabledColor=Color.white;confirm.colors=colors;
            UnityEventTools.AddPersistentListener(confirm.onClick,selector.ConfirmDino);
            var preview=Picture(frame.transform,"Dino Portrait",null,new Vector2(4,4),new Vector2(104,124));preview.preserveAspect=true;
            Picture(layout,"Dino Nameplate","dino_nameplate",new Vector2(104,198),new Vector2(112,22));
            Text(layout,font,"Dino Name","DINO",12,new Vector2(104,199),new Vector2(112,20),new Color(.95f,.86f,.63f));
            Text(layout,font,"Select Hint","ENTER\nSELECT",7,new Vector2(25,108),new Vector2(70,32),new Color(1,.8f,.15f));
            var status=Text(layout,font,"Status","READY",7,new Vector2(22,151),new Vector2(76,16),Color.white);
            var backFrame=Picture(layout,"Back","dino_nameplate",new Vector2(235,197),new Vector2(64,20));backFrame.raycastTarget=true;
            var back=backFrame.gameObject.AddComponent<UnityEngine.UI.Button>();back.targetGraphic=backFrame;
            UnityEventTools.AddPersistentListener(back.onClick,session.MainMenu);
            Text(backFrame.transform,font,"Label","BACK",7,Vector2.zero,new Vector2(64,20),new Color(.95f,.86f,.63f));
            Text(layout,font,"Back Hint","ESC",7,new Vector2(239,179),new Vector2(56,16),Color.white);
            var document=JsonUtility.FromJson<Document>(File.ReadAllText("Tools/SourceArt/MyDinoAnimation/animation.json"));
            var run=document.frames.Where(f=>f.state=="Run").OrderBy(f=>f.frame).ToArray();var laugh=document.frames.Where(f=>f.state=="Laugh").OrderBy(f=>f.frame).ToArray();
            Func<Frame,Sprite> portrait=f=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+Path.GetFileNameWithoutExtension(f.file)+"_portrait.png");
            selector.Configure(session,preview,confirm,status,run.Select(portrait).ToArray(),laugh.Select(portrait).ToArray(),laugh.Select(f=>f.durationMs/1000f).ToArray());
            selector.ConfigureLayout(layout);selector.ConfigureVoice(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Player/Dino-Selection.wav"));
            session.ConfigureCharacterSelection(selector);var hud=UnityEngine.Object.FindAnyObjectByType<GameHud>();hud.ConfigureSelection(panel);
            var start=canvas.Find("Main Menu/Start Run").GetComponent<UnityEngine.UI.Button>();
            while(start.onClick.GetPersistentEventCount()>0) UnityEventTools.RemovePersistentListener(start.onClick,0);
            UnityEventTools.AddPersistentListener(start.onClick,session.OpenCharacterSelect);start.GetComponentInChildren<TMP_Text>().text="SOLDIER SELECT     >";
            EditorUtility.SetDirty(selector);EditorUtility.SetDirty(session);EditorUtility.SetDirty(hud);EditorUtility.SetDirty(start);
        }
        private static TMP_FontAsset PixelFont()
        {
            const string path="Assets/Game/UI/Fonts/Arcade Bitmap.asset";
            var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(asset!=null) return asset;
            var original=AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/UI/Fonts/PressStart2P-Regular.ttf");
            asset=TMP_FontAsset.CreateFontAsset(original,8,1,GlyphRenderMode.RASTER_HINTED,256,128,AtlasPopulationMode.Dynamic,false);asset.name="Arcade Bitmap";
            asset.TryAddCharacters(new string(Enumerable.Range(32,95).Select(i=>(char)i).ToArray()));asset.atlasPopulationMode=AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(asset,path);asset.material.name="Arcade Bitmap Material";AssetDatabase.AddObjectToAsset(asset.material,asset);
            foreach(var texture in asset.atlasTextures) { texture.name="Arcade Bitmap Atlas";texture.filterMode=FilterMode.Point;AssetDatabase.AddObjectToAsset(texture,asset); }
            EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();return asset;
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(position.x,-position.y);r.sizeDelta=size;return r;
        }
        private static UnityEngine.UI.Image Picture(Transform parent,string name,string sprite,Vector2 position,Vector2 size)
        {
            var image=Rect(name,parent,position,size).gameObject.AddComponent<UnityEngine.UI.Image>();image.raycastTarget=false;
            if(sprite!=null) image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+sprite+".png");return image;
        }
        private static TMP_Text Text(Transform parent,TMP_FontAsset font,string name,string text,int size,Vector2 position,Vector2 bounds,Color color)
        {
            var label=Rect(name,parent,position,bounds).gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.fontSize=size;label.text=text;
            label.color=color;label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.NoWrap;label.alignment=TextAlignmentOptions.Center;return label;
        }
    }
}
