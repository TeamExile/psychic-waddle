using UnityEngine;
using UnityEngine.SceneManagement;

namespace Friendslop
{
    /// <summary>
    /// DontDestroyOnLoad component to persist GameObjects across scenes.
    /// </summary>
    public class DDOL : MonoBehaviour
    {
        public bool devMode = true;

        // Load on wake
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            SceneManager.LoadSceneAsync(1);
        }
    }
}