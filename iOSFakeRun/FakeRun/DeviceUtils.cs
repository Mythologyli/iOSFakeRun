using iMobileDevice;
using iMobileDevice.Lockdown;

namespace iOSFakeRun.FakeRun;

internal static class DeviceUtils
{
    public static bool GetName(LockdownClientHandle? lockdownClient, out string deviceName)
    {
        deviceName = "";

        return LibiMobileDevice.Instance.Lockdown.lockdownd_get_device_name(lockdownClient, out deviceName) == LockdownError.Success;
    }

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

            var stringTempList = iosVersion.Split(".");
            iosVersion = stringTempList[0] + "." + stringTempList[1];

            return true;
        }
    }
}