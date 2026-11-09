using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Players
{       
    // ensures it runs before camera and UI so players exist
    // by the time they ask for them, but after map exists
    [DefaultExecutionOrder(-50)]
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private MapManager mapManager;
        [SerializeField] private Player playerPrefab;
        [SerializeField] private HexCoord[] spawnCoords;
        // placeholder until player models: one per player in spawn order,
        // repeating if there are more players than colours
        [SerializeField] private Color[] playerColours =
        {
            Color.cyan,
            Color.magenta,            
            Color.yellow,
        };

        private void Start()
        {
            GameManager manager = GameManager.Instance;
            manager.ResetGame();
            
            for (int i = 0; i < spawnCoords.Length; i++)
            {
                Player spawnedPlayer = Instantiate(playerPrefab);
                spawnedPlayer.name = $"Player {i + 1}";
                spawnedPlayer.Initialize(mapManager, spawnCoords[i]);

                if (playerColours.Length > 0)
                {
                    spawnedPlayer.SetColour(playerColours[i % playerColours.Length]);
                }

                manager.RegisterPlayer(spawnedPlayer);
            }
        }
    }
}

