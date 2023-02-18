// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair
{
    internal struct SpellIds
    {
        // These spells are actually called elemental shield
        // What they do is decrease all Damage by 75% then they increase
        // One school of Damage by 1100%
        public const uint FireVulnerability = 22277;
        public const uint FrostVulnerability = 22278;
        public const uint ShadowVulnerability = 22279;
        public const uint NatureVulnerability = 22280;

        public const uint ArcaneVulnerability = 22281;

        // Other spells
        public const uint Incinerate = 23308;    //Incinerate 23308; 23309
        public const uint Timelapse = 23310;     //Time lapse 23310; 23311(old threat mod that was removed in 2.01)
        public const uint Corrosiveacid = 23313; //Corrosive Acid 23313; 23314
        public const uint Igniteflesh = 23315;   //Ignite Flesh 23315; 23316

        public const uint Frostburn = 23187; //Frost burn 23187; 23189

        // Brood Affliction 23173 - Scripted Spell that cycles through all targets within 100 yards and has a chance to cast one of the afflictions on them
        // Since Scripted spells arn't coded I'll just write a function that does the same thing
        public const uint BroodafBlue = 23153;   //Blue affliction 23153
        public const uint BroodafBlack = 23154;  //Black affliction 23154
        public const uint BroodafRed = 23155;    //Red affliction 23155 (23168 on death)
        public const uint BroodafBronze = 23170; //Bronze Affliction  23170
        public const uint BroodafGreen = 23169;  //Brood Affliction Green 23169
        public const uint ChromaticMut1 = 23174; //Spell cast on player if they get all 5 debuffs
        public const uint Frenzy = 28371;        //The frenzy spell may be wrong
        public const uint Enrage = 28747;
    }

    internal struct TextIds
    {
        public const uint EmoteFrenzy = 0;
        public const uint EmoteShimmer = 1;
    }

    [Script]
    internal class boss_chromaggus : BossAI
    {
        private readonly uint Breath1_Spell;
        private readonly uint Breath2_Spell;
        private uint CurrentVurln_Spell;
        private bool Enraged;

        public boss_chromaggus(Creature creature) : base(creature, DataTypes.Chromaggus)
        {
            Initialize();

            Breath1_Spell = 0;
            Breath2_Spell = 0;

            // Select the 2 breaths that we are going to use until despawned
            // 5 possiblities for the first breath, 4 for the second, 20 total possiblites
            // This way we don't end up casting 2 of the same breath
            // Tl Tl would be stupid
            switch (RandomHelper.URand(0, 19))
            {
                // B1 - Incin
                case 0:
                    Breath1_Spell = SpellIds.Incinerate;
                    Breath2_Spell = SpellIds.Timelapse;

                    break;
                case 1:
                    Breath1_Spell = SpellIds.Incinerate;
                    Breath2_Spell = SpellIds.Corrosiveacid;

                    break;
                case 2:
                    Breath1_Spell = SpellIds.Incinerate;
                    Breath2_Spell = SpellIds.Igniteflesh;

                    break;
                case 3:
                    Breath1_Spell = SpellIds.Incinerate;
                    Breath2_Spell = SpellIds.Frostburn;

                    break;

                // B1 - Tl
                case 4:
                    Breath1_Spell = SpellIds.Timelapse;
                    Breath2_Spell = SpellIds.Incinerate;

                    break;
                case 5:
                    Breath1_Spell = SpellIds.Timelapse;
                    Breath2_Spell = SpellIds.Corrosiveacid;

                    break;
                case 6:
                    Breath1_Spell = SpellIds.Timelapse;
                    Breath2_Spell = SpellIds.Igniteflesh;

                    break;
                case 7:
                    Breath1_Spell = SpellIds.Timelapse;
                    Breath2_Spell = SpellIds.Frostburn;

                    break;

                //B1 - Acid
                case 8:
                    Breath1_Spell = SpellIds.Corrosiveacid;
                    Breath2_Spell = SpellIds.Incinerate;

                    break;
                case 9:
                    Breath1_Spell = SpellIds.Corrosiveacid;
                    Breath2_Spell = SpellIds.Timelapse;

                    break;
                case 10:
                    Breath1_Spell = SpellIds.Corrosiveacid;
                    Breath2_Spell = SpellIds.Igniteflesh;

                    break;
                case 11:
                    Breath1_Spell = SpellIds.Corrosiveacid;
                    Breath2_Spell = SpellIds.Frostburn;

                    break;

                //B1 - Ignite
                case 12:
                    Breath1_Spell = SpellIds.Igniteflesh;
                    Breath2_Spell = SpellIds.Incinerate;

                    break;
                case 13:
                    Breath1_Spell = SpellIds.Igniteflesh;
                    Breath2_Spell = SpellIds.Corrosiveacid;

                    break;
                case 14:
                    Breath1_Spell = SpellIds.Igniteflesh;
                    Breath2_Spell = SpellIds.Timelapse;

                    break;
                case 15:
                    Breath1_Spell = SpellIds.Igniteflesh;
                    Breath2_Spell = SpellIds.Frostburn;

                    break;

                //B1 - Frost
                case 16:
                    Breath1_Spell = SpellIds.Frostburn;
                    Breath2_Spell = SpellIds.Incinerate;

                    break;
                case 17:
                    Breath1_Spell = SpellIds.Frostburn;
                    Breath2_Spell = SpellIds.Timelapse;

                    break;
                case 18:
                    Breath1_Spell = SpellIds.Frostburn;
                    Breath2_Spell = SpellIds.Corrosiveacid;

                    break;
                case 19:
                    Breath1_Spell = SpellIds.Frostburn;
                    Breath2_Spell = SpellIds.Igniteflesh;

                    break;
            }

            EnterEvadeMode();
        }

        public override void Reset()
        {
            _Reset();

            Initialize();
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(0),
                                (Action<Framework.Dynamic.TaskContext>)(                                task =>
                                {
                                    // Remove old vulnerabilty spell
                                    if (CurrentVurln_Spell != 0)
                                        me.RemoveAura(CurrentVurln_Spell);

                                    // Cast new random vulnerabilty on self
                                    uint spell = RandomHelper.RAND(SpellIds.FireVulnerability, SpellIds.FrostVulnerability, SpellIds.ShadowVulnerability, SpellIds.NatureVulnerability, SpellIds.ArcaneVulnerability);
                                    DoCast(me, spell);
                                    CurrentVurln_Spell = spell;
                                    Talk(TextIds.EmoteShimmer);
                                    task.Repeat(TimeSpan.FromSeconds(45));
                                }));

            _scheduler.Schedule(TimeSpan.FromSeconds(30),
                                task =>
                                {
                                    DoCastVictim(Breath1_Spell);
                                    task.Repeat(TimeSpan.FromSeconds(60));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(60),
                                task =>
                                {
                                    DoCastVictim(Breath2_Spell);
                                    task.Repeat(TimeSpan.FromSeconds(60));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(10),
                                task =>
                                {
                                    var players = me.GetMap().GetPlayers();

                                    foreach (var player in players)
                                        if (player)
                                        {
                                            DoCast(player, RandomHelper.RAND(SpellIds.BroodafBlue, SpellIds.BroodafBlack, SpellIds.BroodafRed, SpellIds.BroodafBronze, SpellIds.BroodafGreen), new CastSpellExtraArgs(true));

                                            if (player.HasAura(SpellIds.BroodafBlue) &&
                                                player.HasAura(SpellIds.BroodafBlack) &&
                                                player.HasAura(SpellIds.BroodafRed) &&
                                                player.HasAura(SpellIds.BroodafBronze) &&
                                                player.HasAura(SpellIds.BroodafGreen))
                                                DoCast(player, SpellIds.ChromaticMut1);
                                        }

                                    task.Repeat(TimeSpan.FromSeconds(10));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(15),
                                task =>
                                {
                                    DoCast(me, SpellIds.Frenzy);
                                    task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
                                });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            // Enrage if not already enraged and below 20%
            if (!Enraged &&
                HealthBelowPct(20))
            {
                DoCast(me, SpellIds.Enrage);
                Enraged = true;
            }

            DoMeleeAttackIfReady();
        }

        private void Initialize()
        {
            CurrentVurln_Spell = 0; // We use this to store our last vulnerabilty spell so we can remove it later
            Enraged = false;
        }
    }

    [Script]
    internal class go_chromaggus_lever : GameObjectAI
    {
        private readonly InstanceScript _instance;

        public go_chromaggus_lever(GameObject go) : base(go)
        {
            _instance = go.GetInstanceScript();
        }

        public override bool OnGossipHello(Player player)
        {
            if (_instance.GetBossState(DataTypes.Chromaggus) != EncounterState.Done &&
                _instance.GetBossState(DataTypes.Chromaggus) != EncounterState.InProgress)
            {
                _instance.SetBossState(DataTypes.Chromaggus, EncounterState.InProgress);

                Creature creature = _instance.GetCreature(DataTypes.Chromaggus);

                if (creature)
                    creature.GetAI().JustEngagedWith(player);

                GameObject go = _instance.GetGameObject(DataTypes.GoChromaggusDoor);

                if (go)
                    _instance.HandleGameObject(ObjectGuid.Empty, true, go);
            }

            me.SetFlag(GameObjectFlags.NotSelectable | GameObjectFlags.InUse);
            me.SetGoState(GameObjectState.Active);

            return true;
        }
    }
}