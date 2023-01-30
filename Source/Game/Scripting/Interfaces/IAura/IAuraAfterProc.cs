using Game.Entities;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraAfterProc : IAuraScript
    {
        bool AfterProc(ProcEventInfo info);
    }
}