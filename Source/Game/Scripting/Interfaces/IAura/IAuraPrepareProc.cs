using Game.Entities;

namespace Game.Scripting.Interfaces.IAura
{
	public interface IAuraPrepareProc : IAuraScript
	{
		bool DoPrepareProc(ProcEventInfo info);
	}
}