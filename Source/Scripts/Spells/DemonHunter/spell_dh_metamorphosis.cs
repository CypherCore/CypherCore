using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(191427)]
public class spell_dh_metamorphosis : SpellScript, ISpellBeforeCast
{
	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (!Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_HAVOC, Difficulty.None) || !Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_JUMP, Difficulty.None) || !Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_STUN, Difficulty.None))
			return false;

		return true;
	}

	public void BeforeCast()
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var player = caster.ToPlayer();

		if (player == null)
			return;

		var dest = GetExplTargetDest();

		if (dest != null)
			player.CastSpell(new Position(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ()), DemonHunterSpells.SPELL_DH_METAMORPHOSIS_JUMP, true);

		if (player.HasAura(DemonHunterSpells.SPELL_DH_DEMON_REBORN)) // Remove CD of Eye Beam, Chaos Nova and Blur
		{
			player.GetSpellHistory().ResetCooldown(DemonHunterSpells.SPELL_DH_CHAOS_NOVA, true);
			player.GetSpellHistory().ResetCooldown(DemonHunterSpells.SPELL_DH_BLUR, true);
			player.GetSpellHistory().AddCooldown(DemonHunterSpells.SPELL_DH_BLUR_BUFF, 0, TimeSpan.FromMinutes(1));
			player.GetSpellHistory().ResetCooldown(DemonHunterSpells.SPELL_DH_BLUR_BUFF, true);
			player.GetSpellHistory().ResetCooldown(DemonHunterSpells.SPELL_DH_EYE_BEAM, true);
		}
	}
}