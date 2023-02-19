// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_despawn_self : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster().IsTypeId(TypeId.Unit);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, SpellConst.EffectAll, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		if (GetEffectInfo().IsEffect(SpellEffectName.Dummy) ||
		    GetEffectInfo().IsEffect(SpellEffectName.ScriptEffect))
			GetCaster().ToCreature().DespawnOrUnsummon();
	}
}