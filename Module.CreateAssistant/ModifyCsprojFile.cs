﻿using System.IO;
using System.Text;

namespace Module.CreateAssistant;

public partial class Program
{
    static void ModifyCsprojFile(string csprojPath)
    {
        if (!File.Exists(csprojPath))
        {
            throw new FileNotFoundException("未找到 .csproj 文件", csprojPath);
        }

        string csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <Nullable>enable</Nullable>
        <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
        <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    </PropertyGroup>

   <ItemGroup>
      <ProjectReference Include=""..\Ava.Xioa.Common\Ava.Xioa.Common.csproj"" />
      <ProjectReference Include=""..\Ava.Xioa.Connectlayer\Ava.Xioa.Connectlayer.csproj"" />
      <ProjectReference Include=""..\Ava.Xioa.Infrastructure.Services\Ava.Xioa.Infrastructure.Services.csproj"" />
      <ProjectReference Include=""..\ObservableBind.Generator\ObservableBind.Generator.csproj"" OutputItemType=""Analyzer"" ReferenceOutputAssembly=""false""/>
    </ItemGroup>
</Project>";

        File.WriteAllText(csprojPath, csprojContent, Encoding.UTF8);
    }
}