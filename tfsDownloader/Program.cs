using System.IO.Compression;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

using System.Text.Json;
using Microsoft.Extensions.Configuration;

var input = File.ReadAllText("input.json");
var inputData = JsonSerializer.Deserialize<Data>(input);

if (File.Exists("input.Development.json"))
{
    var inputDevelop = File.ReadAllText("input.Development.json");
    var inputDevelopData = JsonSerializer.Deserialize<Data>(inputDevelop);
    inputData.units.AddRange(inputDevelopData.units);
}   
Console.WriteLine(inputData.units.Count);
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", true)
    .Build();

var token = config.GetSection("Root:TfsToken").Value;
var tfsUri = config.GetSection("Root:TfsUri").Value; 
var rootFolder = config.GetSection("Root:RootFolder").Value;
var projectsFilter = config.GetSection("Root:ProjectNames").GetChildren().Select(x => x.Value);
var deleteTests = bool.TryParse(config.GetSection("Root:DeleteTests").Value, out var deleteTestsVar) ? deleteTestsVar : false;
if (!Directory.Exists(rootFolder))
    Directory.CreateDirectory(rootFolder);
var workDir = Path.Combine(rootFolder, DateTime.Now.ToString("u").Replace(":", "_"));
Directory.CreateDirectory(workDir);
var zipPath = Path.Combine(workDir, "temp.zip");
var extractedPath = Path.Combine(workDir, "Extracted");
var errFile = Path.Combine(workDir, "err.log");
File.Create(errFile);
Directory.CreateDirectory(extractedPath);

foreach (var unit in inputData.units.Where(x => projectsFilter.Any(p=> p == x.project)))
{
    var orgUrl = new Uri($"{tfsUri}/{unit.collection}");
    var connection = new VssConnection(orgUrl, new VssBasicCredential(string.Empty, token));
    using (var gitClient = connection.GetClient<GitHttpClient>())
    {
        Console.WriteLine("Получение информации о проекте");
        try
        {
            var repo = await gitClient.GetRepositoryAsync(unit.project, unit.definition.name);
            var branch = await gitClient.GetBranchAsync(repo.Id, unit.branch);
            var commit = branch.Commit;

            Console.WriteLine($"Скачивание проекта {unit.definition.name}...");
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
            Console.WriteLine($"Распаковка проекта {unit.definition.name}...");
            var projectPath = Path.Combine(extractedPath, unit.definition.name);
            var branchPath = Path.Combine(projectPath, unit.branch.Replace("/", "_"));
            Directory.CreateDirectory(branchPath);
            ZipFile.ExtractToDirectory(zipPath, branchPath);
            if (deleteTests)
            {
                var testFileSolutions = Directory.GetFiles(branchPath, "*Test*.csproj", SearchOption.AllDirectories);
                testFileSolutions.AddRange(Directory.GetFiles(branchPath, "*Test*.sln", SearchOption.AllDirectories));
                foreach (var testFileSolution in testFileSolutions)
                {
                    var testDirectory = Path.GetDirectoryName(testFileSolution);
                    Directory.Delete(testDirectory, true);
                }
            }
            File.Delete(zipPath);
            Console.WriteLine($"Проект {unit.definition.name} загружен");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            File.AppendAllLines(errFile, [$"{unit.definition.name},  {unit.branch} : {ex.Message}"]);
        }
    }
}

var zipResult = Path.Combine(workDir, "zipResult.zip");
using var zipResultStream = new FileStream(zipResult, FileMode.Create);
ZipFile.CreateFromDirectory(extractedPath, zipResultStream, compressionLevel: CompressionLevel.Optimal, includeBaseDirectory : true);