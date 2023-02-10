using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	internal class spell_dru_eclipse_common
	{
		public static void SetSpellCount(Unit unitOwner, uint spellId, uint amount)
		{
			var aura = unitOwner.GetAura(spellId);

			if (aura == null)
				unitOwner.CastSpell(unitOwner, spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, (int)amount));
			else
				aura.SetStackAmount((byte)amount);
		}
	}
}