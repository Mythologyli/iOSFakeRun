using System;
using System.IO;
using System.Runtime.InteropServices;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using iMobileDevice.MobileImageMounter;

namespace iOSFakeRun.FakeRun;

internal static class Image
{
    public static bool MountImage(iDeviceHandle idevice, LockdownClientHandle? lockdownClient, string iosVersion)
    {
        var mounterInstance = LibiMobileDevice.Instance.MobileImageMounter;
        var lockdownInstance = LibiMobileDevice.Instance.Lockdown;
        var plistInstance = LibiMobileDevice.Instance.Plist;

        if (lockdownInstance.lockdownd_start_service(lockdownClient, "com.apple.mobile.mobile_image_mounter", out var lockdownServiceDescriptor) !=
            LockdownError.Success)
        {
            return false;
        }

        using (lockdownServiceDescriptor)
        {
            if (mounterInstance.mobile_image_mounter_new(idevice, lockdownServiceDescriptor, out var mounterClient) != MobileImageMounterError.Success)
            {
                return false;
            }

            using (mounterClient)
            {
                try
                {
                    if (mounterInstance.mobile_image_mounter_lookup_image(mounterClient, "Developer", out var plist) != MobileImageMounterError.Success)
                    {
                        return false;
                    }

                    using (plist)
                    {
                        var array = plistInstance.plist_dict_get_item(plist, "ImageSignature");
                        using (array)
                        {
                            var size = plistInstance.plist_array_get_size(array);
                            if (size > 0)
                            {
                                return true;
                            }
                        }
                    }

                    var image = File.ReadAllBytes($"DeveloperDiskImage/{iosVersion}/DeveloperDiskImage.dmg");
                    var imageSign = File.ReadAllBytes($"DeveloperDiskImage/{iosVersion}/DeveloperDiskImage.dmg.signature");
                    var i = 0;
                    if (mounterInstance.mobile_image_mounter_upload_image(mounterClient, "Developer", (uint) image.Length, imageSign, (ushort) imageSign.Length,
                            (buffer, length, _) =>
                            {
                                Marshal.Copy(image, i, buffer, (int) length);
                                i += (int) length;

                                return (int) length;
                            }, new IntPtr(0))
                        != MobileImageMounterError.Success)
                    {
                        return false;
                    }

                    if (mounterInstance.mobile_image_mounter_mount_image(mounterClient, "/private/var/mobile/Media/PublicStaging/staging.dimage", imageSign,
                            (ushort) imageSign.Length, "Developer", out plist) != MobileImageMounterError.Success)
                    {
                        return false;
                    }

                    using (plist)
                    {
                        uint length = 0;
                        plistInstance.plist_to_xml(plist, out var plistXml, ref length);
                        if (!plistXml.Contains("Complete") || plistXml.Contains("ImageMountFailed"))
                        {
                            return false;
                        }
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    if (mounterClient != null && !mounterClient.IsClosed)
                    {
                        mounterInstance.mobile_image_mounter_hangup(mounterClient);
                    }
                }
            }
        }

        return true;
    }
}