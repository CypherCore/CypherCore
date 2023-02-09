using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(195452)]
public class spell_rog_nightblade_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	private int _cp;

	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		int maxcp = caster.HasAura(RogueSpells.SPELL_ROGUE_DEEPER_STRATAGEM) ? 6 : 5;
		_cp = Math.Min(caster.GetPower(PowerType.ComboPoints) + 1, maxcp);

		Aura aur = GetAura();
		if (aur != null)
		{
			aur.SetMaxDuration(6000 + 2000 * _cp);
			aur.RefreshDuration();
		}

		if (caster != null)
		{
			caster.ModifyPower(PowerType.ComboPoints, -1 * (_cp - 1));
		}

		var catEntry = Global.SpellMgr.GetSpellInfo(RogueSpells.SPELL_ROGUE_SHADOW_DANCE, Difficulty.None);

		if (caster.HasAura(RogueSpells.SPELL_ROGUE_DEEPENING_SHADOWS) && RandomHelper.randChance(20 * _cp))
		{
			caster.GetSpellHistory().ModifyCooldown(catEntry, TimeSpan.FromMilliseconds(_cp * -3000));
		}

		if (caster != null)
		{
			if (caster.HasAura(RogueSpells.SPELL_ROGUE_RELENTLESS_STRIKES) && RandomHelper.randChance(20 * _cp))
			{
				caster.CastSpell(caster, RogueSpells.SPELL_ROGUE_RELENTLESS_STRIKES_POWER, true);
			}
		}
		if (caster.HasAura(RogueSpells.SPELL_ROGUE_ALACRITY) && RandomHelper.randChance(20 * _cp))
		{
			caster.CastSpell(caster, RogueSpells.SPELL_ROGUE_ALACRITY_BUFF, true);
		}
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetDamageInfo().GetAttackType() == WeaponAttackType.BaseAttack || eventInfo.GetDamageInfo().GetAttackType() == WeaponAttackType.OffAttack)
		{
			Unit caster = eventInfo.GetActor();
			Unit target = eventInfo.GetActionTarget();
			if (caster == null || target == null)
			{
				return false;
			}

			caster.CastSpell(target, RogueSpells.SPELL_ROGUE_NIGHTBLADE_SLOW, true);
			return true;
		}
		return false;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.RealOrReapplyMask));
	}
}