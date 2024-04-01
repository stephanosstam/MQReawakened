﻿using Achievement.StaticData;
using Microsoft.Extensions.Logging;
using Server.Reawakened.Players;
using Server.Reawakened.XMLs.Abstractions;
using Server.Reawakened.XMLs.Bundles;
using Server.Reawakened.XMLs.Enums;
using Server.Reawakened.XMLs.Extensions;
using System.Xml;

namespace Server.Reawakened.XMLs.BundlesInternal;

public class InternalEventReward : IBundledXml
{
    public string BundleName => "InternalEventReward";
    public BundlePriority Priority => BundlePriority.Lowest;

    public ILogger<InternalEventReward> Logger { get; set; }
    public ItemCatalog ItemCatalog { get; set; }

    public Dictionary<int, List<AchievementDefinitionRewards>> EventRewards;

    public void InitializeVariables() => EventRewards = [];

    public void EditDescription(XmlDocument xml)
    {
    }

    public void ReadDescription(string xml)
    {
        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xml);

        foreach (XmlNode eventRewardXml in xmlDocument.ChildNodes)
        {
            if (eventRewardXml.Name != "EventRewards") continue;

            foreach (XmlNode tEvent in eventRewardXml.ChildNodes)
            {
                if (tEvent.Name != "Event") continue;

                var eventId = -1;

                foreach (XmlAttribute eventAttribute in tEvent.Attributes)
                    switch (eventAttribute.Name)
                    {
                        case "eventId":
                            eventId = int.Parse(eventAttribute.Value);
                            continue;
                    }

                var rewards = tEvent.GetXmlRewards(Logger, ItemCatalog);

                EventRewards.Add(eventId, rewards);
            }
        }
    }

    public void FinalizeBundle()
    {
    }

    public void CheckEventReward(int eventId, Player player, InternalAchievement internalAchievement)
    {
        if (!EventRewards.TryGetValue(eventId, out var eventRewards))
            return;

        eventRewards.RewardPlayer(player, internalAchievement, Logger);
    }
}
