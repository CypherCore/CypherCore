using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_ethereal_pet_aura : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		uint levelDiff = (uint)Math.Abs(GetTarget().GetLevel() - eventInfo.GetProcTarget().GetLevel());

		return levelDiff <= 9;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		List<TempSummon> minionList = new();
		GetUnitOwner().GetAllMinionsByEntry(minionList, CreatureIds.EtherealSoulTrader);

		foreach (Creature minion in minionList)
			if (minion.IsAIEnabled())
			{
				minion.GetAI().Talk(TextIds.SayStealEssence);
				minion.CastSpell(eventInfo.GetProcTarget(), GenericSpellIds.StealEssenceVisual);
			}
	}
}