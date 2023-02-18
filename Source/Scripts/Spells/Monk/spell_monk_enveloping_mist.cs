// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(124682)]
public class spell_monk_enveloping_mist : SpellScript, ISpellAfterCast, ISpellBeforeCast
{
	public void BeforeCast()
	{
		if (GetCaster().GetCurrentSpell(CurrentSpellTypes.Channeled) && GetCaster().GetCurrentSpell(CurrentSpellTypes.Channeled).GetSpellInfo().Id == MonkSpells.SOOTHING_MIST)
		{
			GetSpell().m_castFlagsEx = SpellCastFlagsEx.None;
			var targets = GetCaster().GetCurrentSpell(CurrentSpellTypes.Channeled).m_targets;
			GetSpell().InitExplicitTargets(targets);
		}
	}

	public void AfterCast()
	{
		var caster = GetCaster().ToPlayer();

		if (caster == null)
			return;

		if (caster.HasAura(MonkSpells.LIFECYCLES))
			caster.CastSpell(caster, MonkSpells.LIFECYCLES_VIVIFY, true);
	}
}