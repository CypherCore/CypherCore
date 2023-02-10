using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DemonHunter;

[Script]
public class at_shattered_soul_fragment : AreaTriggerAI
{
	public at_shattered_soul_fragment(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		if (unit != at.GetCaster() || !unit.IsPlayer() || unit.ToPlayer().GetClass() != Class.DemonHunter)
			return;

		switch (at.GetEntry())
		{
			case 10665:
				if (at.GetCaster().ToPlayer().GetPrimarySpecialization() == TalentSpecialization.DemonHunterHavoc)
					at.GetCaster().CastSpell(at.GetCaster(), ShatteredSoulsSpells.SPELL_DH_SOUL_FRAGMENT_HEAL_25_HAVOC, true);

				at.Remove();

				break;

			case 10666:
				if (at.GetCaster().ToPlayer().GetPrimarySpecialization() == TalentSpecialization.DemonHunterHavoc)
					at.GetCaster().CastSpell(at.GetCaster(), ShatteredSoulsSpells.SPELL_DH_SOUL_FRAGMENT_HEAL_25_HAVOC, true);

				at.Remove();

				break;
		}
	}
}