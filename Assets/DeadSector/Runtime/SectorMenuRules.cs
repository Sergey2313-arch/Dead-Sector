namespace DeadSector
{
    public enum SectorEscapeAction
    {
        CloseSettings,
        Resume,
        CloseInventory,
        CloseJournal,
        CloseAtlas,
        OpenPause
    }

    /// <summary>
    /// Deterministic Escape priority. Menus take precedence over the map,
    /// and the map takes precedence over opening another overlapping modal.
    /// Kept free of Unity state for EditMode regression tests.
    /// </summary>
    public static class SectorMenuRules
    {
        public static SectorEscapeAction OnEscape(
            bool menuOpen, bool settingsOpen,
            bool inventoryOpen, bool craftingOpen, bool atlasOpen,
            bool journalOpen = false)
        {
            if (menuOpen)
                return settingsOpen
                    ? SectorEscapeAction.CloseSettings
                    : SectorEscapeAction.Resume;

            if (journalOpen)
                return SectorEscapeAction.CloseJournal;

            if (inventoryOpen || craftingOpen)
                return SectorEscapeAction.CloseInventory;

            return atlasOpen
                ? SectorEscapeAction.CloseAtlas
                : SectorEscapeAction.OpenPause;
        }
    }
}
