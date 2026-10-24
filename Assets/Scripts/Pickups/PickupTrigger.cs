namespace UnholyTrinity.Pickups
{
    /// May want to add another trigger for a required wait on a tile before collection.    
    public enum PickupTrigger
    {
        /// <summary>Collected whenever a unit enters the hex during a slide, including sliding straight over it.</summary>
        OnEnter = 0,
        /// <summary>Collected only when a slide ends on the hex, including a unit that didn't move.</summary>
        OnStop = 1,
    }
}
