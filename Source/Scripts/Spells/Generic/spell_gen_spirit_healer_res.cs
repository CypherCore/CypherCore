// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_spirit_healer_res : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return GetOriginalCaster() && GetOriginalCaster().IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		var originalCaster = GetOriginalCaster().ToPlayer();
		var target         = GetHitUnit();

		if (target)
		{
			NPCInteractionOpenResult spiritHealerConfirm = new();
			spiritHealerConfirm.Npc             = target.GetGUID();
			spiritHealerConfirm.InteractionType = PlayerInteractionType.SpiritHealer;
			spiritHealerConfirm.Success         = true;
			originalCaster.SendPacket(spiritHealerConfirm);
		}
	}
}