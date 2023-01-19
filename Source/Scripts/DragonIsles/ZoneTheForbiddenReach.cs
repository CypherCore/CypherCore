﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.DragonIsles
{
    struct SpellIds
    {
        // Spells
        public const uint DracthyrLogin = 369728; // teleports to random room, plays scene for the room, binds the home position
        public const uint Stasis1 = 369735; // triggers 366620
        public const uint Stasis2 = 366620; // triggers 366636
        public const uint Stasis3 = 366636; // removes 365560, sends first quest (64864)
        public const uint Stasis4 = 365560; // freeze the target
        public const uint DracthyrMovieRoom01 = 394245; // scene for room 1
        public const uint DracthyrMovieRoom02 = 394279; // scene for room 2
        public const uint DracthyrMovieRoom03 = 394281; // scene for room 3
        public const uint DracthyrMovieRoom04 = 394282; // scene for room 4
                                                        //public const uint DracthyrMovieRoom05    = 394283, // scene for room 5 (only plays sound, unused?)
    }

    struct MiscConst
    {
        public static Tuple<uint, Position>[] LoginRoomData =
        {
            Tuple.Create(SpellIds.DracthyrMovieRoom01, new Position(5725.32f, -3024.26f, 251.047f, 0.01745329238474369f)),
            Tuple.Create(SpellIds.DracthyrMovieRoom02, new Position( 5743.03f, -3067.28f, 251.047f, 0.798488140106201171f)),
            Tuple.Create(SpellIds.DracthyrMovieRoom03, new Position(5787.1597f, -3083.3906f, 251.04698f, 1.570796370506286621f)),
            Tuple.Create(SpellIds.DracthyrMovieRoom04, new Position(5829.32f, -3064.49f, 251.047f, 2.364955902099609375f))
        };
    }

    [Script] // 369728 - Dracthyr Login
    class spell_dracthyr_login : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DracthyrMovieRoom01, SpellIds.DracthyrMovieRoom02, SpellIds.DracthyrMovieRoom03, SpellIds.DracthyrMovieRoom04);
        }

        void HandleTeleport(uint effIndex)
        {
            var room = MiscConst.LoginRoomData[RandomHelper.URand(0, 3)];

            WorldLocation dest = GetHitUnit().GetWorldLocation();
            SetExplTargetDest(dest);

            GetHitDest().Relocate(room.Item2);

            GetCaster().CastSpell(GetHitUnit(), room.Item1, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleTeleport, 0, SpellEffectName.TeleportUnits));
        }
    }

    [Script] // 3730 - Dracthyr Evoker Intro (Post Movie)
    class scene_dracthyr_evoker_intro : SceneScript
    {
        public scene_dracthyr_evoker_intro() : base("scene_dracthyr_evoker_intro") { }

        public override void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            player.CastSpell(player, SpellIds.Stasis1, true);
        }

        public override void OnSceneCancel(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            player.CastSpell(player, SpellIds.Stasis1, true);
        }
    }
}
