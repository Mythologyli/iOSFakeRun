using System;
using System.Globalization;
using System.Text;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using iMobileDevice.Service;

namespace iOSFakeRun.FakeRun;

internal static class Location
{
    public static bool ResetLocation(iDeviceHandle? idevice, LockdownClientHandle? lockdownClient)
    {
        return idevice != null &&
               lockdownClient != null &&
               LibiMobileDevice.Instance.Lockdown.lockdownd_start_service(lockdownClient, "com.apple.dt.simulatelocation", out var lockdownServiceDescriptor) ==
               LockdownError.Success &&
               LibiMobileDevice.Instance.Service.service_client_new(idevice, lockdownServiceDescriptor, out var locationServiceClient) == ServiceError.Success &&
               SendUInt(locationServiceClient, 1u);
    }

    public static bool SetLocation(iDeviceHandle? idevice, LockdownClientHandle? lockdownClient, double latitude, double longitude)
    {
        return idevice != null &&
               lockdownClient != null &&
               LibiMobileDevice.Instance.Lockdown.lockdownd_start_service(lockdownClient, "com.apple.dt.simulatelocation", out var lockdownServiceDescriptor) ==
               LockdownError.Success &&
               LibiMobileDevice.Instance.Service.service_client_new(idevice, lockdownServiceDescriptor, out var locationServiceClient) == ServiceError.Success &&
               SendUInt(locationServiceClient, 0u) &&
               SendString(locationServiceClient, latitude.ToString(CultureInfo.InvariantCulture)) &&
               SendString(locationServiceClient, longitude.ToString(CultureInfo.InvariantCulture));
    }

    private static bool SendUInt(ServiceClientHandle locationServiceClient, uint value)
    {
        var sent = 0u;
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return LibiMobileDevice.Instance.Service.service_send(locationServiceClient, bytes, (uint) bytes.Length, ref sent) == ServiceError.Success;
    }

    private static bool SendString(ServiceClientHandle locationServiceClient, string value)
    {
        var sent = 0u;
        var bytes = Encoding.UTF8.GetBytes(value);
        var bytesLengthBytes = BitConverter.GetBytes((uint) value.Length);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytesLengthBytes);
        }

        return LibiMobileDevice.Instance.Service.service_send(locationServiceClient, bytesLengthBytes, (uint) bytesLengthBytes.Length, ref sent) == ServiceError.Success &&
               LibiMobileDevice.Instance.Service.service_send(locationServiceClient, bytes, (uint) bytes.Length, ref sent) == ServiceError.Success;
    }
}