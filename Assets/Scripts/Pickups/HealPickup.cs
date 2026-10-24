using UnityEngine;

namespace UnholyTrinity.Pickups
{
    [CreateAssetMenu(menuName = "Unholy Trinity/Pickups/Heal", fileName = "HealPickup")]
    public class HealPickup : PickupDefinition
    {
        [field: SerializeField][Min(1)] public int Amount { get; internal set; } = 1;

        public override bool TryApply(IPickupCollector collector) =>
            collector is IHealable healable && healable.TryHeal(Amount);
    }
}
