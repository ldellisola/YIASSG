using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace YIASSG.BackgroundWorker.Git;

public class Git
{
    private readonly string _directory;
    public readonly string Repository;
    private readonly string _email;
    private readonly string _user;

    public Git(string user, string email, string password, string? directory, string repositoryHandle)
    {
        ArgumentNullException.ThrowIfNull(directory);
        
        _directory = directory;
        Repository =
            $@"https://{user}:{password}@{repositoryHandle.Replace("https://", "", StringComparison.InvariantCultureIgnoreCase)}";
        _email = email;
        _user = user;
        if (!System.IO.Directory.Exists(directory)) System.IO.Directory.CreateDirectory(directory);

        if (System.IO.Directory.Exists(directory + "/.git"))
        {
            ConfigEmail(email).Wait();
            ConfigName(user).Wait();
        }
    }

    private bool HasError(GitProcess p)
    {
        return p.HasError();
    }


    public async Task<bool> Clone(CancellationToken token = default)
    {
        var p = new GitProcess(_directory, "clone", $"\"{Repository}\"", _directory);

        await p.Execute(token);


        return !HasError(p);
    }

    public async Task<bool> Add(CancellationToken token = default, params string[] files)
    {
        GitProcess p;

        if (files.Length == 0)
        {
            p = new GitProcess(_directory, "add", "--all");
        }
        else
        {
            var args = files.Prepend("add").ToArray();
            p = new GitProcess(_directory, args);
        }

        await p.Execute(token);


        return !HasError(p);
    }

    public async Task<bool> Commit(string message, CancellationToken token = default, params string[] files)
    {
        GitProcess p;
        await ConfigEmail(_email, token);
        await ConfigName(_user, token);

        if (files.Length == 0)
        {
            p = new GitProcess(_directory, "commit", "-am", $"\"{message}\"");
        }
        else
        {
            var args = files.Prepend("commit").Append("-m").Append($"\"{message}\"").ToArray();
            p = new GitProcess(_directory, args);
        }

        await p.Execute(token);
        return !HasError(p);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>True if there were not errors and something was pulled</returns>
    public async Task<bool> Pull(CancellationToken token = default)
    {
        var p = new GitProcess(_directory, "pull");

        await p.Execute(token);


        return !HasError(p) && !p.GetOutput().Contains("Already up to date");
    }


    public async Task<bool> Push(CancellationToken token = default)
    {
        var p = new GitProcess(_directory, "push", $"\"{Repository}\"");

        await p.Execute(token);
        return !HasError(p);
    }

    public async Task<bool> ConfigName(string name, CancellationToken token = default)
    {
        var p = new GitProcess(_directory, "config", "user.name", $"\"{name}\"");

        await p.Execute(token);
        return !HasError(p);
    }

    public async Task<bool> ConfigEmail(string email, CancellationToken token = default)
    {
        var p = new GitProcess(_directory, "config", "user.email", $"\"{email}\"");

        await p.Execute(token);
        return !HasError(p);
    }
}