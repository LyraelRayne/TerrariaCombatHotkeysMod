using System;
using System.Linq;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace CombatHotkeys
{
    class CombatHotkeysMod : ModPlayer
    {
        private int[] _keyState;
        private int[] HotkeyState
        {
            get
            {
                if (_keyState == null)
                {
                    var keys = getMod().hotKeys;
                    _keyState = new int[keys.Length];
                    for (var index = 0; index < keys.Length; index++)
                    {
                        _keyState[index] = 0;
                    }
                }
                return _keyState;
            }
            set
            {
                this._keyState = value;
            }
        }

        /// <summary>
        /// Place to store the originally selected item before hotkeys took effect.
        /// </summary>
        private int OriginalSelection
        {
            get
            {
                return player.nonTorch;
            }
            set
            {
                player.nonTorch = value;
            }
        }

        /// <summary>
        /// The item currently selected by the player.
        /// </summary>
        private int SelectedItem
        {
            get
            {
                return player.selectedItem;
            }
            set
            {
                player.selectedItem = value;
            }
        }

        /// <summary>
        /// Whether or not the combat hotkeys has an action queued.
        /// </summary>
        private bool HasQueuedAction
        {
            get
            {
                return this.queuedSlot > -1;
            }
        }

        /// <summary>
        /// Whether or not we have a <see cref="OriginalSelection"/>
        /// </summary>
        private bool HasOriginalSelection
        {
            get
            {
                return OriginalSelection > -1;
            }
        }

        /// <summary>
        /// Whether or not the player is currently using an item.
        /// </summary>
        private bool IsCurrentlySwinging
        {
            get
            {
                return player.itemAnimation > 0;
            }
        }

        /// <summary>
        /// Whether or not a reusable item has been used since we started hotkeying.
        /// </summary>
        private bool HasLastReusableSlot
        {
            get { return lastReusableSlot > -1; }
        }

        private Item[] Inventory
        {
            get
            {
                return player.inventory;
            }
        }


        /// <summary>
        /// The slot to use next.
        /// </summary>
        private int queuedSlot = -1;

        /// <summary>
        /// The last slot which was used.
        /// </summary>
        private int lastUsedSlot = -1;

        /// <summary>
        /// Last reusable (autoswing) item slot that was used.
        /// </summary>
        private int lastReusableSlot = -1;


        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            HotkeyState = getMod().hotKeys.Select((key, index) => key.JustPressed ? 2 : key.Current ? 1 : 0).ToArray();
        }

        private CombatHotkeys getMod()
        {
            return ((CombatHotkeys)mod);
        }

        public override bool PreItemCheck()
        {
            // If the player is already using an item via normal means, update the last used slot.
            if (player.controlUseItem)
                lastUsedSlot = SelectedItem;

            UpdateSlotQueue();

            // If queuedslot is -1 or lower, obviously control over controlUseItem should be relinquished to the rest of the event pipeline.
            if (HasQueuedAction)
            {
                // "Release" the use key if we're in the middle of using and another item is next so that the next item can be used.
                if (IsCurrentlySwinging && queuedSlot != lastUsedSlot)
                    player.controlUseItem = false;
                else
                    UseItem(queuedSlot);
            }
            else
            {
                // Also let go of the button if we're about to go back to the original slot so that it can resume swinging if necessary.
                if (HasOriginalSelection && IsCurrentlySwinging)
                    player.controlUseItem = false;

                lastReusableSlot = -1; 
            }

            return true;
        }

        /// <summary>
        /// Stash the original selection, select the new item and then "press" the use key.
        /// </summary>
        /// <param name="slot">Slot containing the item to be used.</param>
        private void UseItem(int slot)
        {
            if (!HasOriginalSelection)
                OriginalSelection = SelectedItem;

            SelectedItem = slot;
            lastUsedSlot = SelectedItem;
            if (Inventory[SelectedItem].autoReuse)
                lastReusableSlot = SelectedItem;
            player.controlUseItem = true;
        }

        /// <summary>
        /// Determines which slot should be used next and sets the queued slot.
        /// 
        /// Uses the button that just got pressed, or the existing queued slot if it hasn't been used yet, or re-use the highest priority auto-swing item otherwise (priority is in order from the right of the hotbar to the left). 
        /// </summary>
        private void UpdateSlotQueue()
        {
            var slotDefs = getMod().slotDefs;
            var slotItems = slotDefs.Select(slot => Inventory[slot]).ToArray();

            // Slots which only just got pressed get priority
            var pressedSlotIndex = Array.FindIndex(HotkeyState.Select((state, keyIndex) => (state > 1 && slotItems[keyIndex].type > 0) ? slotDefs[keyIndex] : -1).ToArray(), slot => slot != -1);
            // Then see if we can reuse any keypresses for auto-swinging weapons.
            var reusedSlotIndex = Array.FindIndex(HotkeyState.Select((state, keyIndex) => { var slot = slotDefs[keyIndex]; return ShouldReuseSlot(state, keyIndex, slotItems, slot) ? slot : -1; }).ToArray(), slot => slot != -1);

            // We have to do it this way because Array.Find will return 0 for not found, but 0 is a valid value!
            var pressedSlot = pressedSlotIndex < 0 ? pressedSlotIndex : slotDefs[pressedSlotIndex];
            var reusedSlot = reusedSlotIndex < 0 ? reusedSlotIndex : slotDefs[reusedSlotIndex];

            // Use the button that just got pressed, or the queued slot if it hasn't been used yet, or re-use an auto-swing item otherwise.
            queuedSlot = pressedSlot > -1 ? pressedSlot : queuedSlot != lastUsedSlot ? queuedSlot : reusedSlot;
        }

        private bool ShouldReuseSlot(int state, int keyIndex, Item[] slotItems, int slot)
        {
            if (HasLastReusableSlot)
                return state == 1 && slot == lastReusableSlot;
            else
                return state == 1 && slotItems[keyIndex].type != 0 && (slotItems[keyIndex].autoReuse);
        }



    }
}
