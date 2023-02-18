// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(117962)]
public class spell_monk_crackling_jade_knockback : SpellScript, ISpellAfterHit
{
	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return Global.SpellMgr.GetSpellInfo(CracklingJade.CRACKLING_JADE_LIGHTNING_KNOCK_BACK, Difficulty.None) != null;
	}

	public void AfterHit()
	{
		var target = GetHitUnit();
		var caster = GetCaster();

		if (caster != null && target != null && caster.HasAura(CracklingJade.CRACKLING_JADE_LIGHTNING_TALENT))
			caster.CastSpell(target, CracklingJade.CRACKLING_JAD_LIGHTNING_TALENT_SPEED, true);
	}
}