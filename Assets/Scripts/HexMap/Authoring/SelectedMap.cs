namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Carries the map picked on the title screen into the game scene.
    /// Null when the game scene was opened directly (e.g. from the editor), so readers should
    /// fall back to their own serialized map.
    /// </summary>
    public static class SelectedMap
    {
        public static HexMapDefinition Definition { get; private set; }

        public static void Set(HexMapDefinition definition)
        {
            Definition = definition;
        }

        public static void Clear()
        {
            Definition = null;
        }
    }
}
