using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(686)] // 686 - Shadow Bolt
	internal class spell_warl_shadow_bolt : SpellScript, ISpellAfterCast
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarlockSpells.SHADOW_BOLT_SHOULSHARD);
		}

		public void AfterCast()
		{
			GetCaster().CastSpell(GetCaster(), WarlockSpells.SHADOW_BOLT_SHOULSHARD, true);
		}
	}
}