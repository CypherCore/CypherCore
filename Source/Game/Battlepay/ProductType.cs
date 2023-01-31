using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Battlepay
{
    public enum ProductType
    {
        // retail values:
        Item_ = 0,
        LevelBoost = 1,
        Pet = 2,
        Mount = 3,
        WoWToken = 4,
        NameChange = 5,
        FactionChange = 6,
        RaceChange = 8,
        CharacterTransfer = 11,
        Toy = 14,
        Expansion = 18,
        GameTime = 20,
        GuildNameChange = 21,
        GuildFactionChange = 22,
        GuildTransfer = 23,
        GuildFactionTranfer = 24,
        TransmogAppearance = 26,
        Gold = 30,
        Currency = 31,
        // custom values:
        ItemSet = 100,
        Heirloom = 101,
        ProfPriAlchemy = 118,
        ProfPriSastre = 119,
        ProfPriJoye = 120,
        ProfPriHerre = 121,
        ProfPriPele = 122,
        ProfPriInge = 123,
        ProfPriInsc = 124,
        ProfPriEncha = 125,
        ProfPriDesu = 126,
        ProfPriMing = 127,
        ProfPriHerb = 128,
        ProfSecCoci = 129,
        Promo = 140,
        RepClassic = 141,
        RepBurnig = 142,
        RepTLK = 143,
        RepCata = 144,
        RepPanda = 145,
        RepDraenor = 146,
        RepLegion = 147,
        PremadePve = 149,
        VueloDL = 150,
    }
}
