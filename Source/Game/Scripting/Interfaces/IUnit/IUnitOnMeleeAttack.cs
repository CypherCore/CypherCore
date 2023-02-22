using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.IUnit
{
    public interface IUnitOnMeleeAttack : IScriptObject
    {
        void OnMeleeAttack(CalcDamageInfo damageInfo, WeaponAttackType attType, bool extra);
    }
}
