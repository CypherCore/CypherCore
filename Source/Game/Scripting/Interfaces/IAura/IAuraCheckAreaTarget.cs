using Game.Entities;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraCheckAreaTarget : IAuraScript
    {
        bool CheckAreaTarget(Unit target);
    }
}