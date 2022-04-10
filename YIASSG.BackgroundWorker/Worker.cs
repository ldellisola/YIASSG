using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YIASSG.Models;
using YIASSG.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace YIASSG.BackgroundWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string _projectDirectory;
    private readonly int _minuteInterval;
    private readonly Git.Git _git;
    private readonly AppSettings _appSettings;

    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        _projectDirectory = config["ProjectDirectory"].FormatAsPath();
        _minuteInterval = int.Parse(config["pullInterval"]);
        _appSettings = JsonSerializer.Deserialize<AppSettings>(config["appSettings"])
                       ??
                       throw new ArgumentException("Invalid appSettings")
            ;

        _git = new Git.Git(
            config["Credentials:Name"],
            config["Credentials:Email"],
            config["Credentials:PAT"],
            _projectDirectory,
            config["RepositoryURL"]);
    }


    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);

            var folderExists = Directory.Exists(_projectDirectory);


            if (!folderExists || !Directory.Exists((_projectDirectory + "/.git").FormatAsPath()))
            {
                _logger.LogInformation("Repository is not here. Clonning from github");
                await _git.Clone(token);
            }

            try
            {
                _logger.LogInformation($"Trying to pull files from repository");

                if (await _git.Pull(token))
                {
                    _logger.LogInformation("New files to pull found");

                    var src = _projectDirectory;
                    var dest = $@"{_projectDirectory}\docs".FormatAsPath();

                    await new MDParser(src, dest, _appSettings).Run(token);


                    await _git.Add(token);
                    await _git.Commit("Automatically converted Markdown files", token);
                    await _git.Push(token);
                }
                else
                {
                    _logger.LogInformation("No new updates");
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "No files to commit. Possible error converting files");
            }

            await Task.Delay(1000 * 60 * _minuteInterval, token);
        }
    }
}