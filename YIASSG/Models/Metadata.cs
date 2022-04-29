using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YIASSG.Models;

public class Metadata
{
    [JsonPropertyName("CourseName")] public string Name { get; set; } = null!;

    [JsonPropertyName("CourseCode")] public string Code { get; set; } = null!;

    [JsonIgnore] public string Path { get; set; } = null!;


    public static Metadata LoadFromFile(string filename)
    {
        var file = new FileInfo(filename);
        var course = JsonSerializer.Deserialize<Metadata>(file.OpenText().ReadToEnd()) ?? throw new Exception();
        course.Path = file.DirectoryName!;
        return course;
    }
}