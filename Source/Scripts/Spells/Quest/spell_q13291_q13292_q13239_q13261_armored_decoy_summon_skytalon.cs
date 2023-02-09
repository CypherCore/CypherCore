using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 59303 - Summon Frost Wyrm
internal class spell_q13291_q13292_q13239_q13261_armored_decoy_summon_skytalon : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCasterBack));
	}

	private void SetDest(ref SpellDestination dest)
	{
		// Adjust effect summon position
		Position offset = new(0.0f, 0.0f, 20.0f, 0.0f);
		dest.RelocateOffset(offset);
	}
}