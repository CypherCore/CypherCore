using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 34914 - Vampiric Touch
internal class spell_pri_vampiric_touch : AuraScript, IAfterAuraDispel, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.VampiricTouchDispel, PriestSpells.GenReplenishment);
	}

	public void HandleDispel(DispelInfo dispelInfo)
	{
		var caster = GetCaster();

		if (caster)
		{
			var target = GetUnitOwner();

			if (target)
			{
				var aurEff = GetEffect(1);

				if (aurEff != null)
				{
					// backfire Damage
					CastSpellExtraArgs args = new(aurEff);
					args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount() * 8);
					caster.CastSpell(target, PriestSpells.VampiricTouchDispel, args);
				}
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 2, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetProcTarget() == GetCaster();
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		eventInfo.GetProcTarget().CastSpell((Unit)null, PriestSpells.GenReplenishment, new CastSpellExtraArgs(aurEff));
	}
}