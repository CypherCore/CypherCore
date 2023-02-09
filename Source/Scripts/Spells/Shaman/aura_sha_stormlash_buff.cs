using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	// 195222 - Stormlash Buff
	[SpellScript(195222)]
	public class aura_sha_stormlash_buff : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
		{
			eventInfo.GetActor().CastSpell(eventInfo.GetActionTarget(), ShamanSpells.SPELL_SHAMAN_STORMLASH_DAMAGE, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}
}