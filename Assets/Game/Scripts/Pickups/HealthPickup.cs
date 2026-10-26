using UnityEngine;

namespace DinoSlug
{
    /// <summary>A soldier's medicine drop restores exactly one life, capped by RunModel.</summary>
    public sealed class HealthPickup : Interactable
    {
        private Transform visual;
        private Vector3 rest;
        private float age;
        private void Awake() { visual=transform.Find("Visual");rest=visual.localPosition; }
        private void Update()
        {
            if (Session==null || !Session.IsPlaying) return;
            age+=Time.deltaTime;
            visual.localPosition=rest+Vector3.up*(Mathf.Sin(age*5)*.08f);
        }
        protected override void Interact()
        {
            Session.Heal(transform.position);
            Destroy(gameObject);
        }
    }
}
