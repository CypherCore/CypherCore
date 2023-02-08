using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(201075)]
public class spell_hun_mortal_wounds : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if ((eventInfo.GetHitMask() & ProcFlagsHit.None) != 0 && eventInfo.GetSpellInfo().Id == HunterSpells.SPELL_HUNTER_LACERATE)
		{
			return true;
		}

		return false;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		PreventDefaultAction();

		Player player = GetCaster().ToPlayer();
		if (player != null)
		{
			var chargeCatId = Global.SpellMgr.GetSpellInfo(HunterSpells.SPELL_HUNTER_MONGOOSE_BITE, Difficulty.None).ChargeCategoryId;

			var mongooseBite = CliDB.SpellCategoryStorage.LookupByKey(chargeCatId);
			if (mongooseBite != null) 
			{
				player.GetSpellHistory().RestoreCharge(chargeCatId);
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}
}