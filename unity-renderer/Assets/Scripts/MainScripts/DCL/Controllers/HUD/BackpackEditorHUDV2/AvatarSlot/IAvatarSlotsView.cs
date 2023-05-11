using System;
using System.Collections.Generic;

namespace DCL.Backpack
{
    public interface IAvatarSlotsView
    {
        delegate void ToggleAvatarSlotDelegate(string slotCategory, bool supportColor, bool isSelected);
        event ToggleAvatarSlotDelegate OnToggleAvatarSlot;

        event Action<string> OnUnequipFromSlot;

        void CreateAvatarSlotSection(string sectionName, bool addSeparator);
        void RebuildLayout();
        void AddSlotToSection(string sectionName, string slotCategory, bool allowUnEquip);
        void DisablePreviousSlot(string category);
        void SetSlotContent(string category, WearableItem wearableItem, string bodyShape, HashSet<string> hideOverrides);
        void ResetCategorySlot(string category, HashSet<string> hideOverrides);
        void RecalculateHideList(HashSet<string> hideOverrides);
    }
}
