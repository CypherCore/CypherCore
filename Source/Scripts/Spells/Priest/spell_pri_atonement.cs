// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 81749 - Atonement
public class spell_pri_atonement : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	private readonly List<ObjectGuid> _appliedAtonements = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.ATONEMENT_HEAL, PriestSpells.SINS_OF_THE_MANY) && spellInfo.GetEffects().Count > 1 && Global.SpellMgr.GetSpellInfo(PriestSpells.SINS_OF_THE_MANY, Difficulty.None).GetEffects().Count > 2;
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetDamageInfo() != null;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public void AddAtonementTarget(ObjectGuid target)
	{
		_appliedAtonements.Add(target);

		UpdateSinsOfTheManyValue();
	}

	public void RemoveAtonementTarget(ObjectGuid target)
	{
		_appliedAtonements.Remove(target);

		UpdateSinsOfTheManyValue();
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var                damageInfo = eventInfo.GetDamageInfo();
		CastSpellExtraArgs args       = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount()));

        _appliedAtonements.RemoveAll((Predicate<ObjectGuid>)(targetGuid =>
		                             {
			                             var target = Global.ObjAccessor.GetUnit(GetTarget(), targetGuid);

			                             if (target)
			                             {
				                             if (target.GetExactDist(GetTarget()) < GetEffectInfo(1).CalcValue())
                                                 GetTarget().CastSpell(target, (uint)PriestSpells.ATONEMENT_HEAL, args);

				                             return false;
			                             }

			                             return true;
		                             }));
	}

	private void UpdateSinsOfTheManyValue()
	{
		double[] damageByStack =
		{
			12.0f, 12.0f, 10.0f, 8.0f, 7.0f, 6.0f, 5.0f, 5.0f, 4.0f, 4.0f, 3.0f
		};

		foreach (int effectIndex in new[]
		                             {
			                             0, 1, 2
		                             })
		{
			var sinOfTheMany = GetUnitOwner().GetAuraEffect(PriestSpells.SINS_OF_THE_MANY, effectIndex);

			sinOfTheMany?.ChangeAmount((int)damageByStack[Math.Min(_appliedAtonements.Count, damageByStack.Length - 1)]);
		}
	}
}