using UnityEngine;

namespace DinoSlug
{
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private float factor = .2f;
        [SerializeField] private float tileWidth = 32f;
        [SerializeField] private bool pixelSnap;
        private Transform[] tiles;
        private Vector3[] initial;
        private float phase;
        public void Configure(GameSession owner, float speedFactor, float width,bool snap=false) { session = owner; factor = speedFactor; tileWidth = width;pixelSnap=snap; }
        private void Awake()
        {
            CacheTiles();
        }
        private void CacheTiles()
        {
            if(tiles!=null) return;
            tiles=new Transform[transform.childCount];initial=new Vector3[tiles.Length];
            for(int i=0;i<tiles.Length;i++) { tiles[i]=transform.GetChild(i);initial[i]=tiles[i].position; }
        }
        public void SetTerrainSprite(Sprite sprite,float horizontalOffset=0)
        {
            CacheTiles();
            if(sprite!=null) tileWidth=sprite.bounds.size.x;
            phase=0;
            for(int i=0;i<tiles.Length;i++)
            {
                var image=tiles[i].GetComponent<SpriteRenderer>();image.sprite=sprite;image.enabled=sprite!=null;
                initial[i]=new Vector3((i-1)*tileWidth+horizontalOffset,initial[i].y,initial[i].z);
            }
            ApplyScroll();
        }
        private void Update()
        {
            phase=(phase+session.ScenerySpeed*factor*Time.deltaTime)%(tileWidth*3);
            ApplyScroll();
        }
        private void ApplyScroll()
        {
            float shift=pixelSnap ? Mathf.Round(phase*32)/32 : phase;
            for(int i=0;i<tiles.Length;i++)
            {
                var position=initial[i]+Vector3.left*shift;
                if(position.x < -tileWidth) position.x+=tileWidth*3;
                tiles[i].position=position;
            }
        }
        public void ResetScroll() { phase=0;if(tiles!=null) ApplyScroll(); }
    }
}
