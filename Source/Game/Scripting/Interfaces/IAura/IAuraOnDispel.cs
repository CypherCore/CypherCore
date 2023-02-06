using Game.Entities;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraOnDispel : IAuraScript
    {
        void OnDispel(DispelInfo dispelInfo);
    }
}