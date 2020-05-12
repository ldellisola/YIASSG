using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;


//https://github.com/commandlineparser/commandline

namespace MDParser
{
    public class Terminal
    {
        private Process CMD;
        public Terminal()
        {
            

        }

        public string RunCommand(string command)
        {


            CMD.StandardInput.WriteLine(command + '\n');
            CMD.StandardInput.Flush();
            
            return CMD.StandardOutput.ReadToEnd();
        }
    }
}
