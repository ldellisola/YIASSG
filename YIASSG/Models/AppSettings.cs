using System.Collections.Generic;

namespace YIASSG.Models;

public class AppSettings
{
    /// <summary>
    /// Path to the dictionary
    /// </summary>
    public Dictionary<string, string> Dictionary { get; set; }

    public string MetadataFileName { get; set; }

    public string Template { get; set; } = "template.html";
}