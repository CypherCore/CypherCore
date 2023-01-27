using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
	public interface IAuraCalcPeriodic : IAuraEffectHandler
	{
		void CalcPeriodic(AuraEffect aura, ref bool isPeriodic, ref int amplitude);
	}

	public class EffectCalcPeriodicHandler : AuraEffectHandler, IAuraCalcPeriodic
	{
		public delegate void AuraEffectCalcPeriodicDelegate(AuraEffect aura, ref bool isPeriodic, ref int amplitude);

		private AuraEffectCalcPeriodicDelegate _fn;

		public EffectCalcPeriodicHandler(AuraEffectCalcPeriodicDelegate fn, uint effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectCalcPeriodic)
		{
			_fn = fn;
		}

		public void CalcPeriodic(AuraEffect aura, ref bool isPeriodic, ref int amplitude)
		{
			_fn(aura, ref isPeriodic, ref amplitude);
		}
	}
}