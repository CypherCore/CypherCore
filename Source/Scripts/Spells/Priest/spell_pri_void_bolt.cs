using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(234746)]
public class spell_pri_void_bolt : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void HandleEffectScriptEffect(uint UnnamedParameter)
	{
		Aura voidBoltDurationBuffAura = GetCaster().GetAura(PriestSpells.SPELL_PRIEST_VOID_BOLT_DURATION);
		if (voidBoltDurationBuffAura != null)
		{
			Unit unit = GetHitUnit();
			if (unit != null)
			{
				var durationIncreaseMs = voidBoltDurationBuffAura.GetEffect(0).GetBaseAmount();

				Aura pain = unit.GetAura(PriestSpells.SPELL_PRIEST_SHADOW_WORD_PAIN, GetCaster().GetGUID());
				if (pain != null)
				{
					pain.ModDuration(durationIncreaseMs);
				}

				Aura vampiricTouch = unit.GetAura(PriestSpells.SPELL_PRIEST_VAMPIRIC_TOUCH, GetCaster().GetGUID());
				if (vampiricTouch != null)
				{
					vampiricTouch.ModDuration(durationIncreaseMs);
				}
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}
}