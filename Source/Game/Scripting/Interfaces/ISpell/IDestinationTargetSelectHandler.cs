using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.Spell
{
    public interface IDestinationTargetSelectHandler : ITargetHookHandler
    {
        void SetDest(ref SpellDestination dest);
    }

    public class DestinationTargetSelectHandler : TargetHookHandler, IDestinationTargetSelectHandler
    {
        public delegate void SpellDestinationTargetSelectFnType(ref SpellDestination dest);
        SpellDestinationTargetSelectFnType _func;


        public DestinationTargetSelectHandler(SpellDestinationTargetSelectFnType func, uint effectIndex, Targets targetType, SpellScriptHookType hookType = SpellScriptHookType.DestinationTargetSelect) : base(effectIndex, targetType, false, hookType, true)
        {
            _func = func;
        }

        public void SetDest(ref SpellDestination dest)
        {
            _func(ref dest);
        }
       
    }


}
