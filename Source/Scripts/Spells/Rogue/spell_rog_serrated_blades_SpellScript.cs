using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(14172)]
public class spell_rog_serrated_blades_SpellScript : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var blade = GetCaster().GetAuraEffectOfRankedSpell(RogueSpells.SPELL_ROGUE_SERRATED_BLADES_R1, 0);

		if (blade != null)
		{
			var combo = GetCaster().ToPlayer().GetPower(PowerType.ComboPoints);

			if (RandomHelper.randChance(blade.GetAmount() * combo))
			{
				var dot = GetHitUnit().GetAura(RogueSpells.SPELL_ROGUE_RUPTURE, GetCaster().GetGUID());

				if (dot != null)
					dot.RefreshDuration();
			}
		}
	}
}