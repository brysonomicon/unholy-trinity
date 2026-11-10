using UnityEngine;

namespace UnholyTrinity.Pickups
{
    [CreateAssetMenu(menuName = "Unholy Trinity/Pickups/Armour", fileName = "ArmourPickup")]
    public class ArmourPickup : PickupDefinition
    {
        [field: SerializeField][Min(1)] public int Amount { get; internal set; } = 1;

        public override bool TryApply(IPickupCollector collector) =>
            collector is IArmourable armourable && Amount > 0 && armourable.TryAddArmour(Amount);
    }
}
