using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DinoSlug
{
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject gameOver;
        [SerializeField] private GameObject inGame;
        [SerializeField] private GameObject menuHero;
        [SerializeField] private TMP_Text score;
        [SerializeField] private TMP_Text record;
        [SerializeField] private TMP_Text coins;
        [SerializeField] private TMP_Text lives;
        [SerializeField] private TMP_Text distance;
        [SerializeField] private TMP_Text result;
        [SerializeField] private TMP_Text resultDetail;
        [SerializeField] private TMP_Text menuRecord;
        [SerializeField] private TMP_Text dashLabel;
        [SerializeField] private RectTransform healthFill;
        [SerializeField] private RectTransform dashFill;
        private int previousScore = -1;
        private int previousRecord = -1;
        private int previousCoins = -1;
        private int previousLives = -1;
        private int previousDistance = -1;

        public void Configure(GameObject main, GameObject pause, GameObject over, GameObject hud, GameObject hero,
            TMP_Text current, TMP_Text best, TMP_Text collected, TMP_Text health, TMP_Text meters, TMP_Text final,
            TMP_Text detail, TMP_Text mainBest, TMP_Text dash, RectTransform healthBar, RectTransform dashBar)
        {
            mainMenu = main; pauseMenu = pause; gameOver = over; inGame = hud; menuHero = hero;
            score = current; record = best; coins = collected; lives = health; distance = meters;
            result = final; resultDetail = detail; menuRecord = mainBest; dashLabel = dash;
            healthFill = healthBar; dashFill = dashBar;
        }

        public void ShowState(RunState state)
        {
            mainMenu.SetActive(state == RunState.MainMenu);
            menuHero.SetActive(state == RunState.MainMenu);
            if (state == RunState.MainMenu) menuHero.GetComponent<Animator>().SetBool("Grounded", true);
            pauseMenu.SetActive(state == RunState.Paused);
            gameOver.SetActive(state == RunState.GameOver);
            inGame.SetActive(state != RunState.MainMenu);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                var activeMenu = state == RunState.MainMenu ? mainMenu : state == RunState.Paused ? pauseMenu : state == RunState.GameOver ? gameOver : null;
                if (activeMenu != null)
                {
                    var first = activeMenu.GetComponentInChildren<UnityEngine.UI.Button>();
                    if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
                }
            }
        }

        public void RefreshValues(RunModel model, float dashReady)
        {
            if (model.Score != previousScore) { previousScore = model.Score; score.text = model.Score.ToString("D5"); result.text = model.Score.ToString("N0"); }
            if (model.HighScore != previousRecord)
            {
                previousRecord = model.HighScore;
                record.text = model.HighScore.ToString("D5");
                menuRecord.text = "PERSONAL BEST   " + model.HighScore.ToString("N0");
            }
            if (model.Coins != previousCoins) { previousCoins = model.Coins; coins.text = model.Coins.ToString("D2"); }
            if (model.Lives != previousLives)
            {
                previousLives = model.Lives;
                lives.text = model.Lives + " / 3";
                healthFill.anchorMax = new Vector2(model.Lives / 3f, 1);
            }
            int meters = (int)model.Distance;
            if (meters != previousDistance) { previousDistance = meters; distance.text = meters + " m"; }
            dashFill.anchorMax = new Vector2(dashReady, 1);
            dashLabel.text = dashReady >= .999f ? "SHIFT   DASH READY" : "SHIFT   RECHARGING";
            if (model.State == RunState.GameOver)
                resultDetail.text = meters + " METERS  /  " + model.Coins + " COINS  /  BEST " + model.HighScore.ToString("N0");
        }
    }
}
