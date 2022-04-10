using System;

namespace YIASSG.Exceptions;

public class SettingsMissingException: Exception
{
    public SettingsMissingException()
        : base("Settings file missing")
    {
        
    }
}