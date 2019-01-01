/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.Kalimdor
{
    [Script]
    class npc_lazy_peon : NullCreatureAI
    {
        public npc_lazy_peon(Creature creature) : base(creature) { }

        public override void InitializeAI()
        {
            me.SetWalk(true);

            scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(120000), task =>
            {
                GameObject Lumberpile = me.FindNearestGameObject(GoLumberpile, 20);
                if (Lumberpile)
                    me.GetMotionMaster().MovePoint(1, Lumberpile.GetPositionX() - 1, Lumberpile.GetPositionY(), Lumberpile.GetPositionZ());
                task.Repeat();
            });

            scheduler.Schedule(TimeSpan.FromMilliseconds(300000), task =>
            {
                me.HandleEmoteCommand(Emote.StateNone);
                me.GetMotionMaster().MovePoint(2, me.GetHomePosition());
                task.Repeat();
            });
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 1:
                    me.HandleEmoteCommand(Emote.StateWorkChopwood);
                    break;
                case 2:
                    DoCast(me, SpellBuffSleep);
                    break;
            }
        }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            if (spell.Id != SpellAwakenPeon)
                return;

            Player player = caster.ToPlayer();
            if (player && player.GetQuestStatus(QuestLazyPeons) == QuestStatus.Incomplete)
            {
                player.KilledMonsterCredit(me.GetEntry(), me.GetGUID());
                Talk(SaySpellHit, caster);
                me.RemoveAllAuras();
                GameObject Lumberpile = me.FindNearestGameObject(GoLumberpile, 20);
                if (Lumberpile)
                    me.GetMotionMaster().MovePoint(1, Lumberpile.GetPositionX() - 1, Lumberpile.GetPositionY(), Lumberpile.GetPositionZ());
            }
        }

        public override void UpdateAI(uint diff)
        {
            scheduler.Update(diff);

            //if (!UpdateVictim())
            //return;

            //DoMeleeAttackIfReady();
        }

        const int QuestLazyPeons = 37446;
        const int GoLumberpile = 175784;
        const uint SpellBuffSleep = 17743;
        const int SpellAwakenPeon = 19938;
        const int SaySpellHit = 0;

        TaskScheduler scheduler = new TaskScheduler();
    }

    [Script]
    class spell_voodoo : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellBrew, SpellGhostly, SpellHex1, SpellHex2, SpellHex3, SpellGrow, SpellLaunch);
        }

        void HandleDummy(uint effIndex)
        {
            uint spellid = RandomHelper.RAND(SpellBrew, SpellGhostly, RandomHelper.RAND(SpellHex1, SpellHex2, SpellHex3), SpellGrow, SpellLaunch);
            Unit target = GetHitUnit();
            if (target)
                GetCaster().CastSpell(target, spellid, false);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }

        const uint SpellBrew = 16712; // Special Brew
        const uint SpellGhostly = 16713; // Ghostly
        const uint SpellHex1 = 16707; // Hex
        const uint SpellHex2 = 16708; // Hex
        const uint SpellHex3 = 16709; // Hex
        const uint SpellGrow = 16711; // Grow
        const uint SpellLaunch = 16716; // Launch (Whee!)
    }
}
