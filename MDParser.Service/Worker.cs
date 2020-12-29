using System;
using System.IO;
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
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                bool folderExists = Directory.Exists(projectDirectory);


                if (!folderExists)
                {
                    var cloneOptions = new CloneOptions
                    {
                        CredentialsProvider = credentialsProvider
                    };

                    string path = Repository.Clone(projectURL, projectDirectory, cloneOptions);
                }

                try
                {
                    var gitRepo = new Repository(projectDirectory);

                    // Pull Latest update
                    var pullOptions = new PullOptions
                    {
                        FetchOptions = new FetchOptions
                        {
                            CredentialsProvider = credentialsProvider
                        }
                    };

                    var signature = new Signature(new Identity(name, email), DateTimeOffset.Now);
                    var mergeResult = Commands.Pull(gitRepo, signature, pullOptions);

                    if (mergeResult.Status != MergeStatus.UpToDate || !folderExists)
                    {

                        // Convert Markdown


                        // Add new Files

                        Commands.Stage(gitRepo, "*");


                        // Commit changes

                        var commit = gitRepo.Commit("Automatically converted Markdown files", signature, signature);

                        // Push changes

                        var pushOptions = new PushOptions {CredentialsProvider = credentialsProvider};
                        gitRepo.Network.Push(gitRepo.Branches["main"], pushOptions);

                    }

                }
                catch (Exception e)
                {
                    _logger.LogWarning(e,"No files to commit. Possible error converting files");
                    throw e;
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    public class Storage
    {
        public DateTimeOffset LastUpdate = DateTimeOffset.UnixEpoch;
    }
}
