// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IScene;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.DragonIsles
{
    internal struct SpellIds
    {
        // Spells
        public const uint DracthyrLogin = 369728;       // teleports to random room, plays scene for the room, binds the home position
        public const uint Stasis1 = 369735;             // triggers 366620
        public const uint Stasis2 = 366620;             // triggers 366636
        public const uint Stasis3 = 366636;             // removes 365560, sends first quest (64864)
        public const uint Stasis4 = 365560;             // freeze the Target
        public const uint DracthyrMovieRoom01 = 394245; // scene for room 1
        public const uint DracthyrMovieRoom02 = 394279; // scene for room 2
        public const uint DracthyrMovieRoom03 = 394281; // scene for room 3

        public const uint DracthyrMovieRoom04 = 394282; // scene for room 4
                                                        //public const uint DracthyrMovieRoom05    = 394283, // scene for room 5 (only plays sound, unused?)
    }

    internal struct MiscConst
    {
        public static Tuple<uint, Position>[] LoginRoomData =
        {
            Tuple.Create(SpellIds.DracthyrMovieRoom01, new Position(5725.32f, -3024.26f, 251.047f, 0.01745329238474369f)), Tuple.Create(SpellIds.DracthyrMovieRoom02, new Position(5743.03f, -3067.28f, 251.047f, 0.798488140106201171f)), Tuple.Create(SpellIds.DracthyrMovieRoom03, new Position(5787.1597f, -3083.3906f, 251.04698f, 1.570796370506286621f)), Tuple.Create(SpellIds.DracthyrMovieRoom04, new Position(5829.32f, -3064.49f, 251.047f, 2.364955902099609375f))
        };
    }

    [Script] // 369728 - Dracthyr Login
    internal class spell_dracthyr_login : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DracthyrMovieRoom01, SpellIds.DracthyrMovieRoom02, SpellIds.DracthyrMovieRoom03, SpellIds.DracthyrMovieRoom04);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleTeleport, 0, SpellEffectName.TeleportUnits, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleTeleport(int effIndex)
        {
            var room = MiscConst.LoginRoomData[RandomHelper.URand(0, 3)];

            WorldLocation dest = GetHitUnit().GetWorldLocation();
            SetExplTargetDest(dest);

            GetHitDest().Relocate(room.Item2);

            GetCaster().CastSpell(GetHitUnit(), room.Item1, true);
        }
    }

    [Script] // 3730 - Dracthyr Evoker Intro (Post Movie)
    internal class scene_dracthyr_evoker_intro : ScriptObjectAutoAddDBBound, ISceneOnSceneChancel, ISceneOnSceneComplete
    {
        public scene_dracthyr_evoker_intro() : base("scene_dracthyr_evoker_intro")
        {
        }

        public void OnSceneCancel(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            player.CastSpell(player, SpellIds.Stasis1, true);
        }

        public void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            player.CastSpell(player, SpellIds.Stasis1, true);
        }
    }
}