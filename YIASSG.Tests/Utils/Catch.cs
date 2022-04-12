using System;

namespace YIASSG.Tests.Utils;

public static class Catch
{
    public static Exception? Exception(Action a)
    {
        try
        {
            a();
            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }
}