using System.Runtime.InteropServices;

namespace NativeTloLibrary;

public class UberUpdate
{
    [UnmanagedCallersOnly(EntryPoint = "uber_update_topics")]
    public static void Topics()
    {

    }

}
