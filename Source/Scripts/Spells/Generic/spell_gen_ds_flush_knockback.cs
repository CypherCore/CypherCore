using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_ds_flush_knockback : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		// Here the Target is the water spout and determines the position where the player is knocked from
		var target = GetHitUnit();

		if (target)
		{
			var player = GetCaster().ToPlayer();

			if (player)
			{
				var horizontalSpeed = 20.0f + (40.0f - GetCaster().GetDistance(target));
				var verticalSpeed   = 8.0f;
				// This method relies on the Dalaran Sewer map disposition and Water Spout position
				// What we do is knock the player from a position exactly behind him and at the end of the pipe
				player.KnockbackFrom(target.GetPosition(), horizontalSpeed, verticalSpeed);
			}
		}
	}
}