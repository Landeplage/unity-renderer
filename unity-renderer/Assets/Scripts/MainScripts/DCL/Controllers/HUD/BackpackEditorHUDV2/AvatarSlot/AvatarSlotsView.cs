using DCL.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCL.Backpack
{
    public class AvatarSlotsView : BaseComponentView, IAvatarSlotsView
    {
        [SerializeField] private RectTransform avatarSlotsContainer;

        [Header("Prefab references")]
        [SerializeField] private GameObject avatarSlotSectionPrefab;
        [SerializeField] private GameObject avatarSlotPrefab;
        [SerializeField] private GameObject sectionSeparator;

        private readonly Dictionary<string, Transform> avatarSlotSections = new ();
        private readonly Dictionary<string, IAvatarSlotComponentView> avatarSlots = new ();

        public event IAvatarSlotsView.ToggleAvatarSlotDelegate OnToggleAvatarSlot;
        public event Action<string> OnUnequipFromSlot;

        public void CreateAvatarSlotSection(string sectionName, bool addSeparator)
        {
            avatarSlotSections.Add(sectionName, Instantiate(avatarSlotSectionPrefab, avatarSlotsContainer).transform);

            if (addSeparator)
                Instantiate(sectionSeparator, avatarSlotsContainer);
        }

        public void RebuildLayout()
        {
            // Needed because adding elements to a transform that contains a
            // layout does not refresh the child placements
            Utils.ForceRebuildLayoutImmediate(avatarSlotsContainer);
        }

        public void AddSlotToSection(string sectionName, string slotCategory, bool allowUnEquip)
        {
            IAvatarSlotComponentView avatarSlot = Instantiate(avatarSlotPrefab, avatarSlotSections[sectionName]).GetComponent<IAvatarSlotComponentView>();
            avatarSlot.SetCategory(slotCategory);
            avatarSlot.SetUnEquipAllowed(allowUnEquip);
            avatarSlots.Add(slotCategory, avatarSlot);
            avatarSlot.OnSelectAvatarSlot += (slotModel, isToggled) => OnToggleAvatarSlot?.Invoke(slotModel.category, slotModel.allowsColorChange, isToggled);
            avatarSlot.OnUnEquip += (wearableId) => OnUnequipFromSlot?.Invoke(wearableId);
            avatarSlot.OnFocusHiddenBy += (hiddenBy) => avatarSlots[hiddenBy].ShakeAnimation();
        }

        public void DisablePreviousSlot(string category) =>
            avatarSlots[category].OnPointerClickOnDifferentSlot();

        public void SetSlotContent(string category, WearableItem wearableItem, string bodyShape, HashSet<string> hideOverrides)
        {
            avatarSlots[category].SetRarity(wearableItem.rarity);
            avatarSlots[category].SetNftImage(wearableItem.ComposeThumbnailUrl());
            avatarSlots[category].SetWearableId(wearableItem.id);
            avatarSlots[category].SetHideList(wearableItem.GetHidesList(bodyShape));
            RecalculateHideList(hideOverrides);
        }

        public void ResetCategorySlot(string category, HashSet<string> hideOverrides)
        {
            if (avatarSlots[category].GetHideList() != null)
                foreach (var slot in avatarSlots[category].GetHideList())
                    if (avatarSlots.ContainsKey(slot))
                        avatarSlots[slot].SetIsHidden(false, category);

            avatarSlots[category].ResetSlot();
            RecalculateHideList(hideOverrides);
        }

        public void RecalculateHideList(HashSet<string> hideOverrides)
        {
            Dictionary<string, HashSet<string>> previouslyHidden = new Dictionary<string, HashSet<string>>();

            for (var i = 0; i < WearableItem.CATEGORIES_PRIORITY.Count; i++)
            {
                previouslyHidden.Add(WearableItem.CATEGORIES_PRIORITY[i], new HashSet<string>());
            }

            foreach (string priorityCategory in WearableItem.CATEGORIES_PRIORITY)
            {
                if (avatarSlots.ContainsKey(priorityCategory) && avatarSlots[priorityCategory].GetHideList() != null)
                {
                    //avatarSlots[priorityCategory].ResetHidden();
                    foreach (string categoryToHide in avatarSlots[priorityCategory].GetHideList())
                    {
                        //if it hides a slot that doesn't exist, avoid processing hides
                        if (!avatarSlots.ContainsKey(categoryToHide)) continue;

                        //if category has already been processed, avoid processing hides
                        if (previouslyHidden.ContainsKey(categoryToHide) && previouslyHidden[categoryToHide].Contains(priorityCategory))
                        {
                            avatarSlots[categoryToHide].SetIsHidden(false, priorityCategory);
                            continue;
                        }

                        if(previouslyHidden.ContainsKey(priorityCategory))
                            previouslyHidden[priorityCategory].Add(categoryToHide);

                        if (hideOverrides != null && hideOverrides.Contains(categoryToHide))
                        {
                            avatarSlots[categoryToHide].SetIsHidden(false, priorityCategory);
                            continue;
                        }

                        avatarSlots[categoryToHide].SetIsHidden(true, priorityCategory);
                    }
                }
            }
        }

        public override void RefreshControl()
        {
        }
    }
}
