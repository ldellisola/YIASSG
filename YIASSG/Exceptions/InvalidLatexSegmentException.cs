using System;
using System.Linq;

namespace YIASSG.Exceptions;

public class InvalidLatexSegmentException : Exception
{
    public InvalidLatexSegmentException(string fileName, string document, int start, int end)
        : base(@$"
There are broken latex segments in {fileName}.
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