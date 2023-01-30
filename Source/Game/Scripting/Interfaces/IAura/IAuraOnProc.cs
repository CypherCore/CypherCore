using Game.Entities;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraOnProc : IAuraScript
    {
        void OnProc(ProcEventInfo info);
    }
}