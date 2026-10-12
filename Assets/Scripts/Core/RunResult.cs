namespace SpaceExplorer.Core
{
    public readonly struct RunResult
    {
        public int FinalScore { get; }
        public int LevelReached { get; }
        public float SurvivalTimeSeconds { get; }

        public RunResult(int finalScore, int levelReached, float survivalTimeSeconds)
        {
            FinalScore = finalScore;
            LevelReached = levelReached;
            SurvivalTimeSeconds = survivalTimeSeconds;
        }
    }
}
