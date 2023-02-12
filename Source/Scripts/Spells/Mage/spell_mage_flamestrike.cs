using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(2120)]
public class spell_mage_flamestrike : SpellScript, ISpellAfterCast, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleOnHit(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (caster.HasAura(MageSpells.SPELL_MAGE_HOT_STREAK))
		{
			caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_HOT_STREAK);

			if (caster.HasAura(MageSpells.SPELL_MAGE_PYROMANIAC))
			{
				var pyromaniacEff0 = caster.GetAuraEffect(MageSpells.SPELL_MAGE_PYROMANIAC, 0);

				if (pyromaniacEff0 != null)
					if (RandomHelper.randChance(pyromaniacEff0.GetAmount()))
					{
						if (caster.HasAura(MageSpells.SPELL_MAGE_HEATING_UP))
							caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_HEATING_UP);

						caster.CastSpell(caster, MageSpells.SPELL_MAGE_HOT_STREAK, true);
					}
			}
		}
	}

	public void AfterCast()
	{
		var caster = GetCaster();
		var dest   = GetExplTargetDest();

		if (caster == null || dest == null)
			return;

		if (caster.HasAura(MageSpells.SPELL_MAGE_FLAME_PATCH))
			caster.CastSpell(dest.GetPosition(), MageSpells.SPELL_MAGE_FLAME_PATCH_TRIGGER, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHit));
	}
}