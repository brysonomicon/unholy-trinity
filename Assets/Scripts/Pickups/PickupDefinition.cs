using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("UnholyTrinity.HexMap.Tests")]

namespace UnholyTrinity.Pickups
{
    /// <summary>
    /// Shared, read-only description of a pickup: what it does, when it is collected and how long it takes to respawn.
    /// Subclass this to add a new effect. Assets are shared: every pickup point that references this asset uses
    /// the same instance, so per-point state (the respawn countdown) lives in <see cref="PickupSpawn"/>.
    /// </summary>
    public abstract class PickupDefinition : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField][Min(1)] public int RespawnTicks { get; private set; } = 1;
        [field: SerializeField] public PickupTrigger Trigger { get; private set; } = PickupTrigger.OnStop;

        [field: SerializeField, Tooltip("Optional. Shown on the pickup point while the pickup is available.")]
        public GameObject VisualPrefab { get; private set; }

        /// <summary>
        /// Applies the effect to <paramref name="collector"/>. Returns false when the collector can't use it,
        /// in which case the pickup is not consumed.
        /// </summary>
        public abstract bool TryApply(IPickupCollector collector);

        internal void Configure(int respawnTicks, PickupTrigger trigger)
        {
            RespawnTicks = respawnTicks;
            Trigger = trigger;
        }
    }
}
