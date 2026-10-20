namespace SpaceExplorer.Asteroids
{
    /// <summary>
    /// Resolves standard asteroid health from active Gameplay time at spawn.
    /// Keep type-specific evolution rules centralized for future asteroid types.
    /// </summary>
    public static class AsteroidEvolution
    {
        public const double StageDurationSeconds = 60d;

        public static int GetMaximumHealth(AsteroidSize size, double activeGameplayElapsedSeconds)
        {
            int stage = GetStage(activeGameplayElapsedSeconds);

            switch (size)
            {
                case AsteroidSize.Small:
                    return stage + 1;
                case AsteroidSize.Large:
                    return (stage * 2) + 3;
                default:
                    return 1;
            }
        }

        private static int GetStage(double activeGameplayElapsedSeconds)
        {
            if (!(activeGameplayElapsedSeconds > 0d))
                return 0;

            if (activeGameplayElapsedSeconds >= StageDurationSeconds * 5d)
                return 5;

            return (int)(activeGameplayElapsedSeconds / StageDurationSeconds);
        }
    }
}
