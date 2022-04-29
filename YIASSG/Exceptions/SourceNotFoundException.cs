using System.IO;

namespace YIASSG.Exceptions;

public class SourceNotFoundException : DirectoryNotFoundException
{
    public SourceNotFoundException(string? directory)
        : base($"The source directory: {directory} does not exists")
    {
    }
}