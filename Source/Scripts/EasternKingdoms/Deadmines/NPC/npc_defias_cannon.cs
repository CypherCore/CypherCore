// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(48266)]
    public class npc_defias_cannon : ScriptedAI
    {
        public static readonly Position[] SourcePosition =
        {
            new Position(-30.2622f, -793.069f, 19.237f),
            new Position(-72.1059f, -786.894f, 39.5538f),
            new Position(-58.6424f, -787.132f, 39.3505f),
            new Position(-82.3142f, -775.5f, 26.8933f),
            new Position(-46.901f, -783.155f, 18.4898f),
            new Position(-89.2569f, -782.528f, 17.2564f),
            new Position(-122.925f, -388.813f, 59.0769f),
            new Position(-40.0035f, -793.302f, 39.4754f)
        };

        public static readonly Position[] TargetPosition =
        {
            new Position(0.512153f, -768.229f, 9.80134f),
            new Position(-72.559f, -731.221f, 8.5869f),
            new Position(-49.3264f, -730.056f, 9.32048f),
            new Position(-100.849f, -703.773f, 9.29407f),
            new Position(-30.6337f, -727.731f, 8.52102f),
            new Position(-88.4253f, -724.722f, 8.67503f),
            new Position(-91.9409f, -375.307f, 57.9774f),
            new Position(-12.0556f, -740.252f, 9.10946f)
        };

        public npc_defias_cannon(Creature creature) : base(creature)
        {
            ;
            Instance = creature.GetInstanceScript();
        }

        public InstanceScript Instance;
        public uint Phase;
        public uint CannonBlast_Timer;
        public ObjectGuid TargetGUID;

        public override void Reset()
        {
            base.Reset();
            Phase = 0;
            CannonBlast_Timer = DMData.DATA_CANNON_BLAST_TIMER;
            if (!me)
            {
                return;
            }

            if (!ObjectAccessor.GetCreature(me, TargetGUID))
            {
                GetCreature();
            }
        }

        public bool GetSupporter()
        {
            Creature supporter = me.FindNearestCreature(DMCreatures.NPC_OGRE_HENCHMAN, 7.0f, true);
            if (supporter != null)
            {
                return true;
            }

            supporter = me.FindNearestCreature(DMCreatures.NPC_DEFIAS_PIRATE, 5.0f, true);
            if (supporter != null)
            {
                return true;
            }

            return false;
        }

        public void EnterCombat(Unit UnnamedParameter)
        {
        }

        public void GetCreature()
        {
            if (!me)
            {
                return;
            }

            //for (byte i = 0; i <= 7; i++)
            //{
            //    if (me.IsInDist(SourcePosition[i], 1.0f))
            //    {
            //        TargetGUID = me.SummonCreature(DMCreatures.NPC_SCORCH_MARK_BUNNY_JMF, TargetPosition[i]).GetGUID();
            //        break;
            //    }
            //}
        }

        public override void UpdateAI(uint uiDiff)
        {
            if (!me)
            {
                return;
            }

            if (Phase == 0)
            {
                if (CannonBlast_Timer <= uiDiff)
                {
                    if (!GetSupporter())
                    {
                        me.RemoveUnitFlag(UnitFlags.Uninteractible);
                        Phase++;
                    }
                    else
                    {
                        Creature target = ObjectAccessor.GetCreature(me, TargetGUID);

                        if (target != null)
                            me.CastSpell(target, DMSpells.SPELL_CANNONBALL);
                    }
                    CannonBlast_Timer = (uint)RandomHelper.IRand(3000, 5000);
                }
                else
                {
                    CannonBlast_Timer -= uiDiff;
                }
            }
        }
    }
}
