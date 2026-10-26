using System;

namespace DinoSlug
{
    public enum RunState { MainMenu, Playing, Paused, Dying, GameOver, SelectingCharacter }

    /// <summary>Authoritative run rules, independent of Unity presentation and physics.</summary>
    public sealed class RunModel
    {
        public const int MaximumLives = 3;
        public RunState State { get; private set; } = RunState.MainMenu;
        public int Lives { get; private set; } = MaximumLives;
        public int Coins { get; private set; }
        public float Distance { get; private set; }
        public int Score => (int)(Distance * 2f) + Coins * 25;
        public const int PointsPerSector = 3000;
        public int SectorIndex => Score / PointsPerSector;
        public int ThreatTier => Math.Min(2, SectorIndex);
        public int HighScore { get; private set; }
        public float BaseSpeed => Math.Min(11f, 7f + Distance / 250f);
        public event Action Changed;

        public RunModel(int highScore = 0) { HighScore = Math.Max(0, highScore); }

        public void OpenCharacterSelect()
        {
            if(State!=RunState.MainMenu) return;
            State=RunState.SelectingCharacter;Changed?.Invoke();
        }

        public void StartRun()
        {
            Lives = MaximumLives;
            Coins = 0;
            Distance = 0;
            State = RunState.Playing;
            Changed?.Invoke();
        }

        public void Advance(float distance)
        {
            if (State != RunState.Playing || distance <= 0) return;
            Distance += distance;
            UpdateRecord();
        }

        public bool CollectCoin()
        {
            if (State != RunState.Playing) return false;
            Coins++;
            UpdateRecord();
            Changed?.Invoke();
            return true;
        }

        public bool TakeHit()
        {
            if (State != RunState.Playing) return false;
            Lives = Math.Max(0, Lives - 1);
            if (Lives == 0) State = RunState.Dying;
            Changed?.Invoke();
            return true;
        }

        public bool Heal()
        {
            if (State != RunState.Playing || Lives >= MaximumLives) return false;
            Lives++;
            Changed?.Invoke();
            return true;
        }

        public void TogglePause()
        {
            if (State == RunState.Playing) State = RunState.Paused;
            else if (State == RunState.Paused) State = RunState.Playing;
            else return;
            Changed?.Invoke();
        }

        public void FinishDeath()
        {
            if (State != RunState.Dying) return;
            State = RunState.GameOver;
            Changed?.Invoke();
        }

        public void ReturnToMenu() { State = RunState.MainMenu; Changed?.Invoke(); }

        private void UpdateRecord()
        {
            if (Score > HighScore) HighScore = Score;
        }
    }
}
