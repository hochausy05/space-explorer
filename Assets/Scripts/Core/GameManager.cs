using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceExplorer.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public RunState State { get; private set; } = RunState.Ready;
        public RunResult? LastResult { get; private set; }
        public int CurrentScore { get; private set; }

        public static event Action RunStarted;
        public event Action<RunResult> RunFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == SceneLoader.Gameplay)
                BeginRunForGameplay();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneLoader.Gameplay)
                BeginRunForGameplay();
        }

        private void BeginRunForGameplay()
        {
            if (State == RunState.Finished && !TryResetRun(out string resetError))
            {
                Debug.LogError(resetError, this);
                return;
            }

            if (State == RunState.Ready && !TryBeginRun(out string beginError))
                Debug.LogError(beginError, this);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (Instance == this)
                Instance = null;
        }

        public bool TryBeginRun(out string error)
        {
            if (State != RunState.Ready)
                return RejectTransition("begin a run", RunState.Ready, out error);

            LastResult = null;
            CurrentScore = 0;
            State = RunState.Playing;
            RunStarted?.Invoke();
            error = null;
            return true;
        }

        public bool TryAddScore(int amount)
        {
            if (State != RunState.Playing || amount <= 0)
                return false;

            CurrentScore += amount;
            return true;
        }

        public bool TryPauseRun(out string error)
        {
            if (State != RunState.Playing)
                return RejectTransition("pause the run", RunState.Playing, out error);

            State = RunState.Paused;
            error = null;
            return true;
        }

        public bool TryResumeRun(out string error)
        {
            if (State != RunState.Paused)
                return RejectTransition("resume the run", RunState.Paused, out error);

            State = RunState.Playing;
            error = null;
            return true;
        }

        public bool TryFinishRun(RunResult result, out string error)
        {
            if (State != RunState.Playing && State != RunState.Paused)
            {
                error = $"Cannot finish a run while the game is {State}. A run must be Playing or Paused.";
                return false;
            }

            LastResult = result;
            State = RunState.Finished;
            error = null;
            RunFinished?.Invoke(result);
            return true;
        }

        public bool TryResetRun(out string error)
        {
            if (State != RunState.Finished)
                return RejectTransition("reset the run", RunState.Finished, out error);

            LastResult = null;
            State = RunState.Ready;
            error = null;
            return true;
        }

        private bool RejectTransition(string action, RunState requiredState, out string error)
        {
            error = $"Cannot {action} while the game is {State}. The required state is {requiredState}.";
            return false;
        }
    }
}
