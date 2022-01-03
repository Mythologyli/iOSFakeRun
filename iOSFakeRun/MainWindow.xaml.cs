using System.Diagnostics;
using System.Windows;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;


namespace iOSFakeRun;

public partial class MainWindow : Window
{
    private readonly IiDeviceApi _ideviceInstance = LibiMobileDevice.Instance.iDevice;
    private readonly ILockdownApi _lockdownInstance = LibiMobileDevice.Instance.Lockdown;

    public MainWindow()
    {
        InitializeComponent();

        NativeLibraries.Load();
    }

    private void LinkAndChangeLocation(object sender, RoutedEventArgs e)
    {
        var count = 0;
        _ideviceInstance.idevice_get_device_list(out var udids, ref count);
        if (udids.Count == 0)
        {
            MessageBox.Show("No device or fail to load device.");
            return;
        }

        _ideviceInstance.idevice_new(out var iDevice, udids[0]).ThrowOnError();
        _lockdownInstance.lockdownd_client_new_with_handshake(iDevice, out var lockdownClient, "iOSFakeRun").ThrowOnError();

        if (!Utils.GetVersion(lockdownClient, out var iosVersion))
        {
            MessageBox.Show("Fail to get iOS version.");
            return;
        }

        if (!Image.MountImage(iDevice, lockdownClient, iosVersion))
        {
            MessageBox.Show("Fail to mount image.");
            return;
        }

        if (!Location.StartService(iDevice, lockdownClient, out var locationServiceClient))
        {
            MessageBox.Show("Fail to start service.");
            return;
        }

        Debug.Assert(locationServiceClient != null, nameof(locationServiceClient) + " != null");

        var coordinate = CoordinateConvertor.Bd09ToWgs84(30.270686, 120.130714);
        if (!Location.SetLocation(locationServiceClient, coordinate[0], coordinate[1]))
        {
            MessageBox.Show("Fail to set location.");
            return;
        }

        iDevice.Dispose();
        lockdownClient.Dispose();

        MessageBox.Show("Successfully change location.");
    }
}