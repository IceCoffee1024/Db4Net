using System.Xml.Linq;

namespace Db4Net.Tests;

public sealed class PackageMetadataTests
{
    [Fact]
    public void Package_metadata_contains_required_alpha_fields()
    {
        var project = XDocument.Load(Path.Combine(GetRepositoryRoot(), "src", "Db4Net", "Db4Net.csproj"));
        var properties = project.Root!.Elements("PropertyGroup").Elements().ToDictionary(element => element.Name.LocalName, element => element.Value);

        Assert.Equal("Db4Net", properties["PackageId"]);
        Assert.Equal("0.1.0-alpha.1", properties["Version"]);
        Assert.Equal("IceCoffee", properties["Authors"]);
        Assert.Equal("Lightweight fluent SQL builder and Dapper execution helpers.", properties["Description"]);
        Assert.Equal("dapper;sql;fluent;query-builder", properties["PackageTags"]);
        Assert.Equal("README.md", properties["PackageReadmeFile"]);
        Assert.Equal("MIT", properties["PackageLicenseExpression"]);
        Assert.Equal("https://github.com/IceCoffee1024/Db4Net", properties["PackageProjectUrl"]);
        Assert.Equal("https://github.com/IceCoffee1024/Db4Net.git", properties["RepositoryUrl"]);
        Assert.Equal("git", properties["RepositoryType"]);
        Assert.Equal("true", properties["GenerateDocumentationFile"]);
        Assert.Equal("true", properties["PublishRepositoryUrl"]);
        Assert.Equal("true", properties["EmbedUntrackedSources"]);
        Assert.Equal("snupkg", properties["SymbolPackageFormat"]);
        Assert.Equal("true", properties["IncludeSymbols"]);
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
