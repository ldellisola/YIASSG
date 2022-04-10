using System;

namespace YIASSG.Exceptions
{
    // TODO: Add line where this happens
    public class InvalidLatexSegmentException: Exception
    {
        public InvalidLatexSegmentException(string fileName)
                :base($"There are broken latex segments in {fileName}")
            { }
        
    }
}