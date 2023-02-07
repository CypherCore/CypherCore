using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(1822)]
public class spell_dru_rake : SpellScript, IHasSpellEffects
{
	private bool _stealthed = false;

	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();
	public override bool Load()
	{
		Unit caster = GetCaster();
		if (caster.HasAuraType(AuraType.ModStealth))
		{
			_stealthed = true;
		}

		return true;
	}

	private void HandleOnHit(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		Unit target = GetExplTargetUnit();
		if (caster == null || target == null)
		{
			return;
		}

		// While stealthed or have Incarnation: King of the Jungle aura, deal 100% increased damage
		if (_stealthed || caster.HasAura(ShapeshiftFormSpells.SPELL_DRUID_INCARNATION_KING_OF_JUNGLE))
		{
			SetHitDamage(GetHitDamage() * 2);
		}

		// Only stun if the caster was in stealth
		if (_stealthed)
		{
			caster.CastSpell(target, RakeSpells.SPELL_DRUID_RAKE_STUN, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}


}