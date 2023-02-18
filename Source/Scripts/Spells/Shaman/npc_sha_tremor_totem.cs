// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman
{
	//8143
	[CreatureScript(8143)]
	public class npc_sha_tremor_totem : ScriptedAI
	{
		public npc_sha_tremor_totem(Creature c) : base(c)
		{
		}

		public enum SpellRelated
		{
			TREMOR_TOTEM_DISPELL = 8146
		}

		public override void Reset()
		{
			base.Reset();
			me.GetOwner();
		}

		public void OnUpdate(uint diff)
		{
			if (diff <= 1000)
			{
				var playerList = me.GetPlayerListInGrid(30.0f);

				if (playerList.Count != 0)
					foreach (Player target in playerList)
						if (target.IsFriendlyTo(me.GetOwner()))
							if (target.HasAuraType(AuraType.ModFear) || target.HasAuraType(AuraType.ModFear2) || target.HasAuraType(AuraType.ModCharm))
								me.CastSpell(target, SpellRelated.TREMOR_TOTEM_DISPELL, true);
			}
		}
	}
}