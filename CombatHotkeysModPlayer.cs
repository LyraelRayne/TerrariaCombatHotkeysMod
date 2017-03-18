using CombatHotkeys;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        private int SelectedSlot
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
                return inputStack.Count > 0;
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

        private Item[] Inventory
        {
            get
            {
                return player.inventory;
            }
        }

        /// <summary>
        /// LIFO queue containing most recent valid inputs.
        /// </summary>
        private Stack<int> inputStack = new Stack<int>(5);


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
            var slotDefs = getMod().slotDefs;
            var slotItems = slotDefs.Select(slot => Inventory[slot]).ToArray();
            // Add newly pressed keys to the input stack
            HotkeyState.Select((state, keyIndex) => (state > 1 && slotItems[keyIndex].type > 0) ? keyIndex : -1).Where(slot => slot > -1).Reverse().ToList().ForEach(inputStack.Push);
           // inputStack.DebugMe();


            if (HasQueuedAction)
            {
                var nextKey = inputStack.Peek();
                var nextSlot = slotDefs[nextKey];
                var nextItem = slotItems[nextKey];

                // "Release" the use key if we're in the middle of using so that the next item can be used.
                if (IsCurrentlySwinging && nextSlot != SelectedSlot)
                    player.controlUseItem = false;
                else {
                    // If we don't have an original selection, then we're holding the key for a slot selected normally.
                    // We shouldn't ignore the input just because we could have used the left mouse, so we use that sucker!
                    if(nextSlot != SelectedSlot || !HasOriginalSelection)
                    {
                        UseItem(nextSlot);
                    } else if(!IsReusableItem(nextItem))
                    {
                        inputStack.Pop();
                    }
                    // Either way we're doing something.
                    player.controlUseItem = true;

                    PopDeadKeys(slotItems);
                }
            }
            else
            {
                // Also let go of the button if we're about to go back to the original slot so that it can resume swinging if necessary.
                if (HasOriginalSelection && IsCurrentlySwinging)
                    player.controlUseItem = false;
            }

            return true;
        }

        private void PopDeadKeys(Item[] slotItems)
        {
            while (HasQueuedAction)
            {
                if (HotkeyState[inputStack.Peek()] < 1)
                    // Get rid of the input if the key is no longer held and it was a reusable item.
                    inputStack.Pop();
                else
                    break;
            }
        }

        /// <summary>
        /// Stash the original selection, select the new item and then "press" the use key.
        /// </summary>
        /// <param name="slot">Slot containing the item to be used.</param>
        private void UseItem(int slot)
        {
            // If we already have the slot selected then we don't need to set original selection.
            if (!HasOriginalSelection && slot != SelectedSlot)
                OriginalSelection = SelectedSlot;

            SelectedSlot = slot;
        }

        private bool IsReusableItem(Item item)
        {
            return item.autoReuse || item.channel;
        }

        
    }

    static class DebugExt
    {
        public static void DebugMe<T>(this Stack<T> stack)
        {
            if (stack.Count > 0)
            {
                var builder = new StringBuilder("Input stack: [");
                foreach (var item in stack)
                {
                    builder.Append(item.ToString() + ", ");
                }
                Debug.WriteLine(builder.Append("]").ToString());
            }
        }
    }
}
