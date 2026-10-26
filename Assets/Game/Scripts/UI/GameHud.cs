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
        [SerializeField] private TMP_Text mapLabel;
        [SerializeField] private MapProgression maps;
        [SerializeField] private GameObject characterSelect;
        [SerializeField] private TMP_FontAsset pixelFont;
        // Juice: short unscaled-time punches so they still read during hit-stop.
        private float scorePunch, coinPunch, hurtShake, dashPunch;
        private bool dashWasReady = true;
        private Color healthColor;
        private RectTransform vitalityPanel, liveTransform;
        private Vector2 vitalityRest, livesRest;
        private GameObject mapBadge, announcement, weaponStatus;

        private void Awake()
        {
            HudStyle.Apply(inGame != null ? inGame.transform.parent : null, pixelFont);
            if (healthFill != null && healthFill.TryGetComponent<UnityEngine.UI.Image>(out var fill)) healthColor = fill.color;
            if (inGame != null)
            {
                vitalityPanel = inGame.transform.Find("Vitality Panel") as RectTransform;
                if (vitalityPanel != null) vitalityRest = vitalityPanel.anchoredPosition;
                mapBadge = Find("Map Badge"); announcement = Find("Weapon Announcement"); weaponStatus = Find("Weapon Status");
            }
            if (lives != null) { liveTransform = lives.rectTransform; livesRest = liveTransform.anchoredPosition; }
        }
        private GameObject Find(string name) { var t = inGame.transform.Find(name); return t != null ? t.gameObject : null; }
        public void ConfigureSelection(GameObject panel) { characterSelect=panel; }
        private int previousSector=-1;
        private bool previousTransition;
        public void ConfigureMaps(MapProgression progression,TMP_Text label) { maps=progression;mapLabel=label; }
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
            if(characterSelect!=null) characterSelect.SetActive(state==RunState.SelectingCharacter);
            menuHero.SetActive(state == RunState.MainMenu);
            if (state == RunState.MainMenu) menuHero.GetComponent<Animator>().SetBool("Grounded", true);
            pauseMenu.SetActive(state == RunState.Paused);
            gameOver.SetActive(state == RunState.GameOver);
            inGame.SetActive(state != RunState.MainMenu && state!=RunState.SelectingCharacter);
            // Keep the pause and results screens clean: no sector badge or pickup banner behind them.
            bool live = state == RunState.Playing || state == RunState.Dying;
            if (mapBadge != null) mapBadge.SetActive(live);
            if (announcement != null && !live) announcement.SetActive(false);
            else if (announcement != null) announcement.SetActive(true);
            if (weaponStatus != null) weaponStatus.SetActive(state != RunState.GameOver);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                var activeMenu = state == RunState.MainMenu ? mainMenu : state==RunState.SelectingCharacter ? characterSelect : state == RunState.Paused ? pauseMenu : state == RunState.GameOver ? gameOver : null;
                if (activeMenu != null)
                {
                    var first = activeMenu.GetComponentInChildren<UnityEngine.UI.Button>();
                    if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
                }
            }
        }

        public void RefreshValues(RunModel model, float dashReady)
        {
            if(mapLabel!=null && (model.SectorIndex!=previousSector || maps.IsTransitioning!=previousTransition))
            {
                previousSector=model.SectorIndex;
                previousTransition=maps.IsTransitioning;
                mapLabel.text=(previousTransition ? "ENTERING " : "SECTOR ")+(model.SectorIndex+1).ToString("D2")+" / "+maps.NameForSector(model.SectorIndex)
                    +"\n<size=13>NEXT MAP  "+((model.SectorIndex+1)*RunModel.PointsPerSector).ToString("N0")+" POINTS</size>";
            }
            if (model.Score != previousScore)
            {
                if (previousScore >= 0 && model.Score - previousScore >= 20) scorePunch = 1;   // coins, not distance ticks
                previousScore = model.Score; score.text = model.Score.ToString("D5"); result.text = model.Score.ToString("N0");
            }
            if (model.HighScore != previousRecord)
            {
                previousRecord = model.HighScore;
                record.text = model.HighScore.ToString("D5");
                menuRecord.text = "PERSONAL BEST   " + model.HighScore.ToString("N0");
            }
            if (model.Coins != previousCoins)
            {
                if (previousCoins >= 0 && model.Coins > previousCoins) coinPunch = 1;
                previousCoins = model.Coins; coins.text = model.Coins.ToString("D2");
            }
            if (model.Lives != previousLives)
            {
                if (previousLives >= 0 && model.Lives < previousLives) hurtShake = 1;
                previousLives = model.Lives;
                lives.text = model.Lives + " / 3";
                healthFill.anchorMax = new Vector2(model.Lives / 3f, 1);
            }
            int meters = (int)model.Distance;
            if (meters != previousDistance) { previousDistance = meters; distance.text = meters + " m"; }
            dashFill.anchorMax = new Vector2(dashReady, 1);
            bool dashReadyNow = dashReady >= .999f;
            if (dashReadyNow && !dashWasReady) dashPunch = 1;
            dashWasReady = dashReadyNow;
            dashLabel.text = dashReadyNow ? "SHIFT   DASH READY" : "SHIFT   RECHARGING";
            if (model.State == RunState.GameOver)
                resultDetail.text = meters.ToString("N0") + " M  /  " + model.Coins + (model.Coins == 1 ? " COIN" : " COINS")
                    + "  /  SECTOR " + (model.SectorIndex + 1).ToString("D2") + "  /  BEST " + model.HighScore.ToString("N0");
            Animate(model);
        }

        private void Animate(RunModel model)
        {
            float dt = Time.unscaledDeltaTime;
            scorePunch = Mathf.Max(0, scorePunch - dt * 6f);
            coinPunch = Mathf.Max(0, coinPunch - dt * 6f);
            dashPunch = Mathf.Max(0, dashPunch - dt * 5f);
            hurtShake = Mathf.Max(0, hurtShake - dt * 3f);
            Punch(score, scorePunch, .18f);
            Punch(coins, coinPunch, .3f);
            Punch(dashLabel, dashPunch, .15f);
            // Vitality panel shakes on damage; pixel-stepped so it stays crisp.
            Vector2 shake = hurtShake > 0 ? new Vector2(Mathf.Round(Mathf.Sin(hurtShake * 60f) * 6f * hurtShake), 0) : Vector2.zero;
            if (vitalityPanel != null) vitalityPanel.anchoredPosition = vitalityRest + shake;
            if (liveTransform != null) liveTransform.anchoredPosition = livesRest + shake;
            // Last life: the health bar pulses red so the danger is obvious at a glance.
            if (healthFill != null && healthFill.TryGetComponent<UnityEngine.UI.Image>(out var fill))
            {
                if (hurtShake > .6f) fill.color = Color.white;
                else if (model.Lives == 1 && model.State == RunState.Playing)
                    fill.color = Color.Lerp(new Color(.85f, .2f, .18f), new Color(1f, .55f, .4f), .5f + .5f * Mathf.Sin(Time.unscaledTime * 9f));
                else fill.color = healthColor;
            }
        }

        private static void Punch(TMP_Text text, float amount, float strength)
        {
            if (text == null) return;
            float s = 1f + strength * amount * amount;
            text.rectTransform.localScale = new Vector3(s, s, 1);
        }
    }
}
