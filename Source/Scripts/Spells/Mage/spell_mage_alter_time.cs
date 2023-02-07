using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(108978)]
public class spell_mage_alter_time : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void HandleDummy(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();

		if (caster == null || target == null)
		{
			return;
		}

		// Check if the spell has been cast before
		Aura alterTime = target.GetAura(MageSpells.SPELL_ALTER_TIME);
		if (alterTime != null)
		{
			// Check if the target has moved a long distance
			if (target.GetDistance(alterTime.GetCaster()) > 50.0f)
			{
				target.RemoveAura(MageSpells.SPELL_ALTER_TIME);
				return;
			}

			// Check if the target has died
			if (target.IsDead())
			{
				target.RemoveAura(MageSpells.SPELL_ALTER_TIME);
				return;
			}

			// Return the target to their location and health from when the spell was first cast
			target.SetHealth(alterTime.GetEffect(0).GetAmount());
			target.NearTeleportTo(alterTime.GetCaster().GetPositionX(), alterTime.GetCaster().GetPositionY(), alterTime.GetCaster().GetPositionZ(), alterTime.GetCaster().GetOrientation());
			target.RemoveAura(MageSpells.SPELL_ALTER_TIME);
		}
		else
		{
			// Save the target's current location and health
			caster.AddAura(MageSpells.SPELL_ALTER_TIME, target);
			target.SetAuraStack(MageSpells.SPELL_ALTER_TIME, target, (uint)target.GetHealth());
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}