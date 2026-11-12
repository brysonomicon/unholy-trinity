using System.Collections.Generic;
using Nrjwolf.Tools.AttachAttributes;
using UnholyTrinity.Helpers;
using UnholyTrinity.Hex;
using UnholyTrinity.Pickups;
using UnityEngine;

namespace UnholyTrinity.Managers
{
    /// <summary>
    /// Scene entry point for pickups: builds a <see cref="PickupField"/> from the map and shows each pickup's
    /// visual while it is available. Registers with the <see cref="GameManager"/>, whose move phase drives it
    /// through <see cref="CollectAlong"/> and <see cref="AdvanceTicks"/>.
    /// </summary>
    public class PickupManager : Singleton<PickupManager>
    {
        [SerializeField][GetComponent] private MapManager mapManager;
        [SerializeField] private float visualHeightOffset = 0.5f;
        [SerializeField] private PickupRespawnRing respawnRingPrefab;
        [SerializeField] private Material pickupGhostMaterial;

        private readonly struct PickupVisual
        {
            public readonly GameObject Root, Model;
            public readonly PickupRespawnRing Ring;
            public readonly PickupGhost Ghost;

            public PickupVisual(GameObject root, GameObject model,
                PickupRespawnRing ring)
            {
                Root = root;
                Model = model;
                Ring = ring;
                Ghost = model.GetComponent<PickupGhost>();
            }
        }

        private readonly Dictionary<HexCoord, PickupVisual> _visuals = new();

        // the player whose plan the visuals currently show, null when they show the real state
        private object _plannedStateShownBy;

        public PickupField Field { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Field = new PickupField(mapManager.Map);
            Field.Collected += OnCollected;
            Field.Respawned += OnRespawned;
            MapManager.LayoutChanged += PositionVisuals;

            SpawnVisuals();
        }

        private void OnDestroy()
        {
            if (Field != null)
            {
                Field.Collected -= OnCollected;
                Field.Respawned -= OnRespawned;
            }
            MapManager.LayoutChanged -= PositionVisuals;
            
        }

        public void CollectAlong(IEnumerable<HexCoord> path, HexCoord end,
            IPickupCollector collector) =>
            Field.CollectAlong(path, end, collector);

        // collects one hex's pickup as a player reaches it, so collection lines up with subtick movement
        public bool TryCollect(HexCoord coord, PickupTrigger reached,
            IPickupCollector collector) =>
            Field.TryCollect(coord, reached, collector);

        // shows each visual as available or taken in a planned state instead of the real one.
        // remembers who asked, so only they can switch back
        public void ShowPlannedState(PickupField plannedState, object shownBy)
        {
            _plannedStateShownBy = shownBy;
            ShowState(plannedState);
        }

        // back to the real state once the player who showed a planned state stops planning. turn changes
        // tell one player to stop and the next to start in either order, so this ignores anyone else
        public void ShowLiveState(object shownBy)
        {
            if (_plannedStateShownBy != shownBy)
            {
                return;
            }

            _plannedStateShownBy = null;
            ShowState(Field);
        }

        private void SpawnVisuals()
        {
            foreach (var (coord, spawn) in Field.Spawns)
            {
                var prefab = spawn.Definition.VisualPrefab;
                if (prefab == null)
                {
                    continue;
                }
                // Debug.Log($"Spawning visual for {spawn.Definition.name} at {coord}");

                var root = new GameObject($"{spawn.Definition.name} {coord}");
                root.transform.SetParent(transform, false);

                var model = Instantiate(prefab, root.transform);
                model.transform.localPosition = Vector3.up * visualHeightOffset;
                if (model.GetComponent<PickupSpin>() == null)
                {
                    model.AddComponent<PickupSpin>();
                }

                var ghost = model.AddComponent<PickupGhost>();
                ghost.Init(pickupGhostMaterial);

                var ring = respawnRingPrefab != null
                    ? Instantiate(respawnRingPrefab, root.transform)
                    : null;

                _visuals.Add(coord, new PickupVisual(root, model, ring));
                Show(coord, spawn);
            }

            PositionVisuals();
        }

        private void PositionVisuals()
        {
            var layout = mapManager.Layout;
            foreach (var (coord, visual) in _visuals)
            {
                visual.Root.transform.position = layout.HexToWorld(coord);
            }
        }

        public void AdvanceTicks(int ticks)
        {
            Field.AdvanceTicks(ticks);
            if (_plannedStateShownBy == null)
            {
                ShowState(Field);
            }

        }

        private void ShowState(PickupField state)
        {
            foreach (var (coord, spawn) in state.Spawns)
            {
                Show(coord, spawn);
            }
        }

        private void Show(HexCoord coord, PickupSpawn spawn)
        {
            if (!_visuals.TryGetValue(coord, out var visual))
            {
                return;
            }

            visual.Ghost.SetGhosted(!spawn.IsAvailable);
            if (visual.Ring != null)
            {
                visual.Ring.Show(spawn);
            }
        }

        private void OnCollected(HexCoord coord, PickupSpawn spawn) =>
            Show(coord, spawn);

        private void OnRespawned(HexCoord coord, PickupSpawn spawn) =>
            Show(coord, spawn);


    }
}
