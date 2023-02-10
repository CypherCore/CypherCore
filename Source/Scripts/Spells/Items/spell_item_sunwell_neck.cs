using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script("spell_item_sunwell_exalted_caster_neck", ItemSpellIds.LightsWrath, ItemSpellIds.ArcaneBolt)]
[Script("spell_item_sunwell_exalted_melee_neck", ItemSpellIds.LightsStrength, ItemSpellIds.ArcaneStrike)]
[Script("spell_item_sunwell_exalted_tank_neck", ItemSpellIds.LightsWard, ItemSpellIds.ArcaneInsight)]
[Script("spell_item_sunwell_exalted_healer_neck", ItemSpellIds.LightsSalvation, ItemSpellIds.ArcaneSurge)]
internal class spell_item_sunwell_neck : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	private readonly uint _aldorSpellId;
	private readonly uint _scryersSpellId;

	public spell_item_sunwell_neck(uint aldorSpellId, uint scryersSpellId)
	{
		_aldorSpellId   = aldorSpellId;
		_scryersSpellId = scryersSpellId;
	}

	public override bool Validate(SpellInfo spellInfo)
	{
		return CliDB.FactionStorage.ContainsKey(FactionIds.Aldor) && CliDB.FactionStorage.ContainsKey(FactionIds.Scryers) && ValidateSpellInfo(_aldorSpellId, _scryersSpellId);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetActor().GetTypeId() != TypeId.Player)
			return false;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var player = eventInfo.GetActor().ToPlayer();
		var target = eventInfo.GetProcTarget();

		// Aggression checks are in the spell system... just cast and forget
		if (player.GetReputationRank(FactionIds.Aldor) == ReputationRank.Exalted)
			player.CastSpell(target, _aldorSpellId, new CastSpellExtraArgs(aurEff));

		if (player.GetReputationRank(FactionIds.Scryers) == ReputationRank.Exalted)
			player.CastSpell(target, _scryersSpellId, new CastSpellExtraArgs(aurEff));
	}
}