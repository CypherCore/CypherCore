using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 71903 - Item - Shadowmourne Legendary
internal class spell_item_shadowmourne : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.ShadowmourneChaosBaneDamage, ItemSpellIds.ShadowmourneSoulFragment, ItemSpellIds.ShadowmourneChaosBaneBuff);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (GetTarget().HasAura(ItemSpellIds.ShadowmourneChaosBaneBuff)) // cant collect shards while under effect of Chaos Bane buff
			return false;

		return eventInfo.GetProcTarget() && eventInfo.GetProcTarget().IsAlive();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		GetTarget().CastSpell(GetTarget(), ItemSpellIds.ShadowmourneSoulFragment, new CastSpellExtraArgs(aurEff));

		// this can't be handled in AuraScript of SoulFragments because we need to know victim
		var soulFragments = GetTarget().GetAura(ItemSpellIds.ShadowmourneSoulFragment);

		if (soulFragments != null)
			if (soulFragments.GetStackAmount() >= 10)
			{
				GetTarget().CastSpell(eventInfo.GetProcTarget(), ItemSpellIds.ShadowmourneChaosBaneDamage, new CastSpellExtraArgs(aurEff));
				soulFragments.Remove();
			}
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		GetTarget().RemoveAurasDueToSpell(ItemSpellIds.ShadowmourneSoulFragment);
	}
}