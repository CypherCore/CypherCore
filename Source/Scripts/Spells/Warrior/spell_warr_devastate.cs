using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
	//20243 - Devastate
	[SpellScript(20243)]
	public class spell_warr_devastate : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleOnHit(int effIndex)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			// https://www.wowhead.com/spell=23922/shield-slam
			if (RandomHelper.randChance(Global.SpellMgr.GetSpellInfo(WarriorSpells.DEVASTATE, Difficulty.None).GetEffect(effIndex).BasePoints))
			{
				var player = caster.ToPlayer();

				if (player != null)
					player.GetSpellHistory().ResetCooldown(WarriorSpells.SHIELD_SLAM, true);
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHit, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}