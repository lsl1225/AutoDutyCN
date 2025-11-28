using System.Collections.Generic;

namespace AutoDuty.Helpers
{
    using System.Linq;
    using ECommons;
    using ECommons.DalamudServices;
    using FFXIVClientStructs.FFXIV.Client.Game.UI;
    using Lumina.Excel;
    using Lumina.Excel.Sheets;

    internal static class BLUHelper
    {
        internal record BLUSpell(uint ID, byte Entry, string Name, uint Unlock);


        internal static readonly Dictionary<uint, BLUSpell> spellDict = [];
        internal static readonly List<BLUSpell> spells = [];

        static BLUHelper()
        {
            ExcelSheet<AozAction>          actions    = Svc.Data.GetExcelSheet<AozAction>();
            ExcelSheet<AozActionTransient> actionsData = Svc.Data.GetExcelSheet<AozActionTransient>();

            foreach (AozAction aozAction in actions)
                if (aozAction.Rank != 0)
                {
                    AozActionTransient aozActionTransient = actionsData.GetRow(aozAction.RowId);
                    if (aozActionTransient.Number != 0)
                    {
                        BLUSpell spell = new BLUSpell(aozAction.RowId, aozActionTransient.Number, aozAction.Action.Value.Name.GetText() ?? "No name", aozAction.Action.Value.UnlockLink.RowId);
                        spellDict[spell.ID] = spell;
                        spells.Add(spell);
                    }
                }

            spells = spells.OrderBy(sp => sp.Entry).ToList();
        }

        internal static bool SpellUnlocked(uint id) =>
            spells.FirstOrDefault(spell => spell.ID == id) is { } bs && SpellUnlocked(bs);
        internal static bool SpellUnlocked(BLUSpell spell) =>
            SpellUnlockedInternal(spell.Unlock);
        private static unsafe bool SpellUnlockedInternal(uint unlockLink) => 
            UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(unlockLink);
    }
}
