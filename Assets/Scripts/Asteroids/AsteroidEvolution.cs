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

        public static int GetSpecialMaximumHealth(AsteroidKind kind, double activeGameplayElapsedSeconds)
        {
            int stage = GetStage(activeGameplayElapsedSeconds);

            switch (kind)
            {
                case AsteroidKind.Speed:
                    switch (stage)
                    {
                        case 0:
                        case 1: return 3;
                        case 2: return 4;
                        case 3: return 5;
                        case 4: return 5;
                        default: return 6;
                    }
                case AsteroidKind.Homing:
                case AsteroidKind.Erratic:
                    switch (stage)
                    {
                        case 0:
                        case 1: return 6;
                        case 2: return 8;
                        case 3: return 9;
                        case 4: return 11;
                        default: return 12;
                    }
                case AsteroidKind.Splitter:
                    switch (stage)
                    {
                        case 0:
                        case 1: return 5;
                        case 2: return 6;
                        case 3: return 8;
                        case 4: return 9;
                        default: return 10;
                    }
                case AsteroidKind.Fragment: return 1;
                default: return 1;
            }
        }

        public static int GetStage(double activeGameplayElapsedSeconds)
        {
            if (!(activeGameplayElapsedSeconds > 0d))
                return 0;

            if (activeGameplayElapsedSeconds >= StageDurationSeconds * 5d)
                return 5;

            return (int)(activeGameplayElapsedSeconds / StageDurationSeconds);
        }

        public static float GetSpecialSpawnChance(double activeGameplayElapsedSeconds)
        {
            switch (GetStage(activeGameplayElapsedSeconds))
            {
                case 1: return 0.06f;
                case 2: return 0.10f;
                case 3: return 0.14f;
                case 4: return 0.18f;
                case 5: return 0.20f;
                default: return 0f;
            }
        }
    }
}
