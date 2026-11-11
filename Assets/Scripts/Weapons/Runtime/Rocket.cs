using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Weapons
{
    public class Rocket : MonoBehaviour
    {
        [SerializeField] private float heightOffset = 0.5f;
        [SerializeField] private AimPreview blastPreview;

        private MapManager mapManager;
        // includes the tile fired from and the impact tile
        private List<HexCoord> path;
        private int pathIndex;

        public RocketLauncherData Data { get; private set; }
        public HexCoord Coord => path[pathIndex];
        public bool HasArrived => pathIndex == path.Count - 1;

        public void Launch(
            RocketLauncherData data, 
            MapManager mapManager,
            HexCoord origin, 
            HexCoord target
        ){
            Data = data;
            this.mapManager = mapManager;
            path = origin.LineTo(target);
            pathIndex = 0;
            SnapToCoord();
            blastPreview.Show(data.GetBlastTiles(mapManager.Map, target), mapManager.Layout);
        }

        public void StepForward()
        {
            if (HasArrived)
            {
                return;
            }

            pathIndex++;
            SnapToCoord();
        }

        public List<AimTile> GetBlastTiles()
        {
            return Data.GetBlastTiles(mapManager.Map, Coord);
        }

        private void SnapToCoord()
        {
            transform.position = mapManager.Layout.HexToWorld(Coord) + Vector3.up * heightOffset;
        }
    }
}

