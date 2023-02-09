using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 40349 - Corrupting Plague
internal class spell_corrupting_plague_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.CorruptingPlague);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
	}

	private void OnPeriodic(AuraEffect aurEff)
	{
		Unit owner = GetTarget();

		List<Creature>           targets           = new();
		CorruptingPlagueSearcher creature_check    = new(owner, 15.0f);
		CreatureListSearcher     creature_searcher = new(owner, targets, creature_check);
		Cell.VisitGridObjects(owner, creature_searcher, 15.0f);

		if (!targets.Empty())
			return;

		PreventDefaultAction();
	}
}