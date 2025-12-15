using System.IO.Compression;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

var input = File.Exists("input.Development.json") ? File.ReadAllText("input.Development.json") : File.ReadAllText("input.json");
var options = new JsonSerializerOptions()
{
    PropertyNameCaseInsensitive = true
};
var inputData = JsonSerializer.Deserialize<Data>(input, options);

Console.WriteLine(inputData.Units.Count);
var configFile = File.Exists("appsettings.Development.json") ? File.ReadAllText("appsettings.Development.json") : File.ReadAllText("appsettings.json");
var config = JsonSerializer.Deserialize<Configuration>(configFile);

if (!Directory.Exists(config.RootFolder))
    Directory.CreateDirectory(config.RootFolder);

var workDir = Path.Combine(config.RootFolder, DateTime.Now.ToString("u").Replace(":", "_"));
Directory.CreateDirectory(workDir);
var zipPath = Path.Combine(workDir, "temp.zip");
var extractedPath = Path.Combine(workDir, "Extracted");
var errFile = Path.Combine(workDir, "err.log");
using var errFileStream = File.Create(errFile);
using var errWriter = new StreamWriter(errFileStream);
Directory.CreateDirectory(extractedPath);

foreach (var unit in inputData.Units.Where(x => config.ProjectNames.Any(p=> p == x.Project)))
{
    var orgUrl = new Uri($"{config.TfsUri}/{unit.Collection}");
    var connection = new VssConnection(orgUrl, new VssBasicCredential(string.Empty, config.TfsToken));
    using (var gitClient = connection.GetClient<GitHttpClient>())
    {
        Console.WriteLine("Получение информации о проекте");
        var repoName = unit.RepositoryName ?? unit.Definition.Name;
        try
        {
            var repo = await gitClient.GetRepositoryAsync(unit.Project, repoName);
            var branch = await gitClient.GetBranchAsync(repo.Id, unit.Branch);
            var commit = branch.Commit;

            Console.WriteLine($"Скачивание проекта {repoName}...");
            // Download zip and extract to targetPath
            using var zipStream = await gitClient.GetItemZipAsync(repo.Id, "/", versionDescriptor: new GitVersionDescriptor()
            {
                Version = commit.CommitId,
                VersionType = GitVersionType.Commit
            });
            using (var fileStream = File.Create(zipPath))
            {
                zipStream.CopyTo(fileStream);
            }
            Console.WriteLine($"Распаковка проекта {repoName}...");
            var projectPath = Path.Combine(extractedPath, repoName);
            var branchPath = Path.Combine(projectPath, unit.Branch.Replace("/", "_"));
            Directory.CreateDirectory(branchPath);
            ZipFile.ExtractToDirectory(zipPath, branchPath);
            if (config.DeleteTests)
            {
                var testFileSolutions = Directory.GetFiles(branchPath, "*Test*.csproj", SearchOption.AllDirectories);
                testFileSolutions.AddRange(Directory.GetFiles(branchPath, "*Test*.sln", SearchOption.AllDirectories));
                foreach (var testFileSolution in testFileSolutions)
                {
                    var testDirectory = Path.GetDirectoryName(testFileSolution);
                    Directory.Delete(testDirectory, true);
                }
            }
            if (config.OnlyCs)
            {
                var nonCsFiles = Directory.GetFiles(branchPath, "*.*", SearchOption.AllDirectories).Except(Directory.GetFiles(branchPath, "*.cs", SearchOption.AllDirectories));
                foreach (var nonCsFile in nonCsFiles)
                {
                    File.Delete(nonCsFile);
                }
            }
            File.Delete(zipPath);
            Console.WriteLine($"Проект {repoName} загружен");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            errWriter.WriteLine($"{repoName},  {unit.Branch} : {ex.Message}");
        }
    }
}

var zipResult = Path.Combine(workDir, "zipResult.zip");
using var zipResultStream = new FileStream(zipResult, FileMode.Create);
ZipFile.CreateFromDirectory(extractedPath, zipResultStream, compressionLevel: CompressionLevel.Optimal, includeBaseDirectory : true);