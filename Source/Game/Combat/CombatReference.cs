// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.Entities;

namespace Game.Combat
{
    public class CombatReference
    {
        public bool IsPvP { get; set; }

        private bool _suppressFirst;
        private bool _suppressSecond;
        public Unit First { get; set; }
        public Unit Second { get; set; }

        public CombatReference(Unit a, Unit b, bool pvp = false)
        {
            First = a;
            Second = b;
            IsPvP = pvp;
        }

        public void EndCombat()
        {
            // sequencing matters here - AI might do nasty stuff, so make sure refs are in a consistent State before you hand off!

            // first, get rid of any threat that still exists...
            First.GetThreatManager().ClearThreat(Second);
            Second.GetThreatManager().ClearThreat(First);

            // ...then, remove the references from both managers...
            First.GetCombatManager().PurgeReference(Second.GetGUID(), IsPvP);
            Second.GetCombatManager().PurgeReference(First.GetGUID(), IsPvP);

            // ...update the combat State, which will potentially remove IN_COMBAT...
            bool needFirstAI = First.GetCombatManager().UpdateOwnerCombatState();
            bool needSecondAI = Second.GetCombatManager().UpdateOwnerCombatState();

            // ...and if that happened, also notify the AI of it...
            if (needFirstAI)
            {
                UnitAI firstAI = First.GetAI();

                firstAI?.JustExitedCombat();
            }

            if (needSecondAI)
            {
                UnitAI secondAI = Second.GetAI();

                secondAI?.JustExitedCombat();
            }
        }

        public void Refresh()
        {
            bool needFirstAI = false, needSecondAI = false;

            if (_suppressFirst)
            {
                _suppressFirst = false;
                needFirstAI = First.GetCombatManager().UpdateOwnerCombatState();
            }

            if (_suppressSecond)
            {
                _suppressSecond = false;
                needSecondAI = Second.GetCombatManager().UpdateOwnerCombatState();
            }

            if (needFirstAI)
                CombatManager.NotifyAICombat(First, Second);

            if (needSecondAI)
                CombatManager.NotifyAICombat(Second, First);
        }

        public void SuppressFor(Unit who)
        {
            Suppress(who);

            if (who.GetCombatManager().UpdateOwnerCombatState())
            {
                UnitAI ai = who.GetAI();

                ai?.JustExitedCombat();
            }
        }

        // suppressed combat refs do not generate a combat State for one side of the relation
        // (used by: vanish, feign death)
        public bool IsSuppressedFor(Unit who)
        {
            return (who == First) ? _suppressFirst : _suppressSecond;
        }

        public void Suppress(Unit who)
        {
            if (who == First)
                _suppressFirst = true;
            else
                _suppressSecond = true;
        }

        public Unit GetOther(Unit me)
        {
            return (First == me) ? Second : First;
        }
    }
}