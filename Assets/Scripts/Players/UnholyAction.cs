using System;
using UnholyTrinity.Hex;
using UnholyTrinity.Simulation;
using UnholyTrinity.Weapons;

namespace UnholyTrinity.Players
{
    // one tick of a players plan.
    // contains the Planned values so that cards can read planned values.
    // movement is plain data the simulator reads, so plans can be previewed and
    // recorded for replays. shoot and aim still use callbacks
    public class UnholyAction
    {
        public Weapon PlannedShot { get; set; }
        public AimDirection PlannedAim { get; set; }
        public HexDirection? PlannedMove { get; set; }

        public MoveInput MoveInput => new(PlannedMove);

        public Action Shoot { get; set; }
        public Action Aim { get; set; }

        public UnholyAction(AimDirection startingAim)
        {
            PlannedAim = startingAim;
        }
    }
}
