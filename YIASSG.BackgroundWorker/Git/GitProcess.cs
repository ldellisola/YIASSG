using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YIASSG.BackgroundWorker.Git;

public class GitProcess
{
    private readonly Process _process;
    private readonly StringBuilder _errors = new();
    private readonly StringBuilder _output = new();

    public GitProcess(string? directory, params string?[] args)
    {
        _process = new Process();
        _process.StartInfo = new()
        {
            FileName = "git",
            Arguments = string.Join(' ',args),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = directory
        };

        _process.OutputDataReceived += (_, e) => { _output.Append(e.Data); };
        _process.ErrorDataReceived += (_, e) => { _errors.Append(e.Data); };
    }

    public async Task Execute(CancellationToken token = default)
    {
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        await _process.WaitForExitAsync(token);
        //Console.WriteLine($"Exit code: {process.ExitCode}");
    }

    public bool HasError()
    {
        return _process.ExitCode != 0;
    }

    public string GetError()
    {
        return _errors.ToString();
    }

    public string GetOutput()
    {
        return _output.ToString();
    }

    public void Dispose()
    {
        _process.Dispose();
    }
}