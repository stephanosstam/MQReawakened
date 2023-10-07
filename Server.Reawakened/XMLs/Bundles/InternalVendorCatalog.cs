﻿using Server.Reawakened.XMLs.Abstractions;
using Server.Reawakened.XMLs.Models;
using System.Xml;

namespace Server.Reawakened.XMLs.Bundles;

public class InternalVendorCatalog : IBundledXml
{
    public Dictionary<int, VendorInfo> VendorCatalog;
    public string BundleName => "InternalVendorCatalog";

    public void InitializeVariables() =>
        VendorCatalog = new Dictionary<int, VendorInfo>();

    public void EditDescription(XmlDocument xml)
    {
    }

    public void ReadDescription(string xml)
    {
        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xml);

        foreach (XmlNode childNode in xmlDocument.ChildNodes)
        {
            if (childNode.Name != "vendorCatalog") continue;

            foreach (XmlNode childNode2 in childNode.ChildNodes)
            {
                var objectId = -1;
                var nameId = -1;
                var descriptionId = -1;

                var numberOfIdolsToAccessBackStore = -1;
                var idolLevelId = -1;

                var vendorId = -1;
                var catalogId = -1;
                var vendorType = NPCController.NPCStatus.Unknown;

                var dialogId = -1;
                var greetingConversationId = -1;
                var leavingConversationId = -1;

                if (childNode2.Name == "vendor")
                    foreach (XmlAttribute item in childNode2.Attributes!)
                    {
                        switch (item.Name)
                        {
                            case "objectId":
                                objectId = int.Parse(item.Value);
                                continue;
                            case "nameId":
                                nameId = int.Parse(item.Value);
                                continue;
                            case "descriptionId":
                                descriptionId = int.Parse(item.Value);
                                continue;

                            case "numberOfIdolsToAccessBackStore":
                                numberOfIdolsToAccessBackStore = int.Parse(item.Value);
                                continue;
                            case "idolLevelId":
                                idolLevelId = int.Parse(item.Value);
                                continue;

                            case "vendorId":
                                vendorId = int.Parse(item.Value);
                                continue;
                            case "catalogId":
                                catalogId = int.Parse(item.Value);
                                continue;
                            case "vendorType":
                                vendorType = (NPCController.NPCStatus) int.Parse(item.Value);
                                continue;

                            case "dialogId":
                                dialogId = int.Parse(item.Value);
                                continue;
                            case "greetingConversationId":
                                greetingConversationId = int.Parse(item.Value);
                                continue;
                            case "leavingConversationId":
                                leavingConversationId = int.Parse(item.Value);
                                continue;
                        }
                    }

                if (VendorCatalog.ContainsKey(objectId))
                    continue;

                var greetingConversation = new Conversation(dialogId, greetingConversationId);
                var leavingConversation = new Conversation(dialogId, leavingConversationId);

                var vendor = new VendorInfo(
                    objectId, nameId, descriptionId,
                    numberOfIdolsToAccessBackStore, idolLevelId,
                    vendorId, catalogId, vendorType,
                    greetingConversation, leavingConversation
                );

                VendorCatalog.Add(objectId, vendor);
            }
        }
    }

    public void FinalizeBundle()
    {
    }

    public VendorInfo GetVendorById(int id) =>
        VendorCatalog.TryGetValue(id, out var vendor) ? vendor : null;
}