using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script("spell_item_purified_shard_of_the_scale", ItemSpellIds.PurifiedCauterizingHeal, ItemSpellIds.PurifiedSearingFlames)]
[Script("spell_item_shiny_shard_of_the_scale", ItemSpellIds.ShinyCauterizingHeal, ItemSpellIds.ShinySearingFlames)]
internal class spell_item_shard_of_the_scale : AuraScript, IHasAuraEffects
{
	private readonly uint _damageProcSpellId;

	private readonly uint _healProcSpellId;

	public spell_item_shard_of_the_scale(uint healProcSpellId, uint damageProcSpellId)
	{
		_healProcSpellId   = healProcSpellId;
		_damageProcSpellId = damageProcSpellId;
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(_healProcSpellId, _damageProcSpellId);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		Unit caster = eventInfo.GetActor();
		Unit target = eventInfo.GetProcTarget();

		if (eventInfo.GetTypeMask().HasFlag(ProcFlags.DealHelpfulSpell))
			caster.CastSpell(target, _healProcSpellId, new CastSpellExtraArgs(aurEff));

		if (eventInfo.GetTypeMask().HasFlag(ProcFlags.DealHarmfulSpell))
			caster.CastSpell(target, _damageProcSpellId, new CastSpellExtraArgs(aurEff));
	}
}