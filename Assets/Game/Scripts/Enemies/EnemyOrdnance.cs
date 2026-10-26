using UnityEngine;

namespace DinoSlug
{
    /// <summary>Enemy rounds and arcing grenades share pause, damage-once and course cleanup.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyOrdnance : MonoBehaviour
    {
        private GameSession session;
        private SpriteRenderer visual;
        private Collider2D player;
        private Sprite[] blastFrames;
        private Vector2 velocity;
        private float age, fuse, blastClock,shellGravity;
        private bool grenade, landed, exploding, charged,rocket;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        public bool IsGrenade => grenade;
        public bool IsRocket => rocket;
        public bool IsExploding => exploding;
        public float FuseRemaining => fuse;
        public void ConfigureAimedRound(Vector2 destination,float seconds)
        {
            velocity=(destination-(Vector2)transform.position)/Mathf.Max(.1f,seconds)+Vector2.right*session.WorldSpeed;
            transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(velocity.y,velocity.x-session.WorldSpeed)*Mathf.Rad2Deg);
        }
        public void ConfigureRocket() { rocket=true;transform.localScale=Vector3.one*.8125f; }
        public void ConfigureVehicleShell(Vector2 speed) { rocket=true;velocity=speed;shellGravity=24;transform.localScale=Vector3.one*.875f; }
        public void ConfigureDroppedBomb()
        {
            float flight=Mathf.Sqrt(2*Mathf.Max(.05f,transform.position.y+3)/14);
            // After landing, the fuse keeps ticking while the ground scrolls.
            // Lead by that travel so the blast reaches the lane at every run speed.
            float landingX=session.Player.transform.position.x+session.WorldSpeed*fuse;
            velocity=new Vector2((landingX-transform.position.x)/flight+session.WorldSpeed,0);
        }

        public void Launch(GameSession owner, bool isGrenade, Sprite image, Sprite[] explosion, Material material)
        {
            session = owner; grenade = isGrenade; blastFrames = explosion;
            visual = GetComponent<SpriteRenderer>(); visual.sprite = image;
            visual.sharedMaterial = material; visual.sortingOrder = 26;
            transform.localScale=Vector3.one*1.25f;
            player = owner.Player.GetComponent<Collider2D>();
            if (grenade)
            {
                transform.localScale=Vector3.one*.5f;
                const float upward = 5.2f, gravity = 14f;
                float height = transform.position.y + 3f;
                float flight = (upward + Mathf.Sqrt(upward*upward+2*gravity*height))/gravity;
                velocity = new Vector2((owner.Player.transform.position.x+2.5f-transform.position.x)/flight+owner.WorldSpeed,upward);
                fuse = .45f;
            }
            else velocity = Vector2.left * 6.5f;
        }

        private void FixedUpdate()
        {
            if (session == null || !session.IsPlaying) return;
            float dt = Time.fixedDeltaTime;
            age += dt;
            if (age > 6 || transform.position.x < -16) { Destroy(gameObject); return; }
            if (exploding)
            {
                transform.position += Vector3.left*session.WorldSpeed*dt;
                blastClock += dt;
                if (blastFrames != null && blastFrames.Length > 0)
                    visual.sprite = blastFrames[Mathf.Min((int)(blastClock*20),blastFrames.Length-1)];
                visual.color = new Color(1,1,1,Mathf.Clamp01((.45f-blastClock)/.15f));
                if (blastClock >= .45f) Destroy(gameObject);
                return;
            }
            Vector2 position = transform.position;
            Vector2 scroll = Vector2.left * session.WorldSpeed * dt;
            if (!grenade)
            {
                velocity.y-=shellGravity*dt;
                Vector2 delta = velocity*dt+scroll;
                // Preserve the former cast's default layers and global trigger-query setting.
                var collisionFilter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
                collisionFilter.SetLayerMask(Physics2D.DefaultRaycastLayers);
                int count = Physics2D.CircleCast(position,.1125f,delta.normalized,collisionFilter,hits,delta.magnitude);
                for (int i=0;i<count;i++)
                    if (hits[i].collider == player)
                    {
                        if (rocket) { transform.position=hits[i].point;DamageOnce();Explode(); }
                        else { DamageOnce();Destroy(gameObject); }
                        return;
                    }
                transform.position = position+delta;
                return;
            }
            if (!landed)
            {
                velocity.y -= 14f*dt;
                position += velocity*dt+scroll;
                transform.Rotate(0,0,-480f*dt);
                if (position.y <= -3f) { position.y=-3f; landed=true; transform.rotation=Quaternion.identity; }
            }
            else
            {
                position += scroll;
                fuse -= dt;
                visual.color = ((int)(fuse*20)%2)==0 ? Color.white : new Color(1,.25f,.15f);
            }
            transform.position = position;
            if (landed && fuse <= 0) Explode();
        }

        private void Explode()
        {
            session.Audio.PlayExplosion(transform.position);
            if (session.Presentation!=null) session.Presentation.Exploded(transform.position);
            exploding = true; visual.color=Color.white;
            transform.localScale=Vector3.one;
            transform.position += Vector3.up*.55f;
            if (blastFrames != null && blastFrames.Length > 0) visual.sprite=blastFrames[0];
            if (player != null && Vector2.Distance(player.ClosestPoint(transform.position),transform.position) <= 1.1f) DamageOnce();
        }

        private void DamageOnce()
        {
            if (charged) return;
            charged=true; session.Hit();
        }
    }
}
