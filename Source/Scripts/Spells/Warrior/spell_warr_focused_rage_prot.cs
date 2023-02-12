using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	//204488 - Focused Rage
	[SpellScript(204488)]
	public class spell_warr_focused_rage_prot : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			return eventInfo.GetSpellInfo().Id == WarriorSpells.SHIELD_SLAM;
		}
	}

	[SpellScript(204488)]
	public class spell_warr_focused_rage_prot_SpellScript : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(WarriorSpells.VENGEANCE_IGNORE_PAIN, Difficulty.None) != null)
				return false;

			return true;
		}

		private void HandleDummy(uint UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster != null)
				if (caster.HasAura(WarriorSpells.VENGEANCE_AURA))
					caster.CastSpell(caster, WarriorSpells.VENGEANCE_IGNORE_PAIN, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}