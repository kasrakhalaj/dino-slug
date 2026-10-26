using UnityEngine;

namespace DinoSlug
{
    public sealed class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private GroundObstacle cactus;
        [SerializeField] private AirObstacle bird;
        [SerializeField] private Coin coin;
        [SerializeField] private HeavyMachineGunPickup machineGun;
        [SerializeField] private ShotgunPickup shotgun;
        [SerializeField] private RebelSoldier rebel;
        [SerializeField] private RebelSoldier rifleSoldier;
        [SerializeField] private RebelSoldier grenadier;
        [SerializeField] private RebelSoldier shieldSoldier;
        [SerializeField] private RebelSoldier bazookaSoldier;
        [SerializeField] private HealthPickup medicine;
        [SerializeField] private VehicleEnemy tank,helicopter;
        [SerializeField] private MobileEnemy biker,flyingTara;
        private MobileEnemy activeAircraft;
        public void ConfigureMobileEnemies(MobileEnemy road,MobileEnemy air) { biker=road;flyingTara=air; }
        private VehicleEnemy activeVehicle;
        [SerializeField, Range(0,1)] private float medicineDropChance=.3f;
        private readonly System.Random lootRandom=new System.Random();
        private float weaponDistance;
        private bool firstWeapon;
        private bool nextIsShotgun;
        private float remainingDistance;
        private int sequence;
        private int encountersSinceVehicle;
        private int vehicleSequence;
        private const float InfantryGap=1.6f;
        public Vector2 EncounterSpacing => new Vector2(14-session.Model.ThreatTier,18-session.Model.ThreatTier);
        public int VehicleInterval => Mathf.Max(6,9-session.Model.ThreatTier*2);
        public void Configure(GameSession owner, GroundObstacle groundPrefab, AirObstacle airPrefab, Coin coinPrefab)
        { session = owner; cactus = groundPrefab; bird = airPrefab; coin = coinPrefab; }
        public void ConfigureWeaponPickup(HeavyMachineGunPickup prefab) { machineGun = prefab; }
        public void ConfigureGroundObstacle(GroundObstacle prefab) { cactus= prefab; }
        public void ConfigureShotgunPickup(ShotgunPickup prefab) { shotgun = prefab; }
        public void ConfigureRebelSoldier(RebelSoldier prefab) { rebel = prefab; }
        public void ConfigureAttackSoldiers(RebelSoldier rifle, RebelSoldier grenade) { rifleSoldier=rifle; grenadier=grenade; }
        public void ConfigureReinforcements(RebelSoldier shield, RebelSoldier bazooka, HealthPickup health)
        { shieldSoldier=shield;bazookaSoldier=bazooka;medicine=health; }
        public void ConfigureVehicles(VehicleEnemy ground,VehicleEnemy air) { tank=ground;helicopter=air; }
        public HealthPickup TryDropMedicine(Vector3 defeatedPosition)
        {
            if (!session.IsPlaying || medicine==null || session.Model.Lives>=RunModel.MaximumLives || lootRandom.NextDouble()>=medicineDropChance) return null;
            var drop=Instantiate(medicine,new Vector3(defeatedPosition.x,-2.65f,0),Quaternion.identity,transform);
            drop.Initialize(session);
            return drop;
        }

        public void ResetCourse()
        {
            foreach (Transform child in transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            remainingDistance = 3f;
            sequence = 0;
            encountersSinceVehicle=0;vehicleSequence=0;
            weaponDistance = 0;
            firstWeapon = true;
            nextIsShotgun = false;
            activeVehicle=null;
            activeAircraft=null;
        }

        private void Update()
        {
            if (!session.IsPlaying) return;
            // Give larger encounters their own clear stretch of the course.
            if(activeVehicle!=null) return;
            if(activeAircraft!=null && activeAircraft.EncounterActive) return;
            if (machineGun != null)
            {
                weaponDistance -= session.WorldSpeed * Time.deltaTime;
                if (weaponDistance <= 0)
                {
                    Interactable pickup=nextIsShotgun && shotgun!=null ? (Interactable)shotgun : machineGun;
                    Spawn(pickup, new Vector3(firstWeapon ? 4f : 13f, -2.5f, 0));
                    weaponDistance=firstWeapon ? 32f : Random.Range(75f-session.Model.ThreatTier*12.5f,95f-session.Model.ThreatTier*12.5f);
                    firstWeapon = false;
                    nextIsShotgun=!nextIsShotgun;
                }
            }
            remainingDistance -= session.WorldSpeed * Time.deltaTime;
            if (remainingDistance > 0) return;
            if(encountersSinceVehicle>=VehicleInterval-1 && tank!=null && helicopter!=null)
            {
                // Finish the preceding wave and its ordnance before bringing in armour.
                if(LaneHasThreats()) return;
                bool flying=vehicleSequence%2==1;
                activeVehicle=Instantiate(flying ? helicopter : tank,new Vector3(15,flying ? -.25f : -3.2f,0),Quaternion.identity,transform);
                activeVehicle.Initialize(session);
                Spawn(machineGun,new Vector3(session.Player.transform.position.x+5,-2.5f,0));
                weaponDistance=Mathf.Max(weaponDistance,35);
                remainingDistance=18;encountersSinceVehicle=0;vehicleSequence++;return;
            }
            if(sequence%12==4 && biker!=null)
            { Spawn(biker,new Vector3(13,-3.2f,0));remainingDistance=20;sequence++;encountersSinceVehicle++;return; }
            if(sequence%12==10 && flyingTara!=null)
            {
                activeAircraft=Instantiate(flyingTara,new Vector3(14,.35f,0),Quaternion.identity,transform);activeAircraft.Initialize(session);
                remainingDistance=20;sequence++;encountersSinceVehicle++;return;
            }
            int encounter=sequence%8;
            // More armed infantry later, while preserving a full jump/recovery gap.
            if(session.Model.ThreatTier>0 && encounter==0) encounter=1;
            if(session.Model.ThreatTier>0 && encounter==4) encounter=3;
            if(session.Model.ThreatTier>1 && encounter==2) encounter=6;
            bool airborne = encounter == 2;
            int infantryCount=1;
            if (airborne) Spawn(bird, new Vector3(13f, -.85f, 0));
            else if (encounter == 1 && rifleSoldier != null) infantryCount=SpawnInfantry(rifleSoldier);
            else if (encounter == 3 && grenadier != null) infantryCount=SpawnInfantry(grenadier);
            else if (encounter == 5 && rebel != null) infantryCount=SpawnInfantry(rebel);
            else if (encounter == 6 && shieldSoldier != null) infantryCount=SpawnInfantry(shieldSoldier);
            else if (encounter == 7 && bazookaSoldier != null) infantryCount=SpawnInfantry(bazookaSoldier);
            else Spawn(cactus, new Vector3(13f, -3.2f, 0));
            // Clear separation remains traversable at maximum speed and during a short dash.
            var spacing=EncounterSpacing;remainingDistance = Random.Range(spacing.x,spacing.y)+(infantryCount-1)*InfantryGap;
            for (int i = 0; i < 3; i++)
                Spawn(coin, new Vector3(17f + i * 1.1f, airborne ? -2.3f : -1.1f + Mathf.Sin(i * Mathf.PI / 2) * .35f, 0));
            sequence++;
            encountersSinceVehicle++;
        }

        private int SpawnInfantry(RebelSoldier leader)
        {
            int count=session.Model.ThreatTier==0 || rebel==null ? 1 :
                session.Model.ThreatTier==1 ? 2 : 2+(sequence/2)%2;
            RebelSoldier commander=null;
            for(int i=0;i<count;i++)
            {
                // One warned attacker leads; close supports keep the wave jumpable.
                var prefab=i==0 ? leader : i==2 && shieldSoldier!=null ? shieldSoldier : rebel;
                var member=(RebelSoldier)Spawn(prefab,new Vector3(13+i*InfantryGap,-3.2f,0));
                if(i==0) commander=member;else member.FollowFormation(commander);
            }
            return count;
        }

        private bool LaneHasThreats()
        {
            foreach(Transform child in transform)
            {
                if(!child.gameObject.activeInHierarchy) continue;
                if(child.TryGetComponent<EnemyOrdnance>(out _)) return true;
                if(child.TryGetComponent<Interactable>(out var item) && item.IsProjectileTarget && item.HitPoints>0) return true;
            }
            return false;
        }

        private Interactable Spawn(Interactable prefab, Vector3 position)
        {
            var item = Instantiate(prefab, position, Quaternion.identity, transform);
            item.Initialize(session);
            return item;
        }
    }
}
