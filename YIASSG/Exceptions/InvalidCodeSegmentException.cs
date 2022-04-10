using System;

namespace YIASSG.Exceptions
{
    // TODO: Add line where this happens
    public class InvalidCodeSegmentException: Exception
    {
        public InvalidCodeSegmentException(string fileName)
            :base($"There are broken code segments in {fileName}")
        { }
        
    }
}