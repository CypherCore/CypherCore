using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // Used for some spells cast by vehicles or charmed creatures that do not send a cooldown event on their own
internal class spell_gen_charmed_unit_spell_cooldown : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		Unit   caster = GetCaster();
		Player owner  = caster.GetCharmerOrOwnerPlayerOrPlayerItself();

		if (owner != null)
		{
			SpellCooldownPkt spellCooldown = new();
			spellCooldown.Caster = owner.GetGUID();
			spellCooldown.Flags  = SpellCooldownFlags.None;
			spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(GetSpellInfo().Id, GetSpellInfo().RecoveryTime));
			owner.SendPacket(spellCooldown);
		}
	}
}