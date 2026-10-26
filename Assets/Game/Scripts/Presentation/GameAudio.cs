using UnityEngine;

namespace DinoSlug
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private GameSession session;
        [SerializeField] private AudioClip[] soldierDeathClips;
        [SerializeField] private AudioClip grenadeExplosion;
        private AudioSource source;
        private readonly AudioSource[] combatVoices=new AudioSource[8];
        private readonly System.Random deathRandom=new System.Random();
        private int nextVoice,previousDeath=-1;
        private bool combatPaused;
        [SerializeField] private AudioClip musicClip;
        private AudioSource music,selection;
        private bool musicPaused;
        public bool MusicEnabled { get; private set; } = true;
        public AudioSource MusicSource => music;
        public bool SelectionVoicePlaying => selection!=null && selection.isPlaying;
        public void ConfigureMusic(AudioClip clip) { musicClip=clip; }
        public void ToggleMusic() { MusicEnabled=!MusicEnabled;if(music!=null) music.mute=!MusicEnabled; }
        public void PlaySelection(AudioClip clip)
        {
            selection.Stop();
            if(clip==null) { Play("click");return; }
            selection.clip=clip;selection.volume=.85f;selection.Play();
        }
        public void StopSelection() { if(selection!=null) selection.Stop(); }
        public int SoldierDeathCount { get; private set; }
        public int RifleShotCount { get; private set; }
        public int ExplosionCount { get; private set; }
        public AudioClip LastSoldierDeath { get; private set; }
        public AudioClip LastCombatClip { get; private set; }
        public void Configure(AudioClip[] sounds) { clips = sounds; }
        public void ConfigureCombat(GameSession owner,AudioClip[] deaths,AudioClip explosion)
        { session=owner;soldierDeathClips=deaths;grenadeExplosion=explosion; }
        private void Awake()
        {
            source = GetComponent<AudioSource>(); source.playOnAwake = false; source.volume = .65f;
            music=new GameObject("Background Music",typeof(AudioSource)).GetComponent<AudioSource>();music.transform.SetParent(transform,false);
            music.playOnAwake=false;music.loop=true;music.spatialBlend=0;music.priority=200;music.volume=0;music.clip=musicClip;
            selection=new GameObject("Dino Selection Voice",typeof(AudioSource)).GetComponent<AudioSource>();selection.transform.SetParent(transform,false);
            selection.playOnAwake=false;selection.spatialBlend=0;selection.priority=32;
            for (int i=0;i<combatVoices.Length;i++)
            {
                var item=new GameObject("Combat Voice "+i,typeof(AudioSource));item.transform.SetParent(transform,false);
                var voice=item.GetComponent<AudioSource>();voice.playOnAwake=false;voice.spatialBlend=0;voice.loop=false;
                voice.outputAudioMixerGroup=source.outputAudioMixerGroup;combatVoices[i]=voice;
            }
        }
        private void Update()
        {
            bool pause=session!=null && session.Model!=null && session.Model.State==RunState.Paused;
            if(music!=null && musicClip!=null)
            {
                if(pause && !musicPaused) { music.Pause();musicPaused=true; }
                else if(!pause)
                {
                    if(musicPaused) { music.UnPause();musicPaused=false; }
                    else if(!music.isPlaying) music.Play();
                    bool duck=SelectionVoicePlaying || session.Player.Weapon.PickupVoicePlaying;
                    float target=duck ? .065f : session.IsPlaying ? .24f : .11f;
                    music.volume=Mathf.MoveTowards(music.volume,target,Time.unscaledDeltaTime*.7f);
                }
            }
            if (pause==combatPaused) return;
            combatPaused=pause;
            foreach (var voice in combatVoices) { if (pause) voice.Pause(); else voice.UnPause(); }
        }
        private bool PlayCombat(AudioClip clip,Vector3 position,float volume)
        {
            if (clip==null || session==null || !session.IsPlaying) return false;
            int index=nextVoice;
            for (int i=0;i<combatVoices.Length;i++)
            {
                int candidate=(nextVoice+i)%combatVoices.Length;
                if (!combatVoices[candidate].isPlaying) { index=candidate;break; }
            }
            nextVoice=(index+1)%combatVoices.Length;
            var voice=combatVoices[index];voice.Stop();voice.clip=clip;voice.volume=source.volume*volume;
            voice.panStereo=Mathf.Clamp(position.x/14f,-1,1)*.35f;voice.Play();LastCombatClip=clip;return true;
        }
        public void PlayRifleShot(AudioClip clip,Vector3 position)
        { if (PlayCombat(clip,position,.55f)) RifleShotCount++; }
        public void PlayExplosion(Vector3 position)
        { if (PlayCombat(grenadeExplosion,position,.85f)) ExplosionCount++; }
        public void PlaySoldierDeath(Vector3 position)
        {
            if (soldierDeathClips==null || soldierDeathClips.Length==0) return;
            int count=soldierDeathClips.Length;
            int index=count==1 ? 0 : deathRandom.Next(count-(previousDeath>=0 ? 1 : 0));
            if (count>1 && previousDeath>=0 && index>=previousDeath) index++;
            if (!PlayCombat(soldierDeathClips[index],position,.75f)) return;
            previousDeath=index;LastSoldierDeath=soldierDeathClips[index];SoldierDeathCount++;
        }
        public void StopCombat()
        {
            foreach (var voice in combatVoices) if (voice!=null) voice.Stop();
            combatPaused=false;LastCombatClip=LastSoldierDeath=null;
            SoldierDeathCount=RifleShotCount=ExplosionCount=0;
        }
        private void OnDisable() { StopCombat();StopSelection();if(music!=null) music.Stop(); }
        public void Play(string cue)
        {
            foreach (var clip in clips) if (clip.name == cue) { source.PlayOneShot(clip); return; }
        }
    }
}
