using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Players
{       
    // ensures it runs before camera and UI so players exist
    // by the time they ask for them, but after map exists
    [DefaultExecutionOrder(-50)]
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private MapManager mapManager;
        [SerializeField] private Player playerPrefab;
        [SerializeField] private HexCoord[] spawnCoords;

        private void Start()
        {
            gameManager.ResetGame();
            
            for (int i = 0; i < spawnCoords.Length; i++)
            {
                Player spawnedPlayer = Instantiate(playerPrefab);
                spawnedPlayer.name = $"Player {i + 1}";
                spawnedPlayer.Initialize(mapManager, spawnCoords[i]);
                gameManager.RegisterPlayer(spawnedPlayer);
            }
        }
    }
}

