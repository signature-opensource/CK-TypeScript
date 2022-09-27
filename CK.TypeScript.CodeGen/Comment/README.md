## DocumentationBuilder limitations

The xml documentation file must currently be located nearby the assembly. Unfortunately, .net build or publish
doesn't copy the xml files: https://github.com/dotnet/sdk/issues/9498

There's one alternative:

- include in each csproj that requires the xml files to exist the msbuild target with something
  like below (from the issue) but this doesn't propagate to publish folder and ALL xml
  documentation files from the .net framework(s) are copied:
```xml
  <Target Name="CopyReferenceFiles" BeforeTargets="Build">
    <ItemGroup>
      <XmlReferenceFiles Condition="Exists('$(OutputPath)%(Filename).dll')" Include="%(Reference.RelativeDir)%(Reference.Filename).xml" />
    </ItemGroup>

    <Message Text="Copying reference files to $(OutputPath)" Importance="High" />
    <Copy SourceFiles="@(XmlReferenceFiles)" DestinationFolder="$(OutputPath)" Condition="Exists('%(RootDir)%(Directory)%(Filename)%(Extension)')" />
  </Target>
```
- lookup the cached nuget package folder path from the assembly if the assembly cannot be found locally.
  This may be done once...

