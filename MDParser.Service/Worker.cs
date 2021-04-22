using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MDParser.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private readonly string projectDirectory;
        private readonly string projectURL;
        private readonly string name;
        private readonly string email;
        private readonly string branch;
        private readonly int minuteInterval;
        private readonly CredentialsHandler credentialsProvider;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            projectDirectory = _config["ProjectDirectory"];
            credentialsProvider = (url, usernameFromUrl, types) =>
                new UsernamePasswordCredentials
                {
                    Username = _config["Credentials:PAT"],
                    Password = string.Empty
                };
            projectURL = _config["RepositoryURL"];
            name = _config["Credentials:Name"];
            email = _config["Credentials:Email"];
            branch = _config["branch"];
            minuteInterval = int.Parse(_config["pullInterval"]);
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                bool folderExists = Directory.Exists(projectDirectory);


                if (!folderExists)
                {
                    _logger.LogInformation("Repository is not here. Clonning from github");
                    var cloneOptions = new CloneOptions
                    {
                        CredentialsProvider = credentialsProvider
                    };

                    string path = Repository.Clone(projectURL, projectDirectory, cloneOptions);
                }

                try
                {
                    var repository = new Repository(projectDirectory);

                    // Pull Latest update
                    var pullOptions = new PullOptions
                    {
                        FetchOptions = new FetchOptions
                        {
                            CredentialsProvider = credentialsProvider
                        }
                    };

                    var signature = new Signature(new Identity(name, email), DateTimeOffset.Now);
                    var mergeResult = Commands.Pull(repository, signature, pullOptions);
                    
                    _logger.LogInformation($"Pulling files from repository. Current status: {mergeResult.Status.ToString()}");
                    bool isDebug = false;
//#if DEBUG
//                    isDebug = true;
//#endif
                    if (mergeResult.Status != MergeStatus.UpToDate || !folderExists|| isDebug)
                    {
                        string src = projectDirectory;
                        string dest = $"{projectDirectory}docs";
                        var pandocDictionary = Pandoc.GetDictionary();

                        _logger.LogInformation($"Converting files from {src} to {dest}");
                        // Convert Markdown
                        
                        Directory.Delete(dest,true);
                        DirectoryStructure.Copy(new DirectoryInfo(src), new DirectoryInfo(dest),true);

                        _logger.LogInformation("Directory structure copied");
                        // Process

                        // This function will create the indexes of courses
                
                        List<Metadata> coursesMedatada = new List<Metadata>();

                        DirectoryStructure.RunInEveryDirectory(t =>
                        {
                            var metadataFiles = t.GetFiles( ".courseMetadata");
                            if(metadataFiles.Length != 1)
                                return;

                            Metadata metadata = JsonSerializer.Deserialize<Metadata>(metadataFiles.First().OpenText().ReadToEnd());
                            coursesMedatada.Add(metadata);
                            var mdFiles = t.GetFiles("*.md").ToList();
                            if (mdFiles.Count != 0)
                            {
                                string indexFile = DirectoryStructure.CreateIndex(mdFiles, metadata.CourseName);
                                File.WriteAllText(t.FullName + @"/index.md",indexFile);
                            }

                        },dest);
                        
                        

                        new Pandoc(pandocDictionary,null).CreateIndex(coursesMedatada,dest);
                        
                        _logger.LogInformation("Metadata adquired and index created");

                        // This function will process all the documents to translate latex and convert links
                        await DirectoryStructure.RunInAllFiles(async t =>
                        {
                            await new Pandoc(pandocDictionary,null).ProcessDocument(t);
                        }, dest);

                        _logger.LogInformation("Files processed");

                        // Export
                        await DirectoryStructure.RunInAllFiles(async t =>
                        {
                            await new Pandoc(pandocDictionary,null).ConvertDocument(t);
                        }, dest);

                        _logger.LogInformation("Files converted to HTML");

                        // Add new Files

                        Commands.Stage(repository, "*");


                        // Commit changes

                        var commit = repository.Commit("Automatically converted Markdown files", signature, signature);

                        _logger.LogInformation("Files Commited");
                        
                        // Push changes

                        var pushOptions = new PushOptions {CredentialsProvider = credentialsProvider};
                        repository.Network.Push(repository.Branches[branch], pushOptions);
                        
                        _logger.LogInformation($"Files pushed to {branch}");

                    }

                }
                catch (Exception e)
                {
                    _logger.LogWarning(e,"No files to commit. Possible error converting files");
                }

                await Task.Delay(1000 * 60 * minuteInterval, stoppingToken);
            }
        }
    }

}
