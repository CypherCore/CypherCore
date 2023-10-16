// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.DragonIsles.AberrusTheShadowedCrucible.KazzaraTheHellforged
{
    struct SpellIds
    {
        // Sundered NPCs
        public const uint Fear = 220540;

        // Kazzara
        public const uint DreadLanding = 411872;
        public const uint KazzaraIntro = 410541;
    }

    [Script] // 201261 - Kazzara the Hellforged
    class boss_kazzara_the_hellforged : BossAI
    {
        public boss_kazzara_the_hellforged(Creature creature) : base(creature, DataTypes.KazzaraTheHellforged) { }

        public override void JustAppeared()
        {
            if (instance.GetData(DataTypes.KazzaraIntroDone) == 0)
            {
                me.SetUninteractible(true);
                me.SetImmuneToAll(true);
                me.SetVisible(false);
            }
        }

        public override void DoAction(int actionId)
        {
            switch (actionId)
            {
                case MiscConst.ActionStartKazzaraIntro:
                {
                    GameObject gate = instance.GetGameObject(DataTypes.KazzaraGate);
                    if (gate != null)
                    {
                        gate.SetFlag(GameObjectFlags.InUse);
                        gate.SetGoState(GameObjectState.Ready);
                    }

                    me.SetVisible(true);

                    DoCast(SpellIds.DreadLanding);
                    DoCast(SpellIds.KazzaraIntro);

                    _scheduler.Schedule(TimeSpan.FromSeconds(1) + TimeSpan.FromMilliseconds(500), _ =>
                    {
                        List<Creature> sunderedMobs = me.GetCreatureListWithOptionsInGrid(50.0f, new FindCreatureOptions() { StringId = "sundered_mob" });
                        foreach (Creature sunderedMob in sunderedMobs)
                        {
                            if (!sunderedMob.IsAlive() || sunderedMob.IsInCombat())
                                continue;

                            sunderedMob.CastSpell(null, SpellIds.Fear, false);
                        }
                    });

                    _scheduler.Schedule(TimeSpan.FromSeconds(12), _ =>
                    {
                        me.SetUninteractible(false);
                        me.SetImmuneToAll(false);
                    });
                    break;
                }
                default:
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }
}