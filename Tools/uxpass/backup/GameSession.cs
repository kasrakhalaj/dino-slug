using UnityEngine;

namespace DinoSlug
{
    public sealed class GameSession : MonoBehaviour
    {
        public const string HighScoreKey = "DesertDash.HighScore.v1";
        [SerializeField] private PlayerController player;
        [SerializeField] private ObstacleSpawner spawner;
        [SerializeField] private RunnerInput input;
        [SerializeField] private GameHud hud;
        [SerializeField] private GameAudio audioBank;
        [SerializeField] private RunPresentation presentation;
        [SerializeField] private MapProgression maps;
        [SerializeField] private CharacterSelection characterSelection;
        private float deathRemaining;
        private int savedRecord;
        public RunModel Model { get; private set; }
        public PlayerController Player => player;
        public GameAudio Audio => audioBank;
        public ObstacleSpawner Spawner => spawner;
        public RunPresentation Presentation => presentation;
        public MapProgression Maps => maps;
        public CharacterSelection CharacterSelection => characterSelection;
        public void ConfigureCharacterSelection(CharacterSelection value) { characterSelection=value; }
        public void OpenCharacterSelect() { Model.OpenCharacterSelect();audioBank.Play("click"); }
        public void ConfigureMaps(MapProgression value) { maps=value; }
        public void ConfigurePresentation(RunPresentation value) { presentation=value; }
        public bool IsPlaying => Model != null && Model.State == RunState.Playing;
        public float WorldSpeed => IsPlaying ? Model.BaseSpeed * (player.IsDashing ? 1.65f : 1f) : 0f;
        public float ScenerySpeed => Model == null || Model.State == RunState.MainMenu || Model.State==RunState.SelectingCharacter ? 1.1f : WorldSpeed;

        public void Configure(PlayerController runner, ObstacleSpawner obstacles, RunnerInput controls, GameHud ui, GameAudio sounds)
        { player = runner; spawner = obstacles; input = controls; hud = ui; audioBank = sounds; }

        private void Awake()
        {
            Time.timeScale = 1f;
            Application.runInBackground = true;
            savedRecord = PlayerPrefs.GetInt(HighScoreKey, 0);
            Model = new RunModel(savedRecord);
            Model.Changed += RefreshState;
        }

        private void Start() { player.ResetRunner(); RefreshState(); }

        private void Update()
        {
            if (IsPlaying) Model.Advance(WorldSpeed * Time.deltaTime);
            else if (Model.State == RunState.Dying)
            {
                deathRemaining -= Time.deltaTime;
                if (deathRemaining <= 0) { Model.FinishDeath(); SaveRecord(); }
            }
            hud.RefreshValues(Model, player.DashReadyFraction);
        }

        public void StartRun()
        {
            Time.timeScale = 1f;
            audioBank.StopCombat();
            spawner.ResetCourse();
            player.ResetRunner();
            if (presentation!=null) presentation.ResetPresentation();
            Model.StartRun();
            if(maps!=null) maps.ResetMap();
            audioBank.Play("click");
        }

        public void TogglePause()
        {
            if(Model.State==RunState.SelectingCharacter) { MainMenu();return; }
            Model.TogglePause();
            if (Model.State == RunState.Paused) SaveRecord();
        }

        public void MainMenu()
        {
            SaveRecord();
            audioBank.StopCombat();
            Time.timeScale = 1f;
            spawner.ResetCourse();
            player.ResetRunner();
            if (presentation!=null) presentation.ResetPresentation();
            Model.ReturnToMenu();
            if(maps!=null) maps.ResetMap();
        }

        public void Hit()
        {
            if (!Model.TakeHit()) return;
            bool fatal = Model.Lives == 0;
            player.ReceiveHit(fatal);
            if (presentation!=null) presentation.PlayerHit();
            audioBank.Play(fatal ? "death" : "hit");
            if (fatal) { deathRemaining = 1.15f; SaveRecord(); }
        }

        public void CollectCoin(Vector3 position)
        {
            if (!Model.CollectCoin()) return;
            audioBank.Play("coin");
            player.EmitCollect(position);
        }

        public bool Heal(Vector3 position)
        {
            if (!Model.Heal()) return false;
            audioBank.Play("coin");
            player.EmitCollect(position);
            return true;
        }

        private void RefreshState()
        {
            Time.timeScale = Model.State == RunState.Paused ? 0f : 1f;
            input.SetGameplayEnabled(IsPlaying);
            player.SetVisible(Model.State != RunState.MainMenu && Model.State!=RunState.SelectingCharacter);
            hud.ShowState(Model.State);
        }

        private void SaveRecord()
        {
            if (Model == null || Model.HighScore <= savedRecord) return;
            savedRecord = Model.HighScore;
            PlayerPrefs.SetInt(HighScoreKey, savedRecord);
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            if(Model.State==RunState.SelectingCharacter && characterSelection!=null) characterSelection.CancelConfirmation();
            if (IsPlaying) TogglePause();
            SaveRecord();
        }

        private void OnApplicationFocus(bool focused)
        {
            if(!focused && Model!=null && Model.State==RunState.SelectingCharacter && characterSelection!=null) characterSelection.CancelConfirmation();
            if (!focused && IsPlaying) TogglePause();
        }

        private void OnDestroy()
        {
            SaveRecord();
            if (Model != null) Model.Changed -= RefreshState;
            Time.timeScale = 1f;
        }

        public void Quit() { SaveRecord(); Application.Quit(); }
    }
}
