﻿using Microsoft.Extensions.Logging;
using Server.Reawakened.Levels.Models.Entities;
using Server.Reawakened.Levels.Services;
using Server.Reawakened.Network.Protocols;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Extensions;
using Server.Reawakened.Players.Helpers;

namespace Protocols.External._l__ExtLevelEditor;

public class LevelUpdate : ExternalProtocol
{
    public override string ProtocolName => "lv";
    
    public ILogger<LevelUpdate> Logger { get; set; }
    public LevelHandler LevelHandler { get; set; }

    public override void Run(string[] message)
    {
        var player = NetState.Get<Player>();
        var level = player.GetCurrentLevel(LevelHandler);

        if (level == null)
            return;

        var gameObjectStore = GetGameObjectStore(level.LevelEntityHandler.Entities);

        SendXt("lv", 0, gameObjectStore);

        foreach (var entity in level.LevelEntityHandler.Entities.Values.SelectMany(x => x))
            entity.SendDelayedData(NetState);

        player.GetCurrentLevel(LevelHandler).SendCharacterInfo(player, NetState);
    }

    private string GetGameObjectStore(Dictionary<int, List<BaseSyncedEntity>> entities)
    {
        var sb = new SeparatedStringBuilder('&');

        foreach (var gameObject in entities.Select(GetGameObject)
                     .Where(gameObject => gameObject.Split('~').Length > 1))
            sb.Append(gameObject);

        return sb.ToString();
    }

    private string GetGameObject(KeyValuePair<int, List<BaseSyncedEntity>> entities)
    {
        var sb = new SeparatedStringBuilder('|');

        sb.Append(entities.Key);

        foreach (var entity in entities.Value)
            sb.Append(GetComponent(entity));

        return sb.ToString();
    }

    private string GetComponent(BaseSyncedEntity entity)
    {
        var sb = new SeparatedStringBuilder('~');

        sb.Append(entity.Name);

        foreach (var setting in entity.GetInitData(NetState))
            sb.Append(setting);

        return sb.ToString();
    }
}