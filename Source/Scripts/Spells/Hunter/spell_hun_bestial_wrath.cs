using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(19574)]
public class spell_hun_bestial_wrath : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			Player player = caster.ToPlayer();
			if (player != null)
			{
				Pet pet = player.GetPet();
				if (pet != null)
				{
					pet.AddAura(19574, pet);
				}
			}
		}
	}
}