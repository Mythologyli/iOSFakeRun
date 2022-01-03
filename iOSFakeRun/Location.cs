using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using iMobileDevice.Service;
using System.Text;


namespace iOSFakeRun
{
    internal static class Location
    {
        public static bool StartService(iDeviceHandle iDevice, LockdownClientHandle lockdownClient, out ServiceClientHandle? locationServiceClient)
        {
            if (LibiMobileDevice.Instance.Lockdown.lockdownd_start_service(lockdownClient, "com.apple.dt.simulatelocation", out var lockdownServiceDescriptor) ==
                LockdownError.Success)
            {
                if (LibiMobileDevice.Instance.Service.service_client_new(iDevice, lockdownServiceDescriptor, out var client) == ServiceError.Success)
                {
                    locationServiceClient = client;
                    return true;
                }
            }

            locationServiceClient = null;
            return false;
        }

        public static bool ResetLocation(ServiceClientHandle locationServiceClient)
        {
            return SendUInt(locationServiceClient, 1u);
        }

        public static bool SetLocation(ServiceClientHandle locationServiceClient, double latitude, double longitude)
        {
            return SendUInt(locationServiceClient, 0u) != false &&
                   SendString(locationServiceClient, latitude.ToString()) != false &&
                   SendString(locationServiceClient, longitude.ToString()) != false;
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
}