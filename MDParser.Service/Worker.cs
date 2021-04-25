using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using MDParser.Utils;
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
        private readonly Git git;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            projectDirectory = _config["ProjectDirectory"].FormatAsPath();
            
            projectURL = _config["RepositoryURL"];
            name = _config["Credentials:Name"];
            email = _config["Credentials:Email"];
            branch = _config["branch"];
            minuteInterval = int.Parse(_config["pullInterval"]);

            git = new Git(name, email, _config["Credentials:PAT"], projectDirectory, projectURL);
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                bool folderExists = Directory.Exists(projectDirectory);


                if (!folderExists || !Directory.Exists((projectDirectory + "/.git" ).FormatAsPath()))
                {
                    _logger.LogInformation("Repository is not here. Clonning from github");
                    await git.Clone(stoppingToken);
                    
                }

                try
                {
                    _logger.LogInformation($"Trying to pull files from repository");

                    if (await git.Pull(stoppingToken)) { 
                    
                        _logger.LogInformation("New files to pull found");


                        string src = projectDirectory;
                        string dest = $@"{projectDirectory}\docs".FormatAsPath();
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
                            var metadataFiles = t.GetFiles( "courseMetadata.stp");
                            if(metadataFiles.Length != 1)
                                return;

                            Metadata metadata = JsonSerializer.Deserialize<Metadata>(metadataFiles.First().OpenText().ReadToEnd());
                            coursesMedatada.Add(metadata);
                            var mdFiles = t.GetFiles("*.md").ToList();
                            if (mdFiles.Count != 0)
                            {
                                string indexFile = DirectoryStructure.CreateIndex(mdFiles, metadata.CourseName);
                                File.WriteAllText((t.FullName + @"/index.md").FormatAsPath(),indexFile);
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

                        await git.Add(stoppingToken);


                        // Commit changes

                        await git.Commit("Automatically converted Markdown files", stoppingToken);

                        _logger.LogInformation("Files Commited");
                        
                        // Push changes

                        await git.Push(stoppingToken);
                        
                        _logger.LogInformation($"Files pushed to {branch}");

                    }
                    else
                    {
                        _logger.LogInformation("No new updates");
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
