﻿using Server.Base.Network;
using Server.Reawakened.Players;
using Server.Reawakened.Rooms.Extensions;
using Server.Reawakened.Rooms.Models.Entities;

namespace Server.Reawakened.Entities;

public class IdolControllerEntity : SyncedEntity<IdolController>
{
    public int Index => EntityData.Index;

    public override object[] GetInitData(NetState netState)
    {
        var player = netState.Get<Player>();
        var character = player.Character;
        var levelId = Room.LevelInfo.LevelId;

        if (!character.CollectedIdols.ContainsKey(levelId))
            character.CollectedIdols.Add(levelId, new List<int>());

        return character.CollectedIdols[levelId].Contains(Index) ? new object[] { 0 } : Array.Empty<object>();
    }

    public override void RunSyncedEvent(SyncEvent syncEvent, NetState netState)
    {
        var player = netState.Get<Player>();
        var character = player.Character;
        var levelId = Room.LevelInfo.LevelId;

        if (character.CollectedIdols[levelId].Contains(Index))
            return;

        character.CollectedIdols[levelId].Add(Index);
        Room.SentEntityTriggered(Id, player, true, true);
    }
}
