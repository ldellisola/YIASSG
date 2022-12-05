using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommandLine;
using YIASSG.Models;
using YIASSG.Utils;
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace YIASSG.Console;

public static class Terminal
{
    public static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<Options>(args)
            .WithNotParsed(ParseError)
            .WithParsedAsync(RunOptionsAsync);
    }
    private sealed record Options
    {
        [property:Option('s', "source", Required = true, HelpText = "source of the markdown documents")]
        public required string Source { get; init; }
    
        [Option('d', "destination", Required = true, HelpText = "Folder to publish the converted documents")]
        public required string Destination { get; init; }
    
        [Option('c', "config", Required = true, HelpText = "Path to the config file")]
        public required string Settings { get; init; }
    }

    private static async Task RunOptionsAsync(Options opts)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(await File.OpenText(opts.Settings.FormatAsPath()).ReadToEndAsync());
        ArgumentNullException.ThrowIfNull(settings);

        await new YIASSG(
            opts.Source.FormatAsPath(),
            opts.Destination.FormatAsPath(),
            settings
        ).Run();
    }

    private static void ParseError(IEnumerable<Error> errors)
    {
        System.Console.WriteLine("Error processing inputs!");
        System.Console.WriteLine("Shutting down.");
    }
}