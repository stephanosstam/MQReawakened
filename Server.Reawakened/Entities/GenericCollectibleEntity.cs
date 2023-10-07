﻿using Microsoft.Extensions.Logging;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Extensions;
using Server.Reawakened.Rooms.Extensions;
using Server.Reawakened.Rooms.Models.Entities;

namespace Server.Reawakened.Entities;

public class GenericCollectibleModel : SyncedEntity<GenericCollectible>
{
    public bool Collected;

    public int Value;
    public ILogger<GenericCollectibleModel> Logger { get; set; }

    public override void InitializeEntity()
    {
        switch (PrefabName)
        {
            case "BananaGrapCollectible":
                Value = 5;
                break;
            case "BananeCollectible":
                Value = 1;
                break;
            default:
                Logger.LogWarning("Collectible not implemented for {PrefabName}", PrefabName);
                break;
        }
    }

    public override object[] GetInitData(Player player) =>
        Collected ? [0] : Array.Empty<object>();

    public override void RunSyncedEvent(SyncEvent syncEvent, Player player)
    {
        Collected = true;
        var collectedValue = Value * Room.Players.Count;

        Room.SentEntityTriggered(Id, player, true, true);

        var effectName = string.Empty;

        switch (PrefabName)
        {
            case "BananaGrapCollectible":
                effectName = "PF_FX_Banana_Level_01";
                break;
            case "BananeCollectible":
                effectName = "PF_FX_Banana_Level_02";
                break;
            default:
                Logger.LogWarning("Collectible not implemented for {PrefabName}", PrefabName);
                break;
        }

        var effectEvent = new FX_SyncEvent(Id.ToString(), Room.Time, effectName,
            Position.X, Position.Y, FX_SyncEvent.FXState.Play);

        Room.SendSyncEvent(effectEvent);

        if (collectedValue <= 0)
            return;

        foreach (var currentPlayer in Room.Players.Values)
            currentPlayer.AddBananas(collectedValue);
    }
}
