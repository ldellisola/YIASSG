using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YIASSG.BackgroundWorker.Git;

public class GitProcess
{
    private readonly Process process;
    protected StringBuilder errors = new();
    protected StringBuilder output = new();

    public GitProcess(string directory, params string[] args)
    {
        process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args.Aggregate("", (resutl, t) => resutl += $" {t}"),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = directory
        };

        process.OutputDataReceived += (s, e) => { output.Append(e.Data); };
        process.ErrorDataReceived += (s, e) => { errors.Append(e.Data); };
    }

    public async Task Execute(CancellationToken token = default)
    {
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(token);
        //Console.WriteLine($"Exit code: {process.ExitCode}");
    }

    public bool HasError()
    {
        return process.ExitCode != 0;
    }

    public string GetError()
    {
        return errors.ToString();
    }

    public string GetOutput()
    {
        return output.ToString();
    }

    public void Dispose()
    {
        process.Dispose();
    }
}