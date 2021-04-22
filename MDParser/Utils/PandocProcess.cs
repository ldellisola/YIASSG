using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MDParser.Utils
{
    public class PandocProcess
    {
        protected Process process;
        protected string errors = String.Empty;
        public PandocProcess(string source, string dest, string title)
        {
            this.process =  new Process
            {
                StartInfo =
                {
                    FileName = "pandoc",
                    Arguments = $"\"{source}\" -s --mathjax --highlight-style tango --metadata pagetitle=\"{title}\" -o \"{dest}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
        }
        
        
        public async Task Execute()
        {
            process.Start();
            await process.WaitForExitAsync();
        }

        public async Task<bool> HasError()
        {
            errors = await process.StandardError.ReadToEndAsync();
            
            return errors != "";
        }

        public  async Task<string> GetError()
        {
            if (errors == String.Empty)
                errors = await process.StandardError.ReadToEndAsync();

            return errors;
        }

        public async Task<string> GetOutput()
        {
            return await process.StandardOutput.ReadToEndAsync();
        }
    }
}