using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player completes a movie
    public interface IPlayerOnMovieComplete : IScriptObject
    {
        void OnMovieComplete(Player player, uint movieId);
    }
}