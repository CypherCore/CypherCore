using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(145205)]
public class spell_dru_efflorescence : SpellScript, ISpellOnCast
{


	private struct eCreature
	{
		public static uint NPC_EFFLORESCENCE = 47649;
	}

	public void OnCast()
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			Creature efflorescence = caster.GetSummonedCreatureByEntry(eCreature.NPC_EFFLORESCENCE);
			if (efflorescence != null)
			{
				efflorescence.DespawnOrUnsummon();
			}
		}
	}


}