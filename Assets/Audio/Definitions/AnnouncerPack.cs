using FMODUnity;
using NaughtyAttributes;
using UnityEngine;

namespace Pulsar.Audio
{
    [CreateAssetMenu(fileName = "AnnouncerPack",
        menuName = "Unholy Trinity/Audio/AnnouncerPack")]
    public class AnnouncerPack : ScriptableObject
    {
        [field: BoxGroup("Announcer")]
        [field: SerializeField]
        public EventReference LevelStart { get; private set; }

        [field: BoxGroup("Announcer")]
        [field: SerializeField]
        public EventReference MultiKill { get; private set; }

        [field: BoxGroup("Announcer")]
        [field: SerializeField]
        public EventReference RageQuit { get; private set; }

        [field: BoxGroup("Announcer")]
        [field: SerializeField]
        public EventReference StopWatch { get; private set; }

        [field: BoxGroup("Announcer")]
        [field: SerializeField]
        public EventReference Victory { get; private set; }

        [field: BoxGroup("Achievement")]
        [field: SerializeField]
        public EventReference DoubleRail { get; private set; }

        [field: BoxGroup("Phase")]
        [field: SerializeField]
        public EventReference NewPhase { get; private set; }

        [field: BoxGroup("Phase")]
        [field: SerializeField]
        public EventReference NewTurn { get; private set; }
    }
}
