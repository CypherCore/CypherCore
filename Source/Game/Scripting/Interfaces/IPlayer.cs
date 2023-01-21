using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Chat;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Spells;

namespace Game.Scripting.Interfaces
{


    public interface IPlayerOnPVPKill
    {

        void OnPVPKill(Player killer, Player killed);

    }

    // Called when a player kills a creature
    public interface IPlayerOnCreatureKill
    {
        void OnCreatureKill(Player killer, Creature killed);

    }

    // Called when a player is killed by a creature
    public interface IPlayerOnPlayerKilledByCreature
    {
        void OnPlayerKilledByCreature(Creature killer, Player killed);

    }

    // Called when a player's level changes (after the level is applied);
    public interface IPlayerOnLevelChanged
    {
        void OnLevelChanged(Player player, byte oldLevel);

    }

    // Called when a player's free talent points change (right before the change is applied);
    public interface IPlayerOnFreeTalentPointsChanged
    {
        void OnFreeTalentPointsChanged(Player player, uint points);

    }

    // Called when a player's talent points are reset (right before the reset is done);
    public interface IPlayerOnTalentsReset
    {

        void OnTalentsReset(Player player, bool noCost);
    }

    // Called when a player's money is modified (before the modification is done);
    public interface IPlayerOnMoneyChanged
    {
        void OnMoneyChanged(Player player, long amount);

    }

    // Called when a player gains XP (before anything is given);
    public interface IPlayerOnGiveXP
    {

        void OnGiveXP(Player player, uint amount, Unit victim);
    }

    // Called when a player's reputation changes (before it is actually changed);
    public interface IPlayerOnReputationChange
    {
        void OnReputationChange(Player player, uint factionId, int standing, bool incremental);

    }

    // Called when a duel is requested
    public interface IPlayerOnDuelRequest
    {
        void OnDuelRequest(Player target, Player challenger);

    }

    // Called when a duel starts (after 3s countdown);
    public interface IPlayerOnDuelStart
    {
        void OnDuelStart(Player player1, Player player2);

    }

    // Called when a duel ends
    public interface IPlayerOnDuelEnd
    {
        void OnDuelEnd(Player winner, Player loser, DuelCompleteType type);

    }

    // The following methods are called when a player sends a chat message.
    public interface IPlayerOnChat
    {
        void OnChat(Player player, ChatMsg type, Language lang, string msg);

    }

    public interface IPlayerOnChatWhisper
    {
        void OnChat(Player player, ChatMsg type, Language lang, string msg, Player receiver);

    }

    public interface IPlayerOnChatGroup
    {
        void OnChat(Player player, ChatMsg type, Language lang, string msg, Group group);

    }

    public interface IPlayerOnChatGuild
    {
        void OnChat(Player player, ChatMsg type, Language lang, string msg, Guild guild);

    }

    public interface IPlayerOnChatChannel
    {
        void OnChat(Player player, ChatMsg type, Language lang, string msg, Channel channel);

    }

    // Both of the below are called on emote opcodes.
    public interface IPlayerOnClearEmote
    {
        void OnClearEmote(Player player);

    }

    public interface IPlayerOnTextEmote
    {
        void OnTextEmote(Player player, uint textEmote, uint emoteNum, ObjectGuid guid);

    }

    // Called in Spell.Cast.
    public interface IPlayerOnSpellCast
    {
        void OnSpellCast(Player player, Spells.Spell spell, bool skipCheck);

    }

    // Called when a player logs in.
    public interface IPlayerOnLogin
    {
        void OnLogin(Player player);

    }

    // Called when a player logs out.
    public interface IPlayerOnLogout
    {
        void OnLogout(Player player);

    }

    // Called when a player is created.
    public interface IPlayerOnCreate
    {
        void OnCreate(Player player);

    }

    // Called when a player is deleted.
    public interface IPlayerOnDelete
    {
        void OnDelete(ObjectGuid guid, uint accountId);

    }

    // Called when a player delete failed
    public interface IPlayerOnFailedDelete
    {
        void OnFailedDelete(ObjectGuid guid, uint accountId);

    }

    // Called when a player is about to be saved.
    public interface IPlayerOnSave
    {
        void OnSave(Player player);

    }

    // Called when a player is bound to an instance
    public interface IPlayerOnBindToInstance
    {
        void OnBindToInstance(Player player, Difficulty difficulty, uint mapId, bool permanent, byte extendState);

    }

    // Called when a player switches to a new zone
    public interface IPlayerOnUpdateZone
    {
        void OnUpdateZone(Player player, uint newZone, uint newArea);

    }

    // Called when a player changes to a new map (after moving to new map);
    public interface IPlayerOnMapChanged
    {
        void OnMapChanged(Player player);

    }

    // Called after a player's quest status has been changed
    public interface IPlayerOnQuestStatusChange
    {
        void OnQuestStatusChange(Player player, uint questId);

    }

    // Called when a player presses release when he died
    public interface IPlayerOnPlayerRepop
    {
        void OnPlayerRepop(Player player);

    }

    // Called when a player completes a movie
    public interface IPlayerOnMovieComplete
    {
        void OnMovieComplete(Player player, uint movieId);

    }

    // Called when a player choose a response from a PlayerChoice
    public interface IPlayerOnPlayerChoiceResponse
    {
        void OnPlayerChoiceResponse(Player player, uint choiceId, uint responseId);
    }

}

