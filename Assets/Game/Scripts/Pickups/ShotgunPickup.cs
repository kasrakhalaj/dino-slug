using UnityEngine;

namespace DinoSlug
{
    public sealed class ShotgunPickup : Interactable
    {
        protected override void Interact()
        {
            Session.Player.Weapon.CollectShotgun();
            Session.Player.EmitCollect(transform.position);
            Destroy(gameObject);
        }
    }
}
