using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceExplorer.Core
{
    public static class SceneLoader
    {
        public const string MainMenu = "MainMenu";
        public const string Gameplay = "Gameplay";
        public const string EndGame = "EndGame";

        // Tải scene đã đăng ký; ghi lỗi nếu tên scene hoặc Build Settings không hợp lệ.
        public static void Load(string sceneName)
        {
            if (!TryLoad(sceneName, out string error))
                Debug.LogError(error);
        }

        // Chỉ tải scene đã đăng ký trong Build Settings và trả lỗi rõ nếu cấu hình thiếu.
        public static bool TryLoad(string sceneName, out string error)
        {
            string scenePath;
            switch (sceneName)
            {
                case MainMenu:
                case Gameplay:
                case EndGame:
                    scenePath = $"Assets/Scenes/{sceneName}.unity";
                    break;
                default:
                    error = string.IsNullOrWhiteSpace(sceneName)
                        ? "Cannot load a scene because the scene name is empty."
                        : $"Cannot load scene '{sceneName}'. Valid scenes are MainMenu, Gameplay, and EndGame.";
                    return false;
            }

            int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
            if (buildIndex < 0)
            {
                error = $"Cannot load scene '{sceneName}' because '{scenePath}' is not registered in Build Settings.";
                return false;
            }

            try
            {
                SceneManager.LoadScene(buildIndex);
                error = null;
                return true;
            }
            catch (System.Exception exception)
            {
                error = $"Failed to load scene '{sceneName}': {exception.Message}";
                return false;
            }
        }
    }
}
