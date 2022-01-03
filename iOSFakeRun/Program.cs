using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using System.Diagnostics;


namespace iOSFakeRun
{
    internal static class Program
    {
        private static void Main()
        {
            NativeLibraries.Load();

            var ideviceInstance = LibiMobileDevice.Instance.iDevice;
            var lockdownInstance = LibiMobileDevice.Instance.Lockdown;

            var count = 0;
            ideviceInstance.idevice_get_device_list(out var udids, ref count);
            if (udids.Count == 0)
            {
                Console.WriteLine("No device or fail to load device.");
                return;
            }

            ideviceInstance.idevice_new(out var iDevice, udids[0]).ThrowOnError();
            lockdownInstance.lockdownd_client_new_with_handshake(iDevice, out var lockdownClient, "iOSFakeRun").ThrowOnError();

            if (!Utils.GetVersion(lockdownClient, out var iosVersion))
            {
                Console.WriteLine("Fail to get iOS version.");
                return;
            }

            if (!Image.MountImage(iDevice, lockdownClient, iosVersion))
            {
                Console.WriteLine("Fail to mount image.");
                return;
            }

            if (!Location.StartService(iDevice, lockdownClient, out var locationServiceClient))
            {
                Console.WriteLine("Fail to start service.");
                return;
            }

            Debug.Assert(locationServiceClient != null, nameof(locationServiceClient) + " != null");

            var coordinate = CoordinateConvertor.Bd09ToWgs84(30.270686, 120.130714);
            if (!Location.SetLocation(locationServiceClient, coordinate[0], coordinate[1]))
            {
                Console.WriteLine("Fail to set location.");
                return;
            }

            iDevice.Dispose();
            lockdownClient.Dispose();

            Console.WriteLine("Successfully change location.");
        }
    }
}