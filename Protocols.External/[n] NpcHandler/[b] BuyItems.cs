﻿using Server.Reawakened.Network.Protocols;
using Server.Reawakened.Players.Extensions;
using Server.Reawakened.XMLs.Bundles;

namespace Protocols.External._n__NpcHandler;

public class BuyItems : ExternalProtocol
{
    public override string ProtocolName => "nb";

    public ItemCatalog ItemCatalog { get; set; }

    public override void Run(string[] message)
    {
        var character = Player.Character;

        //var vendorId = int.Parse(message[5]);
        //var vendorGoId = int.Parse(message[7]);
        var items = message[6].Split('|');
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item)) continue;

            var args = item.Split(":");
            var itemId = int.Parse(args[0]);
            var amount = int.Parse(args[1]);

            var itemInfo = ItemCatalog.GetItemFromId(itemId);

            character.AddItem(itemInfo, amount);

            if (itemInfo.Currency == A2m.Server.CurrencyType.Banana)
            {
                Player.RemoveBananas(itemInfo.RegularPrice * amount);
            }
            else if (itemInfo.Currency == A2m.Server.CurrencyType.NickCash)
            {
                Player.RemoveNCash(itemInfo.RegularPrice * amount);
            }
            
        }

        Player.SendUpdatedInventory(false);
    }
}
