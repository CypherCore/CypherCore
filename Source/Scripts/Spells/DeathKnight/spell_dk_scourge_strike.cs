using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(55090)]
public class spell_dk_scourge_strike : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private void HandleOnHit(uint UnnamedParameter)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (target != null)
		{
			var festeringWoundAura = target.GetAura(DeathKnightSpells.SPELL_DK_FESTERING_WOUND, GetCaster().GetGUID());

			if (festeringWoundAura != null)
			{
				if (caster.HasAura(DeathKnightSpells.SPELL_DK_UNHOLY_FRENZY))
					caster.CastSpell(caster, DeathKnightSpells.SPELL_DK_UNHOLY_FRENZY_BUFF, true);

				var pestilentPustulesAura = caster.GetAura(DeathKnightSpells.SPELL_DK_PESTILENT_PUSTULES);

				if (pestilentPustulesAura != null)
					if (festeringWoundAura.GetStackAmount() >= pestilentPustulesAura.GetSpellInfo().GetEffect(0).BasePoints)
						caster.ModifyPower(PowerType.Runes, 1);

				var festeringWoundBurst = 1;
				var castiragorAura      = caster.GetAura(DeathKnightSpells.SPELL_DK_CASTIGATOR);

				if (castiragorAura != null)
					festeringWoundBurst += castiragorAura.GetSpellInfo().GetEffect(1).BasePoints;

				festeringWoundBurst = Math.Min(festeringWoundBurst, festeringWoundAura.GetStackAmount());

				for (byte i = 0; i < festeringWoundBurst; ++i)
				{
					caster.CastSpell(target, DeathKnightSpells.SPELL_DK_FESTERING_WOUND_DAMAGE, true);
					festeringWoundAura.ModStackAmount(-1);
				}
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}