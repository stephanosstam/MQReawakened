﻿using Server.Reawakened.Configs;
using Server.Reawakened.Network.Protocols;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Extensions;
using Server.Reawakened.XMLs.Bundles;

namespace Protocols.External._n__NpcHandler;

public class SetActiveQuest : ExternalProtocol
{
    public override string ProtocolName => "nz";

    public ServerConfig Config { get; set; }
    public QuestCatalog QuestCatalog { get; set; }

    public override void Run(string[] message)
    {
        var player = NetState.Get<Player>();
        var character = player.GetCurrentCharacter();

        if (character == null)
            return;

        var activeQuest = int.Parse(message[5]);

        if (activeQuest == -1)
            activeQuest = Config.DefaultQuest;

        if (character.ActiveQuestId == activeQuest || character.CompletedQuests.Contains(activeQuest))
            return;

        player.AddQuest(activeQuest, true, NetState, QuestCatalog);
    }
}