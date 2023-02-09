using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 59579 - Burst at the Seams
internal class spell_q13264_q13276_q13288_q13289_burst_at_the_seams_59579 : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = GetTarget();
		target.CastSpell(target, QuestSpellIds.TrollExplosion, true);
		target.CastSpell(target, QuestSpellIds.ExplodeAbominationMeat, true);
		target.CastSpell(target, QuestSpellIds.ExplodeTrollMeat, true);
		target.CastSpell(target, QuestSpellIds.ExplodeTrollMeat, true);
		target.CastSpell(target, QuestSpellIds.ExplodeTrollBloodyMeat, true);
		target.CastSpell(target, QuestSpellIds.BurstAtTheSeamsBone, true);
	}

	private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = GetTarget();
		var caster = GetCaster();

		if (caster != null)
			switch (target.GetEntry())
			{
				case CreatureIds.IcyGhoul:
					target.CastSpell(caster, QuestSpellIds.AssignGhoulKillCreditToMaster, true);

					break;
				case CreatureIds.ViciousGeist:
					target.CastSpell(caster, QuestSpellIds.AssignGeistKillCreditToMaster, true);

					break;
				case CreatureIds.RisenAllianceSoldiers:
					target.CastSpell(caster, QuestSpellIds.AssignSkeletonKillCreditToMaster, true);

					break;
			}

		target.CastSpell(target, QuestSpellIds.BurstAtTheSeams59580, true);
	}
}