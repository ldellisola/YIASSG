using System.Collections.Generic;

namespace YIASSG.Models;

public class AppSettings
{
    /// <summary>
    /// Path to the dictionary
    /// </summary>
    public Dictionary<string, string>? Dictionary { get; set; } = new();

    public string? MetadataFileName { get; set; }
    public string Template { get; set; } = "HTMLResources/template.html";

    public string[] JsAssets { get; set; } = {"HTMLResources/prism.js"};
    public string[] CssAssets { get; set; } = {"HTMLResources/prism.css", "HTMLResources/markdownStyle.css"};
}