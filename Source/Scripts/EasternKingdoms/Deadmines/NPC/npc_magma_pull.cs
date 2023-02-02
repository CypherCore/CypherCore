using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.AI;
using Game.Entities;


using Game.Maps;
using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(49454)]
    public class npc_magma_pull : ScriptedAI
    {
        public static readonly Position VanessaNightmare1 = new Position(-230.717f, -563.0139f, 51.31293f, 1.047198f);
        public static readonly Position GlubtokNightmare1 = new Position(-229.3403f, -560.3629f, 51.31293f, 5.742133f);

        public npc_magma_pull(Creature creature) : base(creature)
        {
            instance = creature.GetInstanceScript();
        }

        public InstanceScript instance;
        public bool Pullplayers;
        public bool Csummon;
        public Player PlayerGUID;
        public uint PongTimer;

        public override void Reset()
        {
            Pullplayers = true;
            Csummon = true;
            PongTimer = 2000;
        }

        public void AfterTeleportPlayer(Player player)
        {
            PlayerGUID = player;
        }

        public override void UpdateAI(uint diff)
        {
            if (PongTimer <= diff)
            {
                if (Pullplayers)
                {
                    List<Unit> players = new List<Unit>();
                    AnyPlayerInObjectRangeCheck checker = new AnyPlayerInObjectRangeCheck(me, 150.0f);
                    PlayerListSearcher searcher = new PlayerListSearcher(me, players, checker);
                    Cell.VisitWorldObjects(me, searcher, 150f);

                    foreach (var item in players)
                    {
                        item.AddAura(boss_vanessa_vancleef.Spells.SPELL_EFFECT_1, item);
                        item.NearTeleportTo(-205.7569f, -579.0972f, 42.98623f, 2.3f);
                    }

                    me.Whisper(boss_vanessa_vancleef.VANESSA_NIGHTMARE_6, PlayerGUID, true);
                    me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(3000));

                    if (!me.FindNearestPlayer(50))
                    {
                        Pullplayers = false;
                    }

                }
                if (Csummon)
                {
                    me.SummonCreature(DMCreatures.NPC_VANESSA_NIGHTMARE, VanessaNightmare1, TempSummonType.ManualDespawn);
                    me.SummonCreature(DMCreatures.NPC_GLUBTOK_NIGHTMARE, GlubtokNightmare1, TempSummonType.ManualDespawn);
                    Csummon = false;
                }

            }
            else
            {
                PongTimer -= diff;
            }
        }
    }
}
