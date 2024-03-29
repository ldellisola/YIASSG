using System;
using System.Linq;

namespace YIASSG.Exceptions;

public class InvalidCodeSegmentException : Exception
{
    public InvalidCodeSegmentException(string fileName, string document, int start, int end)
        : base(@$"
There are broken code segments in {fileName}.
At line: {GetStartingLine(document, start)}
Context: 
{GetContext(document, start, end)}
")
    {
    }

    private static int GetStartingLine(string document, int start)
    {
        return document.Take(start).Count(t => t == '\n') + 1;
    }

    private static string GetContext(string document, int start, int end)
    {
        return document[start..end];
    }
}