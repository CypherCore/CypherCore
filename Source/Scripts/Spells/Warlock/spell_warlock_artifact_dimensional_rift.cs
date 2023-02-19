// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	internal class spell_warlock_artifact_dimensional_rift : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleHit(int effIndex)
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (caster == null || target == null)
				return;

			//green //green //purple
			var spellVisualIds = new List<uint>()
			                     {
				                     219117,
				                     219117,
				                     219107
			                     };

			// Chaos Tear  //Chaos Portal  //Shadowy Tear
			var summonIds = new List<uint>()
			                {
				                108493,
				                108493,
				                99887
			                };

			// Durations must be longer, because if the npc gets destroyed before the last projectile hits
			// it won't deal any damage.
			var durations = new List<uint>()
			                {
				                7000,
				                4500,
				                16000
			                };

			var id  = RandomHelper.RandShort() % 3;
			var pos = caster.GetPosition();
			// Portals appear in a random point, in a distance between 4-8yards
			caster.MovePosition(pos, (float)(RandomHelper.RandShort() % 5) + 4.0f, RandomHelper.RandFloat() * (float)(2 * Math.PI));
			Creature rift = caster.SummonCreature(summonIds[(int)id], pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), 0, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(durations[(int)id]));

			if (rift != null)
			{
				rift.CastSpell(rift, spellVisualIds[(int)id], true);
				rift.SetOwnerGUID(caster.GetGUID());
				// We cannot really use me->GetVictim() inside of the AI, since the target
				// for portal is locked, it doesn't change no matter what. So we set it like this
				rift.SetTarget(target.GetGUID());
				// We use same ID and script for Chaos Portal and Chaos Tear as there are no more NPCs for this spell
				rift.SetArmor((int)id, 0);
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
		}
	}
}