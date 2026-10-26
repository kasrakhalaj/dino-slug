using UnityEngine;

namespace DinoSlug
{
    public sealed class Coin : Interactable
    {
        private float phase;
        private SpriteRenderer sprite;
        private Vector3 baseScale;
        private void Awake() { sprite = GetComponentInChildren<SpriteRenderer>(); baseScale = sprite.transform.localScale; }
        private void Update()
        {
            if (Session == null || !Session.IsPlaying) return;
            phase += Time.deltaTime * 4f;
            sprite.transform.localScale = new Vector3(baseScale.x * (.82f + .18f * Mathf.Cos(phase)), baseScale.y, baseScale.z);
        }
        protected override void Interact() { Session.CollectCoin(transform.position); Destroy(gameObject); }
    }
}
