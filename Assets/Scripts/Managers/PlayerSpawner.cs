using System;
using UnholyTrinity.Hex;
using UnholyTrinity.Players;
using UnholyTrinity.Turns;
using UnholyTrinity.Weapons;
using UnityEngine;

namespace UnholyTrinity.Managers
{
// ensures it runs before camera and UI so players exist
// by the time they ask for them, but after map exists
[DefaultExecutionOrder(-50)]
public class PlayerSpawner : MonoBehaviour
{
    [Serializable]
    private struct SpawnPoint
    {
        [field: SerializeField] public HexCoord Coord { get; private set; }

        [field: SerializeField]
        [field: Tooltip("A0 = east, each step is 30 degree CCW. A3 = north, A6 = west, A9 = south")]
        public AimDirection Aim { get; private set; }
    }

    [SerializeField] private Player playerPrefab;
    [SerializeField] private SpawnPoint[] spawnPoints;

    private void Start()
    {
        GameManager manager = GameManager.Instance;
        MapManager mapManager = MapManager.Instance;
        manager.ResetGame();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Player spawnedPlayer = Instantiate(playerPrefab);
            spawnedPlayer.turnPhase = i switch
            {
                0 => TurnPhase.PlayerOne,
                1 => TurnPhase.PlayerTwo,
                _ => throw new ArgumentException("Need additional turn phases for more players!")
            };
            spawnedPlayer.name = $"Player {i + 1}";
            spawnedPlayer.Initialize(mapManager, spawnPoints[i].Coord, spawnPoints[i].Aim);
        }
    }
}
}