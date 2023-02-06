using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Warrior
{

    [CreatureScript(119052)] //119052
    public class npc_warr_banner : ScriptedAI
    {
        public npc_warr_banner(Creature creature) : base(creature)
        {
            Initialize();
        }

        private uint _timer;

        private void Initialize()
        {
            _timer = 0;
        }

        public override void IsSummonedBy(WorldObject summoner)
        {
            base.IsSummonedBy(summoner);
            me.SetReactState(Framework.Constants.ReactStates.Passive);
        }

        public override void UpdateAI(uint diff)
        {
            if (_timer <= diff)
            {
                Unit owner = me.GetOwner();
                if (owner != null)
                {
                    me.SetLevel(owner.GetLevel());
                    List<Unit> allies = new List<Unit>();
                    
                    me.GetFriendlyUnitListInRange(allies, 30.0f, true);
                    foreach (var targets in allies)
                    {
                        if (targets.IsFriendlyTo(owner) && targets.IsPlayer())
                        {
                            if (!targets.HasAura(WarriorSpells.WAR_BANNER_BUFF))
                            {
                                targets.AddAura(WarriorSpells.WAR_BANNER_BUFF, targets);
                            }

                            targets.m_Events.AddEventAtOffset(() =>
                            {
                                if (!targets)
                                {
                                    return;
                                }
                                targets.RemoveAura(WarriorSpells.WAR_BANNER_BUFF);
                            }, TimeSpan.FromSeconds(15));

                            _timer = 1000;
                        }
                    }
                }
                else
                {
                    _timer -= diff;
                }
            }
        }
    }

}
