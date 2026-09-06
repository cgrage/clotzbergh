namespace Clotzbergh
{
    /// <summary>
    /// What the player's action affects. Shared rather than client-only because the command
    /// carries it and the server derives the affected region from it, instead of trusting a
    /// list of klotzes from the client.
    /// </summary>
    public enum SelectionTool
    {
        None,
        SingleKlotz,
        DigSmall,
        DigMedium,
        DigLarge,
        LevelSmall,
        LevelMedium,
        LevelLarge,
    }

    public static class SelectionTools
    {
        /// <summary>
        /// Radius of the ball the dig tools clear, in sub-klotzes, measured horizontally. How
        /// far it reaches vertically follows from it - see <see cref="KlotzRegion.AroundKlotz"/>.
        /// </summary>
        private const int DigSmallRadius = 2;
        private const int DigMediumRadius = 4;
        private const int DigLargeRadius = 6;

        /// <summary>
        /// The flat disc the level tools clear, in sub-klotzes: how far it spreads around the
        /// klotz, and how far above it reaches. Kept apart from the dig sizes because the two
        /// shapes have nothing to do with each other.
        /// </summary>
        private const int LevelSmallRadius = 1;
        private const int LevelSmallHeight = 15;
        private const int LevelMediumRadius = 2;
        private const int LevelMediumHeight = 15;
        private const int LevelLargeRadius = 3;
        private const int LevelLargeHeight = 15;

        /// <summary>
        /// The region a tool covers when aimed at the klotz occupying the given range. Both the
        /// selection preview and the action itself go through here, so what is shown and what
        /// is removed cannot drift apart.
        /// </summary>
        public static KlotzRegion RegionFor(SelectionTool tool, AbsKlotzCoords klotzMin, AbsKlotzCoords klotzMax)
        {
            return tool switch
            {
                // Reaching nowhere beyond the klotz leaves exactly the klotz itself.
                SelectionTool.SingleKlotz => KlotzRegion.AroundKlotz(klotzMin, klotzMax, 0),
                SelectionTool.DigSmall => KlotzRegion.AroundKlotz(klotzMin, klotzMax, DigSmallRadius),
                SelectionTool.DigMedium => KlotzRegion.AroundKlotz(klotzMin, klotzMax, DigMediumRadius),
                SelectionTool.DigLarge => KlotzRegion.AroundKlotz(klotzMin, klotzMax, DigLargeRadius),
                SelectionTool.LevelSmall => KlotzRegion.AboveKlotz(klotzMin, klotzMax, LevelSmallRadius, LevelSmallHeight),
                SelectionTool.LevelMedium => KlotzRegion.AboveKlotz(klotzMin, klotzMax, LevelMediumRadius, LevelMediumHeight),
                SelectionTool.LevelLarge => KlotzRegion.AboveKlotz(klotzMin, klotzMax, LevelLargeRadius, LevelLargeHeight),
                _ => KlotzRegion.Empty,
            };
        }
    }
}
