using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 61752 - Illidan Kill Credit Master
internal class spell_q13400_illidan_kill_master : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(QuestSpellIds.IllidanKillCredit);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		Unit caster = GetCaster();

		if (caster.IsVehicle())
		{
			Unit passenger = caster.GetVehicleKit().GetPassenger(0);

			if (passenger)
				passenger.CastSpell(passenger, QuestSpellIds.IllidanKillCredit, true);
		}
	}
}