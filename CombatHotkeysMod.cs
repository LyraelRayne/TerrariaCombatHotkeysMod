using System.Linq;
using Terraria.ModLoader;

namespace CombatHotkeys
{
	class CombatHotkeys : Mod
	{
        public int[] slotDefs = { 4, 5, 6, 7, 8, 9 };
        private string[] slotDefaultKeys = { "F", "V", "C", "X", "Z", "Q" };
        public ModHotKey[] hotKeys;

		public CombatHotkeys()
		{
			Properties = new ModProperties()
			{
				Autoload = true,
				AutoloadGores = true,
				AutoloadSounds = true
			};
		}

        public override void Load()
        {
            base.Load();
            hotKeys = slotDefs.Select((slot, index) => RegisterHotKey(string.Format("Quick Use Slot {0}", slot + 1), slotDefaultKeys[index])).ToArray();
        }
    }
}
