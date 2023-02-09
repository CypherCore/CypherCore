using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_toy_train_set_pulse : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(HandleTargets, SpellConst.EffectAll, Targets.UnitSrcAreaAlly));
	}

	private void HandleDummy(int index)
	{
		var target = GetHitUnit().ToPlayer();

		if (target)
		{
			target.HandleEmoteCommand(Emote.OneshotTrain);
			var soundEntry = Global.DB2Mgr.GetTextSoundEmoteFor((uint)TextEmotes.Train, target.GetRace(), target.GetNativeGender(), target.GetClass());

			if (soundEntry != null)
				target.PlayDistanceSound(soundEntry.SoundId);
		}
	}

	private void HandleTargets(List<WorldObject> targetList)
	{
		targetList.RemoveAll(obj => !obj.IsTypeId(TypeId.Player));
	}
}