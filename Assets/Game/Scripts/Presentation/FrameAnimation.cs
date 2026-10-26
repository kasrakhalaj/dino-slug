using UnityEngine;

namespace DinoSlug
{
    public sealed class FrameAnimation : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 8;
        private SpriteRenderer sprite;
        private float clock;
        private bool loop = true;
        private bool manual;
        private RebelSoldier owner;
        public void Hold(Sprite pose)
        {
            manual=true;
            if (sprite==null) sprite=GetComponent<SpriteRenderer>();
            sprite.sprite=pose;
        }
        public void Configure(Sprite[] sprites) { frames = sprites; }
        public void PlayLoop(Sprite[] sprites, float fps)
        {
            PlayOnce(sprites,fps);
            loop = true;
        }
        public void PlayOnce(Sprite[] sprites, float fps)
        {
            frames = sprites; framesPerSecond = fps; clock = 0; loop = false; manual=false;
            if (sprite == null) sprite = GetComponent<SpriteRenderer>();
            if (frames.Length > 0) sprite.sprite = frames[0];
        }
        private void Awake() { sprite = GetComponent<SpriteRenderer>(); owner=GetComponentInParent<RebelSoldier>(); }
        private void Update()
        {
            if (manual || (owner!=null && (owner.Owner==null || !owner.Owner.IsPlaying))) return;
            clock += Time.deltaTime;
            if (frames.Length > 0)
            {
                int index = (int)(clock * framesPerSecond);
                sprite.sprite = frames[loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
            }
        }
    }
}
