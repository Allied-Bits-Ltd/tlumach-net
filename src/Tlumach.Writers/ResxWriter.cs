using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

using Tlumach.Base;

namespace Tlumach.Writers;

/// <summary>
/// A writer for the .NET .resx XML format.
/// </summary>
public class ResxWriter : BaseXmlWriter
{
    public override string FormatName => "RESX";

    public override string ConfigExtension => ".resxcfg";

    public override string TranslationExtension => ".resx";

    protected override void InternalWriteXmlTranslations(Translation translation, Stream stream)
    {
        if (translation is null)
            throw new ArgumentNullException(nameof(translation));

        XDocument doc = new();

        XElement root = new("root");
        doc.Add(root);

        // Add the xsd schema definition (standard resx format)
        AddSchemaDefinition(root);

        // Add resheader elements
        root.Add(
            new XElement(
                "resheader",
                new XAttribute("name", "resmimetype"),
                new XElement("value", "text/microsoft-resx")));

        root.Add(
            new XElement(
                "resheader",
                new XAttribute("name", "version"),
                new XElement("value", "2.0")));

        root.Add(
            new XElement(
                "resheader",
                new XAttribute("name", "reader"),
                new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")));

        root.Add(
            new XElement(
                "resheader",
                new XAttribute("name", "writer"),
                new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")));

        // Add data elements for translations
        List<TranslationEntry> entryList = GetSortedEntries(translation);

        foreach (var entry in entryList)
        {
            XElement dataElement = new("data");
            dataElement.SetAttributeValue("name", entry.Key);
            dataElement.SetAttributeValue("type", "System.String");

            // Add value element
            string valueText = ShouldWriteReference(entry) ? "@" + entry.Reference ?? string.Empty : entry.Text ?? string.Empty;
            XElement valueElement = new("value", valueText);

            // Add xml:space="preserve" if the value has leading or trailing whitespace
            if (!string.IsNullOrEmpty(valueText) &&
                (char.IsWhiteSpace(valueText[0]) || char.IsWhiteSpace(valueText[valueText.Length - 1])))
            {
                dataElement.SetAttributeValue(XNamespace.Xml + "space", "preserve");
            }

            dataElement.Add(valueElement);

            // Add comment if present
            if (!string.IsNullOrEmpty(entry.Comment))
            {
                XElement commentElement = new("comment", entry.Comment);
                dataElement.Add(commentElement);
            }

            root.Add(dataElement);
        }

        using (var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
        {
            doc.Save(writer, SaveOptions.None);
        }
    }

    private static void AddSchemaDefinition(XElement root)
    {
        XNamespace xsd = "http://www.w3.org/2001/XMLSchema";
        XNamespace msdata = "urn:schemas-microsoft-com:xml-msdata";

        XElement schema = new(
            xsd + "schema",
            new XAttribute("id", "root"),
            new XAttribute(XNamespace.Xmlns + "xsd", xsd),
            new XAttribute(XNamespace.Xmlns + "msdata", msdata));

        // Add import element
        schema.Add(
            new XElement(
                xsd + "import",
                new XAttribute("namespace", "http://www.w3.org/XML/1998/namespace")));

        // Add root element definition
        XElement rootElementDef = new(
            xsd + "element",
            new XAttribute("name", "root"),
            new XAttribute(msdata + "IsDataSet", "true"));

        XElement complexType = new(xsd + "complexType");
        XElement choice = new(
            xsd + "choice",
            new XAttribute("maxOccurs", "unbounded"));

        // metadata element definition
        XElement metadataElement = new(
            xsd + "element",
            new XAttribute("name", "metadata"));
        XElement metadataComplexType = new(xsd + "complexType");
        XElement metadataSequence = new(xsd + "sequence");
        metadataSequence.Add(
            new XElement(
                xsd + "element",
                new XAttribute("name", "value"),
                new XAttribute("type", "xsd:string"),
                new XAttribute("minOccurs", "0")));
        metadataComplexType.Add(metadataSequence);
        metadataComplexType.Add(
            new XElement(
                xsd + "attribute",
                new XAttribute("name", "name"),
                new XAttribute("use", "required"),
                new XAttribute("type", "xsd:string")));
        metadataComplexType.Add(
            new XElement(
                xsd + "attribute",
                new XAttribute("name", "type"),
                new XAttribute("type", "xsd:string")));
        metadataComplexType.Add(
            new XElement(
                xsd + "attribute",
                new XAttribute("name", "mimetype"),
                new XAttribute("type", "xsd:string")));
        metadataComplexType.Add(
            new XElement(
                xsd + "attribute",
                new XAttribute("ref", xsd + "space")));
        metadataElement.Add(metadataComplexType);
        choice.Add(metadataElement);

        // assembly element definition
        XElement assemblyElement = new(
            xsd + "element",
            new XAttribute("name", "assembly"));
        XElement assemblyComplexType = new(xsd + "complexType");
        assemblyComplexType.Add(
            new XElement(
                xsd + "attribute",
                new XAttribute("name", "alias"),
                new XAttribute("type", "xsd:string")));
        assemblyComplexType.Add(
            new XElement(
                xsd + "attribute",
                new XAttribute("name", "name"),
                new XAttribute("type", "xsd:string")));
        assemblyElement.Add(assemblyComplexType);
        choice.Add(assemblyElement);

        // data element definition
        XElement dataElement = new(
            xsd + "element",
            new XAttribute("name", "data"));
        XElement dataComplexType = new(xsd + "complexType");
        XElement dataSequence = new(xsd + "sequence");
        dataSequence.Add(
            new XElement(
                xsd + "element",
                new XAttribute("name", "value"),
                new XAttribute("type", "xsd:string"),
                new XAttribute("minOccurs", "0"),
                new XAttribute(msdata + "Ordinal", "1")));
        dataSequence.Add(
            new XElement(
                xsd + "element",
                new XAttribute("name", "comment"),
                new XAttribute("type", "xsd:string"),
                new XAttribute("minOccurs", "0"),
                new XAttribute(msdata + "Ordinal", "2")));
        dataComplexType.Add(dataSequence);
        dataComplexType.Add(
            new XElement(
                xsd + "attribute",
                new XAttribute("name", "name"),
                new XAttribute("type", "xsd:string"),
                new XAttribute("use", "required"),
                new XAttribute(msdata + "Ordinal", "1")));
        dataComplexType.Add(
            new XElement(
                xsd + "attribute",
                new XAttribute("name", "type"),
                new XAttribute("type", "xsd:string"),
                new XAttribute(msdata + "Ordinal", "3")));
        dataComplexType.Add(
            new XElement(
                xsd + "attribute",
                new XAttribute("name", "mimetype"),
                new XAttribute("type", "xsd:string"),
                new XAttribute(msdata + "Ordinal", "4")));
        dataComplexType.Add(
            new XElement(
                xsd + "attribute",
                new XAttribute("ref", xsd + "space")));
        dataElement.Add(dataComplexType);
        choice.Add(dataElement);

        // resheader element definition
        XElement resheaderElement = new(
            xsd + "element",
            new XAttribute("name", "resheader"));
        XElement resheaderComplexType = new(xsd + "complexType");
        XElement resheaderSequence = new(xsd + "sequence");
        resheaderSequence.Add(
            new XElement(
                xsd + "element",
                new XAttribute("name", "value"),
                new XAttribute("type", "xsd:string"),
                new XAttribute("minOccurs", "0"),
                new XAttribute(msdata + "Ordinal", "1")));
        resheaderComplexType.Add(resheaderSequence);
        resheaderComplexType.Add(
            new XElement(
                xsd + "attribute",
                new XAttribute("name", "name"),
                new XAttribute("type", "xsd:string"),
                new XAttribute("use", "required")));
        resheaderElement.Add(resheaderComplexType);
        choice.Add(resheaderElement);

        complexType.Add(choice);
        rootElementDef.Add(complexType);
        schema.Add(rootElementDef);

        root.Add(schema);
    }
}
