using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YIASSG.Models
{
    public class Metadata 
    {
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        
        [JsonIgnore]
        public string Path { get; set; }


        public static Metadata LoadFromFile(string filename)
        {
            var file = new FileInfo(filename);
            var course = JsonSerializer.Deserialize<Metadata>(file.OpenText().ReadToEnd()) ?? throw new Exception();
            course.Path = file.DirectoryName;
            return course;
        }
        
    }
}