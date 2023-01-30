// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Spells
{
    public class CastSpellTargetArg
    {
        public SpellCastTargets Targets { get; set; }

        public CastSpellTargetArg()
        {
            Targets = new SpellCastTargets();
        }

        public CastSpellTargetArg(WorldObject target)
        {
            if (target != null)
            {
                Unit unitTarget = target.ToUnit();

                if (unitTarget != null)
                {
                    Targets = new SpellCastTargets();
                    Targets.SetUnitTarget(unitTarget);
                }
                else
                {
                    GameObject goTarget = target.ToGameObject();

                    if (goTarget != null)
                    {
                        Targets = new SpellCastTargets();
                        Targets.SetGOTarget(goTarget);
                    }
                    // error when targeting anything other than units and gameobjects
                }
            }
            else
            {
                Targets = new SpellCastTargets(); // nullptr is allowed
            }
        }

        public CastSpellTargetArg(Item itemTarget)
        {
            Targets = new SpellCastTargets();
            Targets.SetItemTarget(itemTarget);
        }

        public CastSpellTargetArg(Position dest)
        {
            Targets = new SpellCastTargets();
            Targets.SetDst(dest);
        }

        public CastSpellTargetArg(SpellCastTargets targets)
        {
            Targets = new SpellCastTargets();
            Targets = targets;
        }
    }
}