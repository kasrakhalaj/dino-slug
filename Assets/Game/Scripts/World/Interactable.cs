using UnityEngine;

namespace DinoSlug
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public abstract class Interactable : MonoBehaviour
    {
        protected GameSession Session { get; private set; }
        private Rigidbody2D body;
        private bool consumed;
        private SpriteRenderer hitVisual;
        private Color originalColor;
        private float hitFlash;
        private Vector3 visualScale = Vector3.one;
        public virtual bool IsProjectileTarget => false;
        protected virtual float ApproachSpeed => 0;
        protected virtual int InitialHitPoints => 3;
        public int HitPoints { get; private set; } = 3;

        public bool TakeProjectileHit()
        { return TakeProjectileHit((Vector2)transform.position+Vector2.up*.8f); }

        public bool TakeProjectileHit(Vector2 position)
        {
            if (!IsProjectileTarget || consumed || Session == null || !Session.IsPlaying) return false;
            HitPoints--;
            if (Session.Presentation!=null) Session.Presentation.Impact(position,HitPoints==0);
            if (HitPoints > 0)
            {
                hitFlash = .075f;
                if (hitVisual != null)
                {
                    hitVisual.color = new Color(1f,.45f,.25f);
                    // Squash toward the shot so every hit visibly lands.
                    hitVisual.transform.localScale = new Vector3(visualScale.x * 1.1f, visualScale.y * .9f, visualScale.z);
                }
            }
            else
            {
                consumed = true;
                GetComponent<BoxCollider2D>().enabled = false;
                if (hitVisual != null) { hitVisual.color = originalColor; hitVisual.transform.localScale = visualScale; }
                Session.HitStop(.05f);                       // a short freeze sells the kill
                OnProjectileDestroyed();
            }
            return true;
        }

        protected virtual void OnProjectileDestroyed() { gameObject.SetActive(false); Destroy(gameObject); }

        public void Initialize(GameSession session)
        {
            Session = session;
            body = GetComponent<Rigidbody2D>();
            HitPoints = InitialHitPoints;
            consumed = false;
            hitFlash = 0;
            hitVisual = GetComponentInChildren<SpriteRenderer>();
            if (hitVisual != null) { originalColor = hitVisual.color; visualScale = hitVisual.transform.localScale; }
        }

        private void FixedUpdate()
        {
            if (Session == null || !Session.IsPlaying) return;
            if (hitFlash > 0)
            {
                hitFlash -= Time.fixedDeltaTime;
                if (hitFlash <= 0 && hitVisual != null) { hitVisual.color = originalColor; hitVisual.transform.localScale = visualScale; }
            }
            body.MovePosition(body.position + Vector2.left * ((Session.WorldSpeed + ApproachSpeed) * Time.fixedDeltaTime));
            if (body.position.x < -15f) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || Session == null || !Session.IsPlaying || !other.TryGetComponent<PlayerController>(out _)) return;
            consumed = true; // Several colliders or repeated callbacks can never charge twice.
            GetComponent<BoxCollider2D>().enabled = false;
            Interact();
        }

        protected abstract void Interact();
    }
}
