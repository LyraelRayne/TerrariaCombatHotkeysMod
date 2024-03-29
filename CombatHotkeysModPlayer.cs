﻿using CombatHotkeys;
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
    class CombatHotkeysModPlayer : ModPlayer
    {
        private int[] _keyState;
        private int[] HotkeyState
        {
            get
            {
                if (_keyState != null) return _keyState;
                
                var keys = CHKMod.HotKeys;
                var tempKeyState = new int[keys.Length];
                for (var index = 0; index < keys.Length; index++)
                {
                    tempKeyState[index] = 0;
                }
                _keyState = tempKeyState;
                return tempKeyState;
            }
            set => _keyState = value;
        }

        public CombatHotkeysModPlayer()
        {

        }

        /// <summary>
        /// Place to store the originally selected item before hotkeys took effect.
        /// </summary>
        private int OriginalSelection
        {
            get => Player.nonTorch;
            set => Player.nonTorch = value;
        }

        /// <summary>
        /// The item currently selected by the player.
        /// </summary>
        private int SelectedSlot
        {
            get => Player.selectedItem;
            set => Player.selectedItem = value;
        }

        /// <summary>
        /// Whether or not the combat hotkeys has an action queued.
        /// </summary>
        private bool HasQueuedAction
        {
            get
            {
                return _inputStack.Count > 0;
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
                return Player.itemAnimation > 0;
            }
        }

        private Item[] Inventory
        {
            get
            {
                return Player.inventory;
            }
        }

        /// <summary>
        /// LIFO queue containing most recent valid inputs.
        /// </summary>
        private readonly Stack<int> _inputStack = new Stack<int>(5);


        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            HotkeyState = CHKMod.HotKeys.Select((key, index) => key.JustPressed ? 2 : key.Current ? 1 : 0).ToArray();
        }

        private CombatHotkeysMod CHKMod => ((CombatHotkeysMod)Mod);

        public override bool PreItemCheck()
        {
            var slotDefs = CHKMod.SlotDefs;
            var slotItems = slotDefs.Select(slot => Inventory[slot]).ToArray();
            // Add newly pressed keys to the input stack
            HotkeyState.Select((state, keyIndex) => (state > 1 && slotItems[keyIndex].type > 0) ? keyIndex : -1).Where(slot => slot > -1).Reverse().ToList().ForEach(_inputStack.Push);
           // inputStack.DebugMe();


            if (HasQueuedAction)
            {
                var nextKey = _inputStack.Peek();
                var nextSlot = slotDefs[nextKey];
                var nextItem = slotItems[nextKey];

                // "Release" the use key if we're in the middle of using so that the next item can be used.
                if (IsCurrentlySwinging && nextSlot != SelectedSlot)
                    Player.controlUseItem = false;
                else {
                    // If we don't have an original selection, then we're holding the key for a slot selected normally.
                    // We shouldn't ignore the input just because we could have used the left mouse, so we use that sucker!
                    if(nextSlot != SelectedSlot || !HasOriginalSelection)
                    {
                        UseItem(nextSlot);
                    } else if(!IsReusableItem(nextItem))
                    {
                        _inputStack.Pop();
                    }
                    // Either way we're doing something.
                    Player.controlUseItem = true;

                    PopDeadKeys(slotItems);
                }
            }
            else
            {
                // Also let go of the button if we're about to go back to the original slot so that it can resume swinging if necessary.
                if (HasOriginalSelection && IsCurrentlySwinging)
                    Player.controlUseItem = false;
            }

            return true;
        }

        private void PopDeadKeys(Item[] slotItems)
        {
            while (HasQueuedAction)
            {
                if (HotkeyState[_inputStack.Peek()] < 1)
                    // Get rid of the input if the key is no longer held and it was a reusable item.
                    _inputStack.Pop();
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

    //static class DebugExt
    //{
    //    public static void DebugMe<T>(this Stack<T> stack)
    //    {
    //        if (stack.Count > 0)
    //        {
    //            var builder = new StringBuilder("Input stack: [");
    //            foreach (var item in stack)
    //            {
    //                builder.Append(item.ToString() + ", ");
    //            }
    //            Debug.WriteLine(builder.Append("]").ToString());
    //        }
    //    }
    //}
}
