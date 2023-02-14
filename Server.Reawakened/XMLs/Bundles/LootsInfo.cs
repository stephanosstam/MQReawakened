﻿using Server.Base.Core.Extensions;
using Server.Reawakened.XMLs.Abstractions;
using System.Xml;

namespace Server.Reawakened.XMLs.Bundles;

internal class LootsInfo : LootsInfoXML, IBundledXml
{
    public string BundleName => "LootsInfo";

    public void InitializeVariables()
    {
        _rootXmlName = BundleName;
        _hasLocalizationDict = false;

        this.SetField<LootsInfoXML>("_lootsInfoXMLDict", new Dictionary<int, LootsInfoInfo>());
    }

    public void EditXml(XmlDocument xml)
    {
    }

    public void ReadXml(string xml) => ReadDescriptionXml(xml);

    public void FinalizeBundle()
    {
    }
}