using Game.Entities;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraCheckProc : IAuraScript
    {
        bool CheckProc(ProcEventInfo info);
    }
}