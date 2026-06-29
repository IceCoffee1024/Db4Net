using System.Xml.Linq;

namespace Db4Net.Tests;

public sealed class PackageMetadataTests
{
    [Fact]
    public void Package_metadata_contains_required_alpha_fields()
    {
        var project = XDocument.Load(Path.Combine(GetRepositoryRoot(), "src", "Db4Net", "Db4Net.csproj"));
        var properties = project.Root!.Elements("PropertyGroup").Elements().ToDictionary(element => element.Name.LocalName, element => element.Value);

        Assert.Equal("net8.0;netstandard2.0", properties["TargetFrameworks"]);
        Assert.Equal("latest", properties["LangVersion"]);
        Assert.Equal("Db4Net", properties["PackageId"]);
        Assert.Equal("0.1.0-alpha.1", properties["Version"]);
        Assert.Equal("IceCoffee1024", properties["Authors"]);
        Assert.Equal("Safe, SQL-shaped fluent query and command builder for Dapper.", properties["Description"]);
        Assert.Equal("0.1.0-alpha.1 adds SQL-shaped single-table SELECT/CUD builders, existence, count, scalar aggregate queries including sum and explicit-result average, entity and many conveniences, conflict-aware inserts, generated-column safeguards, paging validation, explicit filter groups, lightweight transaction scopes, net8.0/netstandard2.0 package assets, and bilingual documentation.", properties["PackageReleaseNotes"]);
        Assert.Equal("dapper;sql;fluent;query-builder", properties["PackageTags"]);
        Assert.Equal("README.md", properties["PackageReadmeFile"]);
        Assert.Equal("MIT", properties["PackageLicenseExpression"]);
        Assert.Equal("https://dotnet.db4.dev", properties["PackageProjectUrl"]);
        Assert.Equal("https://github.com/IceCoffee1024/Db4Net.git", properties["RepositoryUrl"]);
        Assert.Equal("git", properties["RepositoryType"]);
        Assert.Equal("true", properties["GenerateDocumentationFile"]);
        Assert.Equal("true", properties["PublishRepositoryUrl"]);
        Assert.Equal("true", properties["EmbedUntrackedSources"]);
        Assert.Equal("snupkg", properties["SymbolPackageFormat"]);
        Assert.Equal("true", properties["IncludeSymbols"]);
    }

    [Fact]
    public void Netstandard_target_uses_private_polysharp_build_dependency()
    {
        var project = XDocument.Load(Path.Combine(GetRepositoryRoot(), "src", "Db4Net", "Db4Net.csproj"));
        var netstandardItemGroup = project.Root!
            .Elements("ItemGroup")
            .Single(element => (string?)element.Attribute("Condition") == "'$(TargetFramework)' == 'netstandard2.0'");

        var polySharp = netstandardItemGroup
            .Elements("PackageReference")
            .Single(element => (string?)element.Attribute("Include") == "PolySharp");
        var annotations = netstandardItemGroup
            .Elements("PackageReference")
            .Single(element => (string?)element.Attribute("Include") == "System.ComponentModel.Annotations");

        Assert.Equal("5.0.0", (string?)annotations.Attribute("Version"));
        Assert.Equal("1.16.0", (string?)polySharp.Attribute("Version"));
        Assert.Equal("all", polySharp.Element("PrivateAssets")?.Value);
        Assert.Equal("runtime; build; native; contentfiles; analyzers; buildtransitive", polySharp.Element("IncludeAssets")?.Value);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Db4Net.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the Db4Net repository root.");
    }
}
