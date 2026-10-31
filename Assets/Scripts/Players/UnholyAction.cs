using System;
using UnholyTrinity.Hex;
using UnholyTrinity.Weapons;

namespace UnholyTrinity.Players
{
    // one tick of a players plan.
    // contains the Planned values so that cards can read planned values
    // until we can figure out how to communicate the plan with just 
    // callbacks
    public class UnholyAction
    {
        public Weapon PlannedShot { get; set; }
        public AimDirection PlannedAim { get; set; }
        public HexDirection? PlannedMove { get; set; }

        public Action Shoot { get; set; }
        public Action Aim { get; set; }
        public Action Move { get; set ;}

        public UnholyAction(AimDirection startingAim)
        {
            PlannedAim = startingAim;
        }
    }
}

