using FMODUnity;
using NaughtyAttributes;
using Pulsar.Audio;
using UnityEngine;

namespace UnholyTrinity.Audio
{
    [CreateAssetMenu(fileName = "FmodEvents",
        menuName = "Unholy Trinity/Audio/FmodEvents")]
    public class FmodEvents : SoSingleton<FmodEvents>
    {
        [field: SerializeField] public AnnouncerPack Announcer { get; set; }

        [field: BoxGroup("Level")]
        [field: SerializeField]
        public EventReference BGM { get; private set; }

        [field: BoxGroup("Player")]
        [field: SerializeField]
        public EventReference HealthHeavy { get; private set; }

        [field: BoxGroup("Player")]
        [field: SerializeField]
        public EventReference HealthLight { get; private set; }

        [field: BoxGroup("Player")]
        [field: SerializeField]
        public EventReference StepWet { get; private set; }

        [field: BoxGroup("Player")]
        [field: SerializeField]
        public EventReference StepDry { get; private set; }

        [field: BoxGroup("SFX")]
        [field: SerializeField]
        public EventReference PickupArmor { get; private set; }

        [field: BoxGroup("SFX")]
        [field: SerializeField]
        public EventReference PickupHealth { get; private set; }

        [field: BoxGroup("SFX")]
        [field: SerializeField]
        public EventReference PickupWeapon { get; private set; }

        [field: BoxGroup("SFX")]
        [field: SerializeField]
        public EventReference Gib { get; private set; }

        [field: BoxGroup("Rocket")]
        [field: SerializeField]
        public EventReference RocketMove { get; private set; }

        [field: BoxGroup("Rocket")]
        [field: SerializeField]
        public EventReference RocketExplode { get; private set; }

        [field: BoxGroup("UI")]
        [field: SerializeField]
        public EventReference ButtonClick { get; private set; }

        [field: BoxGroup("UI")]
        [field: SerializeField]
        public EventReference ButtonHover { get; private set; }

        [field: BoxGroup("UI")]
        [field: SerializeField]
        public EventReference GhostCollect { get; private set; }

        [field: BoxGroup("UI")]
        [field: SerializeField]
        public EventReference GhostProject { get; private set; }

        [field: BoxGroup("UI")]
        [field: SerializeField]
        public EventReference GhostRecall { get; private set; }

        [field: BoxGroup("Weapons")]
        [field: SerializeField]
        public EventReference AttackGauntlet { get; private set; }

        [field: BoxGroup("Weapons")]
        [field: SerializeField]
        public EventReference AttackLightning { get; private set; }

        [field: BoxGroup("Weapons")]
        [field: SerializeField]
        public EventReference AttackRail { get; private set; }

        [field: BoxGroup("Weapons")]
        [field: SerializeField]
        public EventReference AttackRocket { get; private set; }

        [field: BoxGroup("Weapons")]
        [field: SerializeField]
        public EventReference WeaponSwitch { get; private set; }
    }
}
