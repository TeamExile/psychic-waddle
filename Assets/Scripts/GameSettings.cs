using UnityEngine;

namespace Friendslop
{
    /// <summary>
    /// ScriptableObject for game settings - modern Unity pattern for configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Friendslop/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Game Information")]
        public string gameName = "Friendslop";
        public string gameVersion = "1.0.0";

        [Header("Gameplay Settings")]
        [Range(1, 10)]
        public int maxPlayers = 4;

        [Range(1f, 100f)]
        public float playerSpeed = 5f;

        [Range(1f, 50f)]
        public float jumpForce = 10f;

        [Header("Game Rules")]
        public bool friendlyFire = false;
        public int scoreToWin = 100;

        [Range(1f, 600f)]
        public float gameTimeLimit = 300f; // 5 minutes default

        [Header("Visual Settings")]
        public Color primaryColor = Color.blue;
        public Color secondaryColor = Color.green;
    }
}