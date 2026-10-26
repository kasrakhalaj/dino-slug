using System;
using UnityEngine;

namespace DinoSlug
{
    /// <summary>Switches a complete authored environment at each score milestone.</summary>
    public sealed class MapProgression : MonoBehaviour
    {
        [Serializable] public sealed class Map
        {
            public string title;
            public Sprite distant, backdrop, ground;
            public float backdropOffset;
            public Sprite[] props = Array.Empty<Sprite>();
        }
        [SerializeField] private GameSession session;
        [SerializeField] private ParallaxLayer distant,backdrop,ground;
        [SerializeField] private Map[] maps;
        private int sector=-1;
        private readonly ParallaxLayer[] outgoing=new ParallaxLayer[3];
        private SpriteRenderer[] incomingImages,outgoingImages;
        private MaterialPropertyBlock wipe;
        private static readonly int Side=Shader.PropertyToID("_WipeSide"),Boundary=Shader.PropertyToID("_BoundaryX");
        public bool IsTransitioning { get; private set; }
        public float BoundaryX { get; private set; }
        public float TransitionProgress => IsTransitioning ? Mathf.Clamp01((14-BoundaryX)/29) : 1;
        public int ActiveSector => sector;
        public int MapCount => maps.Length;
        public string ActiveMapName => NameForSector(Mathf.Max(0,sector));
        public string NameForSector(int index) => maps[index%maps.Length].title;

        public void Configure(GameSession owner,ParallaxLayer far,ParallaxLayer near,ParallaxLayer floor,Map[] stages)
        { session=owner;distant=far;backdrop=near;ground=floor;maps=stages; }

        private void Awake() { wipe=new MaterialPropertyBlock(); }

        private void Start() { ResetMap(); }
        private void LateUpdate()
        {
            if(session.Model==null) return;
            int desired=session.Model.State==RunState.MainMenu || session.Model.State==RunState.SelectingCharacter ? 0 : session.Model.SectorIndex;
            if(desired!=sector)
            {
                if(session.IsPlaying && sector>=0 && desired>sector) BeginTransition(desired);
                else { FinishTransition();Apply(desired); }
            }
            if(IsTransitioning && session.IsPlaying)
            {
                BoundaryX-=session.WorldSpeed*Time.deltaTime;
                SetMask(incomingImages,1);SetMask(outgoingImages,-1);
                if(BoundaryX<=-15) FinishTransition();
            }
        }
        public void ResetMap() { FinishTransition();Apply(0); }
        private void BeginTransition(int index)
        {
            FinishTransition();
            var layers=new[]{distant,backdrop,ground};
            for(int i=0;i<3;i++)
            {
                outgoing[i]=Instantiate(layers[i],transform);outgoing[i].name="Outgoing "+layers[i].name;
            }
            outgoingImages=GetOutgoingImages();
            Apply(index);incomingImages=GetIncomingImages();
            BoundaryX=14;IsTransitioning=true;
            SetMask(incomingImages,1);SetMask(outgoingImages,-1);
        }
        private SpriteRenderer[] GetIncomingImages()
        {
            var result=new SpriteRenderer[9];int cursor=0;
            foreach(var layer in new[]{distant,backdrop,ground}) foreach(var image in layer.GetComponentsInChildren<SpriteRenderer>()) result[cursor++]=image;
            return result;
        }
        private SpriteRenderer[] GetOutgoingImages()
        {
            var result=new SpriteRenderer[9];int cursor=0;
            foreach(var layer in outgoing) foreach(var image in layer.GetComponentsInChildren<SpriteRenderer>()) result[cursor++]=image;
            return result;
        }
        private void SetMask(SpriteRenderer[] images,float side)
        {
            if(images==null) return;
            wipe.Clear();wipe.SetFloat(Side,side);wipe.SetFloat(Boundary,BoundaryX);
            foreach(var image in images) if(image!=null) image.SetPropertyBlock(wipe);
        }
        private void FinishTransition()
        {
            IsTransitioning=false;SetMask(incomingImages,0);incomingImages=null;outgoingImages=null;
            for(int i=0;i<outgoing.Length;i++) if(outgoing[i]!=null)
            { outgoing[i].gameObject.SetActive(false);Destroy(outgoing[i].gameObject);outgoing[i]=null; }
        }
        private void Apply(int index)
        {
            var map=maps[index%maps.Length];
            distant.SetTerrainSprite(map.distant);
            backdrop.SetTerrainSprite(map.backdrop,map.backdropOffset);
            ground.SetTerrainSprite(map.ground);
            session.Presentation.SetMapProps(map.props);
            sector=index;
        }
        private void OnDestroy() { FinishTransition(); }
    }
}
