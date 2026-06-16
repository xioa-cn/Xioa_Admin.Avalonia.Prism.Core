using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Ava.Xioa.Common.Models;

/// <summary>
/// 模块配置根节点
/// </summary>
[XmlRoot("configuration")]
public class ModulesConfiguration {
    [XmlAttribute("name")] public string Name { get; set; }

    [XmlElement("modules")] public ModulesSection Modules { get; set; }
    
    
    public static ModulesConfiguration? LoadConfiguration(string configFile) {
        var serializer = new XmlSerializer(typeof(ModulesConfiguration));

        using var reader = new StreamReader(configFile);
        if (reader == null)
            throw new FileNotFoundException();
        var result = serializer?.Deserialize(reader) as ModulesConfiguration;

        return result;
    }
}

/// <summary>
/// 模块节
/// </summary>
public class ModulesSection {
    [XmlElement("module")] public List<ModuleElement> Modules { get; set; }
}

/// <summary>
/// 模块元素
/// </summary>
public class ModuleElement {
    [XmlAttribute("assemblyFile")] public string AssemblyFile { get; set; }

    [XmlElement("moduleName")] public List<string> ModuleNames { get; set; }
}