using System;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(358733)] // 358733 - Glide (Racial)
internal class spell_evo_glide : SpellScript, ISpellCheckCast, ISpellOnCast
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(EvokerSpells.GlideKnockback, EvokerSpells.Hover, EvokerSpells.SoarRacial);
	}

	public SpellCastResult CheckCast()
	{
		var caster = GetCaster();

		if (!caster.IsFalling())
			return SpellCastResult.NotOnGround;

		return SpellCastResult.SpellCastOk;
	}

	public void OnCast()
	{
		var caster = GetCaster().ToPlayer();

		if (caster == null)
			return;

		caster.CastSpell(caster, EvokerSpells.GlideKnockback, true);

		caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(EvokerSpells.Hover, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
		caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(EvokerSpells.SoarRacial, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
	}
}