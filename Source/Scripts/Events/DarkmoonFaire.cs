// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.Events.DarkmoonFaire
{
    struct GossipIds
    {
        public const uint MenuSelinaPois = 13076;
        public const uint MenuSelinaItem = 13113;

        public const uint MenuOptionTonkArenaPoi = 0;
        public const uint MenuOptionCannonPoi = 1;
        public const uint MenuOptionWhackAGnollPoi = 2;
        public const uint MenuOptionRingTossPoi = 3;
        public const uint MenuOptionShootingGalleryPoi = 4;
        public const uint MenuOptionFortuneTellerPoi = 5;
    }

    struct PoiIds
    {
        public const uint WhackAGnoll = 2716;
        public const uint Cannon = 2717;
        public const uint ShootingGallery = 2718;
        public const uint TonkArena = 2719;
        public const uint FortuneTeller = 2720;
        public const uint RingToss = 2721;
    }

    [Script] // 10445 - Selina Dourman
    class npc_selina_dourman : ScriptedAI
    {
        const uint SpellReplaceDarkmoonAdventuresGuide = 103413;
        const uint SayWelcome = 0;

        bool _talkCooldown;

        public npc_selina_dourman(Creature creature) : base(creature) { }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            switch (menuId)
            {
                case GossipIds.MenuSelinaPois:
                {
                    uint poiId = 0;
                    switch (gossipListId)
                    {
                        case GossipIds.MenuOptionTonkArenaPoi:
                            poiId = PoiIds.TonkArena;
                            break;
                        case GossipIds.MenuOptionCannonPoi:
                            poiId = PoiIds.Cannon;
                            break;
                        case GossipIds.MenuOptionWhackAGnollPoi:
                            poiId = PoiIds.WhackAGnoll;
                            break;
                        case GossipIds.MenuOptionRingTossPoi:
                            poiId = PoiIds.RingToss;
                            break;
                        case GossipIds.MenuOptionShootingGalleryPoi:
                            poiId = PoiIds.ShootingGallery;
                            break;
                        case GossipIds.MenuOptionFortuneTellerPoi:
                            poiId = PoiIds.FortuneTeller;
                            break;
                        default:
                            break;
                    }
                    if (poiId != 0)
                        player.PlayerTalkClass.SendPointOfInterest(poiId);
                    break;
                }
                case GossipIds.MenuSelinaItem:
                    me.CastSpell(player, SpellReplaceDarkmoonAdventuresGuide);
                    player.CloseGossipMenu();
                    break;
                default:
                    break;
            }

            return false;
        }

        public void DoWelcomeTalk(Unit talkTarget)
        {
            if (talkTarget == null || _talkCooldown)
                return;

            _talkCooldown = true;
            _scheduler.Schedule(TimeSpan.FromSeconds(30), _ => _talkCooldown = false);
            Talk(SayWelcome, talkTarget);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script] // 7016 - Darkmoon Faire Entrance
    class at_darkmoon_faire_entrance : AreaTriggerScript
    {
        const uint NpcSelinaDourman = 10445;

        public at_darkmoon_faire_entrance() : base("at_darkmoon_faire_entrance") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
        {
            Creature selinaDourman = player.FindNearestCreature(NpcSelinaDourman, 50.0f);
            if (selinaDourman != null)
            {
                npc_selina_dourman selinaDourmanAI = selinaDourman.GetAI<npc_selina_dourman>();
                if (selinaDourmanAI != null)
                    selinaDourmanAI.DoWelcomeTalk(player);
            }

            return true;
        }
    }
}