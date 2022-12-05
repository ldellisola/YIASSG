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
using YIASSG.Exceptions;

namespace YIASSG.BackgroundWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string? _projectDirectory;
    private readonly int _minuteInterval;
    private readonly Git.Git _git;
    private readonly AppSettings _appSettings;

    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        var env = Environment.GetEnvironmentVariables()
                  ?? throw new Exception("Cannot access environment variables");

        _logger = logger;

        _projectDirectory = (
            env["PROJECT_DIRECTORY"]?.ToString()
            ?? config["ProjectDirectory"]
            ?? throw new ArgumentException("PROJECT_DIRECTORY")
        ).FormatAsPath();

        _minuteInterval = int.Parse(
            env["PULL_INTERVAL"]?.ToString()
            ?? config["pullInterval"]
            ?? throw new ArgumentException("PULL_INTERVAL")
        );

        _appSettings = JsonSerializer.Deserialize<AppSettings>(
                           File.OpenRead(
                               config["appSettings"]
                               ?? throw new ArgumentException("appSettings")
                           )
                       )
                       ??
                       throw new ArgumentException("Invalid appSettings")
            ;

        _git = new Git.Git(
            env["GITHUB_NAME"]?.ToString() ?? config["Github:Name"] ?? throw new ArgumentException("GITHUB_NAME"),
            env["GITHUB_EMAIL"]?.ToString() ?? throw new Exception("Invalid GITHUB_EMAIL variable"),
            env["GITHUB_PAT"]?.ToString() ?? throw new Exception("Invalid GITHUB_PAT variable"),
            _projectDirectory,
            env["REPOSITORY_URL"]?.ToString() ?? throw new Exception("Invalid REPOSITORY_URL variable")
        );
    }


    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);

            var folderExists = Directory.Exists(_projectDirectory);


            if (!folderExists || !Directory.Exists((_projectDirectory + "/.git").FormatAsPath()))
            {
                _logger.LogInformation("Cloning from github: {}", _git.Repository);
                if (await _git.Clone(token))
                    _logger.LogInformation("Cloned Successfully");
                else
                    _logger.LogError("Could not clone the repository");
            }

            try
            {
                _logger.LogInformation($"Trying to pull files from repository");

                if (await _git.Pull(token))
                {
                    _logger.LogInformation("New files found");

                    var src = _projectDirectory;
                    var dest = $@"{_projectDirectory}\docs".FormatAsPath();

                    await new YIASSG(src, dest, _appSettings).Run(token);


                    await _git.Add(token);
                    await _git.Commit("Automatically converted Markdown files", token);
                    await _git.Push(token);
                }
                else
                {
                    _logger.LogInformation("No new updates");
                }
            }
            catch (InvalidCodeSegmentException e)
            {
                _logger.LogError(e, "Invalid code segments");
            }
            catch (InvalidImageLinkException e)
            {
                _logger.LogError(e, "Invalid image link");
            }
            catch (InvalidLatexSegmentException e)
            {
                _logger.LogError(e, "Invalid latex segments");
            }
            catch (SourceNotFoundException e)
            {
                _logger.LogError(e, "Source directory does not exists");
            }

            await Task.Delay(1000 * 60 * _minuteInterval, token);
        }
    }
}