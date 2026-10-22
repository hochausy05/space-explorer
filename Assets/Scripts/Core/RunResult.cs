namespace SpaceExplorer.Core
{
    public readonly struct RunResult
    {
        public int FinalScore { get; }
        public int LevelReached { get; }
        public float SurvivalTimeSeconds { get; }

        // Lưu snapshot bất biến gồm Score, Level và thời gian sống sót cuối run.
        public RunResult(int finalScore, int levelReached, float survivalTimeSeconds)
        {
            FinalScore = finalScore;
            LevelReached = levelReached;
            SurvivalTimeSeconds = survivalTimeSeconds;
        }
    }
}
