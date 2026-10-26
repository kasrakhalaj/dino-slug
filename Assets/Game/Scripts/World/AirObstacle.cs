namespace DinoSlug
{
    public sealed class AirObstacle : Interactable
    {
        public override bool IsProjectileTarget => true;
        protected override void Interact() { Session.Hit(); }
    }
}
