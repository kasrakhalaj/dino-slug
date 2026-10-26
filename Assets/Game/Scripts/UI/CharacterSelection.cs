using UnityEngine;
using UnityEngine.UI;

namespace DinoSlug
{
    /// <summary>The single available fighter, with an authored preview and cancellable deployment.</summary>
    public sealed class CharacterSelection : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private UnityEngine.UI.Image preview;
        [SerializeField] private UnityEngine.UI.Button confirm;
        [SerializeField] private TMPro.TMP_Text status;
        [SerializeField] private Sprite[] run,laugh;
        [SerializeField] private float[] laughDurations;
        [SerializeField] private AudioClip selectionVoice;
        [SerializeField] private RectTransform arcadeLayout;
        private Canvas canvas;
        public void ConfigureLayout(RectTransform layout) { arcadeLayout=layout; }
        private void FitLayout()
        {
            if(arcadeLayout==null) return;
            if(canvas==null) canvas=GetComponentInParent<Canvas>();
            float scale=canvas!=null ? canvas.scaleFactor : 1;
            var area=((RectTransform)transform).rect;
            float available=Mathf.Max(0,Mathf.Min(area.width*scale/320,area.height*scale/224));
            // Below native resolution there is no whole-pixel scale that fits.
            // Fit fractionally there; ordinary windows retain crisp integer scaling.
            float pixels=available>=1 ? Mathf.Floor(available) : available;
            arcadeLayout.localScale=Vector3.one*(pixels/Mathf.Max(.01f,scale));
        }
        private void OnRectTransformDimensionsChange() { FitLayout(); }
        private float clock;
        public bool IsConfirming { get; private set; }
        public bool HasDinoVoice => selectionVoice!=null;
        public Sprite PreviewSprite => preview.sprite;
        public void Configure(GameSession owner,UnityEngine.UI.Image image,UnityEngine.UI.Button button,TMPro.TMP_Text caption,
            Sprite[] running,Sprite[] laughing,float[] durations)
        { session=owner;preview=image;confirm=button;status=caption;run=running;laugh=laughing;laughDurations=durations; }
        public void ConfigureVoice(AudioClip clip) { selectionVoice=clip; }
        private void OnEnable()
        {
            clock=0;IsConfirming=false;if(confirm!=null) confirm.interactable=true;
            if(run!=null && run.Length>0 && preview!=null) preview.sprite=run[0];
            if(status!=null) status.text="READY";
            FitLayout();
        }
        public void ConfirmDino()
        {
            if(IsConfirming || session.Model.State!=RunState.SelectingCharacter) return;
            IsConfirming=true;clock=0;confirm.interactable=false;status.text="DINO!";
            session.Audio.PlaySelection(selectionVoice);
        }
        public void CancelConfirmation()
        {
            IsConfirming=false;clock=0;if(confirm!=null) confirm.interactable=true;
            if(status!=null) status.text="READY";
            if(session!=null && session.Audio!=null) session.Audio.StopSelection();
        }
        private void Update()
        {
            if(session.Model==null || session.Model.State!=RunState.SelectingCharacter) return;
            clock+=Time.unscaledDeltaTime;
            if(!IsConfirming) { preview.sprite=run[(int)(clock/.065f)%run.Length];return; }
            float end=0;int frame=laugh.Length-1;
            for(int i=0;i<laugh.Length;i++) { end+=laughDurations[i];if(clock<end) { frame=i;break; } }
            preview.sprite=laugh[frame];
            float duration=0;foreach(float value in laughDurations) duration+=value;
            if(clock>=Mathf.Max(duration,selectionVoice!=null ? selectionVoice.length : 0)+.15f) session.StartRun();
        }
        private void OnDisable() { CancelConfirmation(); }
    }
}
