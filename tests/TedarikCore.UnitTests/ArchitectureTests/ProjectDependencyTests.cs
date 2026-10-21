using System.Xml.Linq;

namespace TedarikCore.UnitTests.ArchitectureTests;

// Bağımlılık kuralı .csproj içindeki <ProjectReference> satırlarında yaşar.
// (Derleyici kullanılmayan referansları assembly metadata'sına yazmadığı için
// reflection ile kontrol etmek eksik sonuç verirdi.)
public class ProjectDependencyTests
{
    [Theory]
    [InlineData("TedarikCore.Domain", new string[] { })]
    [InlineData("TedarikCore.Application", new[] { "TedarikCore.Domain" })]
    [InlineData("TedarikCore.Infrastructure", new[] { "TedarikCore.Application", "TedarikCore.Domain" })]
    [InlineData("TedarikCore.Api", new[] { "TedarikCore.Application", "TedarikCore.Infrastructure" })]
    public void Project_References_Only_Allowed_Projects(string project, string[] allowed)
    {
        var references = GetProjectReferences(project);

        var forbidden = references.Except(allowed).ToList();

        Assert.True(
            forbidden.Count == 0,
            $"{project} şu projelere bağımlı olmamalı: {string.Join(", ", forbidden)}");
    }

    [Theory]
    [InlineData("TedarikCore.Application", "TedarikCore.Domain")]
    [InlineData("TedarikCore.Infrastructure", "TedarikCore.Application")]
    [InlineData("TedarikCore.Api", "TedarikCore.Application")]
    [InlineData("TedarikCore.Api", "TedarikCore.Infrastructure")]
    public void Project_References_Required_Project(string project, string required)
    {
        Assert.Contains(required, GetProjectReferences(project));
    }

    private static List<string> GetProjectReferences(string project)
    {
        var csproj = Path.Combine(FindRepositoryRoot(), "src", project, $"{project}.csproj");
        Assert.True(File.Exists(csproj), $"Proje dosyası bulunamadı: {csproj}");

        return XDocument.Load(csproj)
            .Descendants("ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension(
                ((string)e.Attribute("Include")!).Replace('\\', '/')))
            .ToList();
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TedarikCore.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("TedarikCore.slnx bulunamadı.");
    }
}
