using System.Linq;
using Terraria.ModLoader;

namespace CombatHotkeys
{
	class CombatHotkeysMod : Mod
	{
        public readonly int[] SlotDefs = { 4, 5, 6, 7, 8, 9 };
        private readonly string[] _slotDefaultKeys = { "F", "V", "C", "X", "Z", "Q" };
        public ModKeybind[] HotKeys;

		// public CombatHotkeysMod()
		// {
		// 	Properties/* tModPorter Note: Removed. Instead, assign the properties directly (ContentAutoloadingEnabled, GoreAutoloadingEnabled, MusicAutoloadingEnabled, and BackgroundAutoloadingEnabled) */ = new ModProperties()
		// 	{
		// 		Autoload = true,
		// 		AutoloadGores = true,
		// 		AutoloadSounds = true
		// 	};
		// }

        public override void Load()
        {
            base.Load();
            HotKeys = SlotDefs.Select((slot, index) => KeybindLoader.RegisterKeybind(this, $"Quick Use Slot {slot + 1}", _slotDefaultKeys[index])).ToArray();
        }
    }
}
