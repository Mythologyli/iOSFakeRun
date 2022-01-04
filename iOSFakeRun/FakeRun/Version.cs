using iMobileDevice;
using iMobileDevice.Lockdown;

namespace iOSFakeRun.FakeRun;

internal static class Utils
{
    public static bool GetVersion(LockdownClientHandle? lockdownClient, out string iosVersion)
    {
        iosVersion = "";

        if (LibiMobileDevice.Instance.Lockdown.lockdownd_get_value(lockdownClient, null, "ProductVersion", out var plistHandle) != LockdownError.Success)
        {
            return false;
        }

        using (plistHandle)
        {
            LibiMobileDevice.Instance.Plist.plist_get_string_val(plistHandle, out iosVersion);

            return true;
        }
    }
}