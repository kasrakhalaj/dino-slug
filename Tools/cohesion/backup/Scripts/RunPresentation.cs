using UnityEngine;

namespace DinoSlug
{
    /// <summary>Original scenery, wind and pooled combat debris; the camera stays fixed.</summary>
    public sealed class RunPresentation : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private Transform view;
        [SerializeField] private Material material;
        [SerializeField] private Sprite[] props;
        [SerializeField] private Sprite cloud;
        private const int PoolSize=96;
        private const float Pixel=1f/32f;
        private enum DebrisKind { Spark, Dust, Casing }
        private sealed class Debris
        {
            public SpriteRenderer renderer;
            public Vector2 position,velocity;
            public Color color;
            public float life,duration,spin,angle;
            public DebrisKind kind;
            public bool bounced;
        }
        private sealed class Decoration
        {
            public Transform transform;
            public Vector3 position,initial;
            public float speed,phase;
            public bool wind;
        }
        private readonly System.Random random=new System.Random(4917);
        private readonly Debris[] debris=new Debris[PoolSize];
        private readonly Decoration[] scenery=new Decoration[39];
        private Sprite pixelSprite;
        private Vector3 cameraRest;
        private float clock;
        private int next;
        public int CasingsEmitted { get; private set; }
        public int ImpactCount { get; private set; }
        public int ExplosionCount { get; private set; }
        public int LandingCount { get; private set; }
        public int FootstepCount { get; private set; }
        public int EffectCapacity => PoolSize;
        public int ActiveEffectCount
        { get { int count=0;foreach (var item in debris) if (item!=null && item.life>0) count++;return count; } }
        public Vector3 CameraOffset => view.position-cameraRest;

        public void Configure(GameSession owner,Transform camera,Material shared,Sprite[] sceneryProps,Sprite clouds)
        { session=owner;view=camera;material=shared;props=sceneryProps;cloud=clouds; }

        private void Awake()
        {
            cameraRest=view.position;
            // A utility quad for tiny particles; all character and weapon art stays untouched.
            pixelSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
            pixelSprite.name="Presentation Pixel";
            var effects=new GameObject("Pooled Combat Debris").transform;effects.SetParent(transform,false);
            for (int i=0;i<debris.Length;i++)
            {
                var renderer=PixelRenderer("Debris "+i,effects,28);
                renderer.enabled=false;debris[i]=new Debris { renderer=renderer };
            }
            BuildScenery();
        }

        private float Range(float a,float b) => Mathf.Lerp(a,b,(float)random.NextDouble());
        private SpriteRenderer PixelRenderer(string name,Transform parent,int order)
        {
            var item=new GameObject(name,typeof(SpriteRenderer));item.transform.SetParent(parent,false);
            var renderer=item.GetComponent<SpriteRenderer>();renderer.sprite=pixelSprite;
            renderer.sharedMaterial=material;renderer.sortingOrder=order;return renderer;
        }
        private void BuildScenery()
        {
            var propRoot=new GameObject("Metal Slug Scenery").transform;propRoot.SetParent(transform,false);
            for (int i=0;i<12;i++)
            {
                bool near=i<6;
                var item=new GameObject("Prop "+i,typeof(SpriteRenderer));item.transform.SetParent(propRoot,false);
                var renderer=item.GetComponent<SpriteRenderer>();renderer.sprite=props[i%props.Length];
                renderer.sharedMaterial=material;renderer.sortingOrder=near ? 34 : 9;
                // Opaque sprite props, below or behind the running lane, never tied to jump height.
                renderer.color=near ? Color.white : new Color(.8f,.76f,.8f,1);
                float scale=near ? 1 : .75f;item.transform.localScale=new Vector3(scale,scale,1);
                scenery[i]=new Decoration { transform=item.transform,position=new Vector3(-12+(i%6)*7.5f+(near ? 0 : 2),near ? -5.7f : -3.2f,0),speed=near ? 1.1f : .58f };
                scenery[i].initial=scenery[i].position;item.transform.position=Snap(scenery[i].position);
            }
            var wind=new GameObject("Wind And Sand").transform;wind.SetParent(transform,false);
            for (int i=12;i<36;i++)
            {
                var renderer=PixelRenderer("Wind Sand "+(i-12),wind,8);
                bool streak=i%4==0;
                renderer.transform.localScale=new Vector3(streak ? Range(.35f,.8f) : Range(Pixel,Pixel*2),Pixel,1);
                renderer.color=new Color(.92f,.71f,.49f,streak ? .09f : .24f);
                scenery[i]=new Decoration { transform=renderer.transform,position=new Vector3(Range(-15,25),streak ? Range(-3.12f,-2.95f) : Range(-3f,.5f),0),speed=Range(.25f,.55f),phase=Range(0,6.28f),wind=true };
                scenery[i].initial=scenery[i].position;renderer.transform.position=Snap(scenery[i].position);
            }
            for (int i=36;i<39;i++)
            {
                var item=new GameObject("Drifting Cloud "+(i-36),typeof(SpriteRenderer));item.transform.SetParent(transform,false);
                var renderer=item.GetComponent<SpriteRenderer>();renderer.sprite=cloud;renderer.sharedMaterial=material;
                renderer.sortingOrder=-17;renderer.color=new Color(.85f,.68f,.73f,.08f);
                item.transform.localScale=new Vector3(Range(2,3.5f),Range(.5f,.8f),1);
                scenery[i]=new Decoration { transform=item.transform,position=new Vector3((i-37)*14,Range(2.8f,4.3f),0),speed=.035f,wind=true,phase=i };
                scenery[i].initial=scenery[i].position;item.transform.position=Snap(scenery[i].position);
            }
        }
        private static Vector3 Snap(Vector3 value)
        { return new Vector3(Mathf.Round(value.x/Pixel)*Pixel,Mathf.Round(value.y/Pixel)*Pixel,value.z); }

        private void Update()
        {
            if (session.Model==null || session.Model.State==RunState.Paused) return;
            float dt=Time.deltaTime;
            if (session.IsPlaying || session.Model.State==RunState.MainMenu)
            {
                clock+=dt;
                foreach (var item in scenery)
                {
                    item.position.x-=(session.ScenerySpeed*item.speed+(item.wind ? .3f : 0))*dt;
                    if (item.position.x < -17) item.position.x+=46;
                    var pos=item.position;
                    if (item.wind) pos.y+=Mathf.Sin(clock*.9f+item.phase)*.09f;
                    item.transform.position=Snap(pos);
                }
            }
            foreach (var item in debris)
            {
                if (item.life<=0) continue;
                item.life-=dt;
                if (item.life<=0) { item.renderer.enabled=false;continue; }
                item.velocity.y-=(item.kind==DebrisKind.Dust ? -.12f : 11f)*dt;
                item.position+=item.velocity*dt+Vector2.left*session.WorldSpeed*dt;
                item.angle+=item.spin*dt;
                if (item.kind==DebrisKind.Casing && item.position.y < -3.12f)
                {
                    item.position.y=-3.12f;
                    if (!item.bounced) { item.velocity.y=Mathf.Abs(item.velocity.y)*.35f;item.bounced=true;item.spin*=.4f; }
                    else { item.velocity=Vector2.zero;item.spin=0; }
                }
                item.renderer.transform.position=Snap(item.position);
                item.renderer.transform.rotation=Quaternion.Euler(0,0,Mathf.Round(item.angle/30)*30);
                var color=item.color;color.a*=Mathf.Clamp01(item.life/(item.duration*.35f));item.renderer.color=color;
            }
        }

        // Screen movement was distracting: jumping, firing and explosions never move the camera.
        private void LateUpdate() { view.position=cameraRest; }

        private void Emit(DebrisKind kind,Vector2 position,Vector2 velocity,Color color,float life,Vector2 size)
        {
            var item=debris[next];next=(next+1)%debris.Length;
            item.kind=kind;item.position=position;item.velocity=velocity;item.color=color;
            item.life=item.duration=life;item.bounced=false;item.angle=Range(0,180);item.spin=Range(-500,500);
            item.renderer.transform.position=Snap(position);item.renderer.transform.rotation=Quaternion.Euler(0,0,item.angle);
            item.renderer.transform.localScale=new Vector3(size.x,size.y,1);item.renderer.color=color;
            item.renderer.sortingOrder=kind==DebrisKind.Dust ? 15 : 28;item.renderer.enabled=true;
        }
        public void Fired(Vector3 muzzle,bool shotgun)
        {
            if (!session.IsPlaying) return;
            Emit(DebrisKind.Casing,muzzle+new Vector3(-.85f,.05f,0),new Vector2(Range(-2.8f,-1.2f),Range(2.5f,3.8f)),
                shotgun ? new Color(.83f,.24f,.13f) : new Color(1,.74f,.32f),Range(.85f,1.15f),new Vector2(shotgun ? Pixel*4 : Pixel*3,Pixel*1.2f));
            CasingsEmitted++;
        }
        public void Impact(Vector2 position,bool lethal)
        {
            if (!session.IsPlaying) return;
            ImpactCount++;
            for (int i=0;i<(lethal ? 8 : 4);i++)
                Emit(DebrisKind.Spark,position,new Vector2(Range(-2.8f,2.2f),Range(.6f,3.4f)),new Color(1,Range(.6f,.9f),.35f),Range(.12f,.3f),new Vector2(Pixel*2,Pixel));
            if (lethal) Dust(new Vector2(position.x,-3.1f),8);
        }
        private void Dust(Vector2 position,int count)
        {
            for (int i=0;i<count;i++)
                Emit(DebrisKind.Dust,position+new Vector2(Range(-.2f,.2f),0),new Vector2(Range(-1.8f,1.2f),Range(.25f,.65f)),
                    new Color(.79f,.6f,.47f,.5f),Range(.3f,.6f),Vector2.one*Range(Pixel*2,Pixel*4));
        }
        public void Exploded(Vector3 position)
        {
            if (!session.IsPlaying) return;
            ExplosionCount++;Dust(position,12);
            for (int i=0;i<14;i++)
                Emit(DebrisKind.Spark,position,new Vector2(Range(-4,4),Range(1.5f,5)),new Color(1,.62f,.24f),Range(.18f,.4f),new Vector2(Pixel*3,Pixel));
        }
        public void Landed(Vector3 position)
        {
            if (!session.IsPlaying) return;
            LandingCount++;Dust(position+Vector3.up*.08f,6);
        }
        /// <summary>Two or three tiny sand puffs kicked back from a planted foot.</summary>
        public void Footstep(Vector3 position)
        {
            if (!session.IsPlaying) return;
            FootstepCount++;
            int count=Range(0,1)<.5f ? 2 : 3;
            for (int i=0;i<count;i++)
                Emit(DebrisKind.Dust,(Vector2)position+new Vector2(Range(-.12f,.08f),.04f),new Vector2(Range(-1.6f,-.6f),Range(.2f,.5f)),
                    new Color(.79f,.6f,.47f,.45f),Range(.18f,.32f),Vector2.one*Range(Pixel*2,Pixel*3));
        }
        public void PlayerHit()
        { }
        public void ResetPresentation()
        {
            next=CasingsEmitted=ImpactCount=ExplosionCount=LandingCount=FootstepCount=0;
            clock=0;
            if (view!=null) view.position=cameraRest;
            foreach (var item in debris) if (item!=null) { item.life=0;item.renderer.enabled=false; }
            foreach (var item in scenery) if (item!=null) { item.position=item.initial;item.transform.position=Snap(item.position); }
        }
        private void OnDisable() { if (view!=null) view.position=cameraRest; }
        private void OnDestroy() { if (pixelSprite!=null) Destroy(pixelSprite); }
    }
}
