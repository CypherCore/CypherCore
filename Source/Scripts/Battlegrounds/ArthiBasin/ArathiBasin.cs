// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Battlegrounds.ArthiBasin
{
    [Script]
    [Script("npc_bg_ab_arathor_gryphon_rider_leader", PathGryphonRiderLeader)]
    [Script("npc_bg_ab_defiler_bat_rider_leader", PathBatRiderLeader)]
    class npc_bg_ab_gryphon_bat_rider_leader : ScriptedAI
    {
        const uint PathGryphonRiderLeader = 800000059;
        const uint PathBatRiderLeader = 800000058;

        uint _pathId;

        public npc_bg_ab_gryphon_bat_rider_leader(Creature creature, uint pathId) : base(creature)
        {
            _pathId = pathId;
        }

        public override void WaypointPathEnded(uint nodeId, uint pathId)
        {
            if (pathId != _pathId)
                return;

            // despawn formation group
            List<Creature> followers = me.GetCreatureListWithEntryInGrid(me.GetEntry());
            foreach (Creature follower in followers)
                follower.DespawnOrUnsummon(TimeSpan.FromMilliseconds(500));

            me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(500));
        }
    }

    [Script] // 261985 - Blacksmith Working
    class spell_bg_ab_blacksmith_working : AuraScript
    {
        const uint ItemBlacksmithHammer = 5956;

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().SetVirtualItem(0, ItemBlacksmithHammer);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Creature creature = GetTarget().ToCreature();
            if (creature != null)
                creature.LoadEquipment(creature.GetOriginalEquipmentId());
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }
}