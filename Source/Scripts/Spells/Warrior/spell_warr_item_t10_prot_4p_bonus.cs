using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// 70844 - Item - Warrior T10 Protection 4P Bonus
	[Script] // 7.1.5
	internal class spell_warr_item_t10_prot_4p_bonus : AuraScript, IAuraOnProc
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarriorSpells.STOICISM) && spellInfo.GetEffects().Count > 1;
		}

		public void OnProc(ProcEventInfo eventInfo)
		{
			PreventDefaultAction();

			var                target = eventInfo.GetActionTarget();
			var                bp0    = (int)MathFunctions.CalculatePct(target.GetMaxHealth(), GetEffectInfo(1).CalcValue());
			CastSpellExtraArgs args   = new(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
			target.CastSpell((Unit)null, WarriorSpells.STOICISM, args);
		}
	}
}