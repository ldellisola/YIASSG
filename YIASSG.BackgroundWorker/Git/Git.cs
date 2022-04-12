using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace YIASSG.BackgroundWorker.Git;

public class Git
{
    private string Directory;
    private string repository;
    private string Email;
    private string User;

    public Git(string _user, string _email, string _password, string _directory, string _repositoryHandle)
    {
        Directory = _directory;
        repository =
            $@"https://{_user}:{_password}@{_repositoryHandle.Replace("https://", "", StringComparison.InvariantCultureIgnoreCase)}";
        Email = _email;
        User = _user;
        if (!System.IO.Directory.Exists(_directory)) System.IO.Directory.CreateDirectory(_directory);

        if (System.IO.Directory.Exists(_directory + "/.git"))
        {
            ConfigEmail(_email).Wait();
            ConfigName(_user).Wait();
        }
    }

    private bool HasError(GitProcess p)
    {
        return p.HasError();
    }


    public async Task<bool> Clone(CancellationToken token = default)
    {
        var p = new GitProcess(Directory, "clone", $"\"{repository}\"", Directory);

        await p.Execute(token);


        return !HasError(p);
    }

    public async Task<bool> Add(CancellationToken token = default, params string[] files)
    {
        GitProcess p;

        if (files.Length == 0)
        {
            p = new GitProcess(Directory, "add", "--all");
        }
        else
        {
            var args = files.Prepend("add").ToArray();
            p = new GitProcess(Directory, args);
        }

        await p.Execute(token);


        return !HasError(p);
    }

    public async Task<bool> Commit(string message, CancellationToken token = default, params string[] files)
    {
        GitProcess p;
        await ConfigEmail(Email, token);
        await ConfigName(User, token);

        if (files.Length == 0)
        {
            p = new GitProcess(Directory, "commit", "-am", $"\"{message}\"");
        }
        else
        {
            var args = files.Prepend("commit").Append("-m").Append($"\"{message}\"").ToArray();
            p = new GitProcess(Directory, args);
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
        var p = new GitProcess(Directory, "pull");

        await p.Execute(token);


        return !HasError(p) && !p.GetOutput().Contains("Already up to date");
    }


    public async Task<bool> Push(CancellationToken token = default)
    {
        var p = new GitProcess(Directory, "push", $"\"{repository}\"");

        await p.Execute(token);
        return !HasError(p);
    }

    public async Task<bool> ConfigName(string _name, CancellationToken token = default)
    {
        var p = new GitProcess(Directory, "config", "user.name", $"\"{_name}\"");

        await p.Execute(token);
        return !HasError(p);
    }

    public async Task<bool> ConfigEmail(string _email, CancellationToken token = default)
    {
        var p = new GitProcess(Directory, "config", "user.email", $"\"{_email}\"");

        await p.Execute(token);
        return !HasError(p);
    }
}