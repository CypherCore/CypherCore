// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.CollectionItemSetFavorite)]
        void HandleCollectionItemSetFavorite(CollectionItemSetFavorite collectionItemSetFavorite)
        {
            switch (collectionItemSetFavorite.Type)
            {
                case CollectionType.Toybox:
                    GetCollectionMgr().ToySetFavorite(collectionItemSetFavorite.Id, collectionItemSetFavorite.IsFavorite);
                    break;
                case CollectionType.Appearance:
                    {
                        var pair = GetCollectionMgr().HasItemAppearance(collectionItemSetFavorite.Id);
                        if (!pair.Item1 || pair.Item2)
                            return;

                        GetCollectionMgr().SetAppearanceIsFavorite(collectionItemSetFavorite.Id, collectionItemSetFavorite.IsFavorite);
                        break;
                    }
                case CollectionType.TransmogSet:
                    break;
                default:
                    break;
            }
        }
    }
}
