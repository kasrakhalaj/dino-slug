using DinoSlug;
using NUnit.Framework;

public sealed class RunModelTests
{
    [Test] public void CharacterSelectionCannotEarnScoreOrTakeDamageAndDeployStartsClean()
    {
        var run=new RunModel();run.OpenCharacterSelect();Assert.That(run.State,Is.EqualTo(RunState.SelectingCharacter));
        run.Advance(100);Assert.That(run.CollectCoin(),Is.False);Assert.That(run.TakeHit(),Is.False);Assert.That(run.Score,Is.Zero);
        run.TogglePause();Assert.That(run.State,Is.EqualTo(RunState.SelectingCharacter));
        run.ReturnToMenu();run.OpenCharacterSelect();run.StartRun();Assert.That(run.State,Is.EqualTo(RunState.Playing));Assert.That(run.Lives,Is.EqualTo(3));
        run.OpenCharacterSelect();Assert.That(run.State,Is.EqualTo(RunState.Playing));
    }
    [TestCase(2999,0)] [TestCase(3000,1)] [TestCase(5999,1)]
    [TestCase(6000,2)] [TestCase(9000,3)] [TestCase(18000,6)]
    public void ScoreMilestonesAdvanceSectorsAndCapThreatWithoutChangingSpeedRules(int score,int sector)
    {
        var run=new RunModel();run.StartRun();run.Advance(score/2f);
        Assert.That(run.SectorIndex,Is.EqualTo(sector));Assert.That(run.ThreatTier,Is.EqualTo(System.Math.Min(2,sector)));
        Assert.That(run.BaseSpeed,Is.EqualTo(System.Math.Min(11,7+run.Distance/250f)));
    }
    [Test] public void CoinsCanCrossMapBoundaryAndRestartReturnsToFirstSector()
    {
        var run=new RunModel();run.StartRun();run.Advance(1495);Assert.That(run.SectorIndex,Is.Zero);
        run.TogglePause();Assert.That(run.CollectCoin(),Is.False);Assert.That(run.SectorIndex,Is.Zero);
        run.TogglePause();run.CollectCoin();Assert.That(run.Score,Is.EqualTo(3015));Assert.That(run.SectorIndex,Is.EqualTo(1));
        run.StartRun();Assert.That(run.SectorIndex,Is.Zero);Assert.That(run.ThreatTier,Is.Zero);
    }
    [Test] public void MedicineHealsOneLifeOnlyWhilePlayingAndCannotReviveOrOverheal()
    {
        var run=new RunModel();Assert.That(run.Heal(),Is.False);run.StartRun();
        Assert.That(run.Heal(),Is.False);run.TakeHit();run.TakeHit();
        Assert.That(run.Heal(),Is.True);Assert.That(run.Lives,Is.EqualTo(2));
        run.TogglePause();Assert.That(run.Heal(),Is.False);run.TogglePause();
        Assert.That(run.Heal(),Is.True);Assert.That(run.Lives,Is.EqualTo(3));Assert.That(run.Heal(),Is.False);
        run.TakeHit();run.TakeHit();run.TakeHit();Assert.That(run.Heal(),Is.False);Assert.That(run.Lives,Is.Zero);
    }
    [Test] public void ThreeHitsEndRunAndLaterHitsAreIgnored()
    {
        var run = new RunModel(); run.StartRun();
        Assert.That(run.TakeHit(), Is.True); Assert.That(run.Lives, Is.EqualTo(2));
        Assert.That(run.TakeHit(), Is.True); Assert.That(run.Lives, Is.EqualTo(1));
        Assert.That(run.TakeHit(), Is.True); Assert.That(run.State, Is.EqualTo(RunState.Dying));
        Assert.That(run.TakeHit(), Is.False); Assert.That(run.Lives, Is.Zero);
        run.FinishDeath(); Assert.That(run.State, Is.EqualTo(RunState.GameOver));
    }

    [Test] public void PauseFreezesDistanceRewardsAndDamage()
    {
        var run = new RunModel(); run.StartRun(); run.Advance(10); run.TogglePause();
        run.Advance(100); Assert.That(run.CollectCoin(), Is.False); Assert.That(run.TakeHit(), Is.False);
        Assert.That(run.Score, Is.EqualTo(20)); Assert.That(run.Lives, Is.EqualTo(3));
        run.TogglePause(); run.Advance(5); Assert.That(run.Score, Is.EqualTo(30));
    }

    [Test] public void RestartResetsRunAndPreservesBest()
    {
        var run = new RunModel(20); run.StartRun(); run.Advance(100); run.CollectCoin(); run.TakeHit();
        run.TogglePause(); run.StartRun();
        Assert.That(run.State, Is.EqualTo(RunState.Playing)); Assert.That(run.Lives, Is.EqualTo(3));
        Assert.That(run.Score, Is.Zero); Assert.That(run.Coins, Is.Zero); Assert.That(run.HighScore, Is.EqualTo(225));
    }

    [Test] public void MenuAndDeathCannotEarnScore()
    {
        var run = new RunModel(); run.Advance(10); run.CollectCoin(); Assert.That(run.Score, Is.Zero);
        run.StartRun(); run.TakeHit(); run.TakeHit(); run.TakeHit(); run.Advance(50); run.CollectCoin();
        Assert.That(run.Score, Is.Zero); run.TogglePause(); Assert.That(run.State, Is.EqualTo(RunState.Dying));
    }

    [Test] public void HighScoreNeverDecreasesAndDifficultyHasACap()
    {
        var run = new RunModel(100); run.StartRun(); run.CollectCoin(); Assert.That(run.HighScore, Is.EqualTo(100));
        run.Advance(100000); Assert.That(run.BaseSpeed, Is.EqualTo(11));
        int best = run.HighScore; run.ReturnToMenu(); run.StartRun(); Assert.That(run.HighScore, Is.EqualTo(best));
    }

    [Test] public void NegativeDistanceAndInvalidSavedRecordAreRejected()
    {
        var run = new RunModel(-50); run.StartRun(); run.Advance(-100);
        Assert.That(run.Distance, Is.Zero); Assert.That(run.HighScore, Is.Zero);
    }
}
