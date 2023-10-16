// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.DragonIsles.ZoneTheForbiddenReach
{
    [Script] // 369728 - Dracthyr Login
    class spell_dracthyr_login : SpellScript
    {
        const uint SpellDracthyrMovieRoom01 = 394245; // scene for room 1
        const uint SpellDracthyrMovieRoom02 = 394279; // scene for room 2
        const uint SpellDracthyrMovieRoom03 = 394281; // scene for room 3
        const uint SpellDracthyrMovieRoom04 = 394282; // scene for room 4

        (uint, Position)[] LoginRoomData =
        {
            (SpellDracthyrMovieRoom01, new(5725.32f, -3024.26f, 251.047f, 0.01745329238474369f)),
            (SpellDracthyrMovieRoom02, new(5743.03f, -3067.28f, 251.047f, 0.798488140106201171f)),
            (SpellDracthyrMovieRoom03, new(5787.1597f, -3083.3906f, 251.04698f, 1.570796370506286621f)),
            (SpellDracthyrMovieRoom04, new(5829.32f, -3064.49f, 251.047f, 2.364955902099609375f))
        };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellDracthyrMovieRoom01, SpellDracthyrMovieRoom02, SpellDracthyrMovieRoom03, SpellDracthyrMovieRoom04);
        }

        void HandleTeleport(uint effIndex)
        {
            var (spellId, pos) = LoginRoomData[RandomHelper.URand(0, 3)];

            WorldLocation dest = GetHitUnit().GetWorldLocation();
            SetExplTargetDest(dest);

            GetHitDest().Relocate(pos);

            GetCaster().CastSpell(GetHitUnit(), spellId, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleTeleport, 0, SpellEffectName.TeleportUnits));
        }
    }

    [Script] // 3730 - Dracthyr Evoker Intro (Post Movie)
    class scene_dracthyr_evoker_intro : SceneScript
    {
        const uint SpellStasis1 = 369735; // triggers 366620'

        public scene_dracthyr_evoker_intro() : base("scene_dracthyr_evoker_intro") { }

        public override void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            player.CastSpell(player, SpellStasis1, true);
        }

        public override void OnSceneCancel(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            player.CastSpell(player, SpellStasis1, true);
        }
    }
}