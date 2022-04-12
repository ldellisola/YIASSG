using System.IO;
using System.Linq;

namespace YIASSG.Exceptions;

internal class InvalidImageLinkException : FileNotFoundException
{
    public InvalidImageLinkException(string fileName, string imageMissing, string document, int start, int end)
        : base(@$"
There are some missing images on this file: {fileName}.
File missing: {imageMissing}
At line: {GetStartingLine(document, start)}
Context: 
{GetContext(document, start, end)}
This may happen if you are linking to the same image file twice or more. The recommended way is to have duplicated the images on your storage
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