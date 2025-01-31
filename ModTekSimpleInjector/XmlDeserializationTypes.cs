using System.Xml.Serialization;
using Mono.Cecil;

namespace ModTekSimpleInjector;

// XmlSerializer requires the following classes to be public

[XmlRoot(ElementName = "ModTekSimpleInjector")]
public class Additions
{
    [XmlElement(ElementName = "AddField")]
    public AddField[] AddField = [];
    [XmlElement(ElementName = "AddEnumConstant")]
    public AddEnumConstant[] AddEnumConstant = [];
}

public abstract class AssemblyAddition
{
    [XmlAttribute("InAssembly")]
    public string InAssembly;
    [XmlAttribute("Comment")]
    public string Comment;

    public override string ToString()
    {
        return $"{this.GetType().Name}:{InAssembly}:{Comment}";
    }
}

public abstract class MemberAddition : AssemblyAddition
{
    [XmlAttribute("ToType")]
    public string ToType;

    public override string ToString()
    {
        return $"{base.ToString()}:{ToType}";
    }
}

[XmlType("AddField")]
[XmlRoot(ElementName = "AddField")]
public class AddField : MemberAddition
{
    [XmlAttribute("Name")]
    public string Name;
    [XmlAttribute("OfType")]
    public string OfType;
    [XmlAttribute("Attributes")]
    public FieldAttributes Attributes = FieldAttributes.Private;

    public override string ToString()
    {
        return $"{base.ToString()}:{Name}:{OfType}:{Attributes}";
    }
}

[XmlType("AddEnumConstant")]
[XmlRoot(ElementName = "AddEnumConstant")]
public class AddEnumConstant : MemberAddition
{
    [XmlAttribute("Name")]
    public string Name;
    [XmlAttribute("Value")]
    public int Value;

    public override string ToString()
    {
        return $"{base.ToString()}:{Name}:{Value}";
    }
}
