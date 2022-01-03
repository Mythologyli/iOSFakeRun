using System.Diagnostics;
using System.Windows;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using iMobileDevice.Service;


namespace iOSFakeRun;

public partial class MainWindow : Window
{
    private readonly IiDeviceApi _ideviceInstance = LibiMobileDevice.Instance.iDevice;
    private readonly ILockdownApi _lockdownInstance = LibiMobileDevice.Instance.Lockdown;
    private iDeviceHandle? _idevice;
    private LockdownClientHandle? _lockdownClient;

    public MainWindow()
    {
        InitializeComponent();

        NativeLibraries.Load();
    }

    private void Link(object sender, RoutedEventArgs e)
    {
        var count = 0;
        _ideviceInstance.idevice_get_device_list(out var udids, ref count);
        if (udids.Count == 0)
        {
            MessageBox.Show("No device or fail to load device.");
            return;
        }

        _ideviceInstance.idevice_new(out _idevice, udids[0]).ThrowOnError();
        _lockdownInstance.lockdownd_client_new_with_handshake(_idevice, out _lockdownClient, "iOSFakeRun").ThrowOnError();

        if (!Utils.GetVersion(_lockdownClient, out var iosVersion))
        {
            MessageBox.Show("Fail to get iOS version.");
            return;
        }

        if (!Image.MountImage(_idevice, _lockdownClient, iosVersion))
        {
            MessageBox.Show("Fail to mount image.");
            return;
        }

        MessageBox.Show("Successfully link.");
    }

    private void ChangeLocation(object sender, RoutedEventArgs e)
    {
        if (_idevice == null)
        {
            MessageBox.Show("Link first!");
            return;
        }

        var coordinate = CoordinateConvertor.Bd09ToWgs84(30.270686, 120.130714);
        if (!Location.SetLocation(_idevice, _lockdownClient, coordinate[0], coordinate[1]))
        {
            MessageBox.Show("Fail to set location.");
            return;
        }

        MessageBox.Show("Successfully change location.");
    }

    private void ResetLocation(object sender, RoutedEventArgs e)
    {
        if (_idevice == null)
        {
            MessageBox.Show("Link first!");
            return;
        }

        if (!Location.ResetLocation(_idevice, _lockdownClient))
        {
            MessageBox.Show("Fail to reset location.");
            return;
        }

        MessageBox.Show("Successfully reset location.");
    }

    private void UnLink(object sender, RoutedEventArgs e)
    {
        _idevice?.Dispose();
        _lockdownClient?.Dispose();

        _idevice = null;
        _lockdownClient = null;

        MessageBox.Show("Successfully unlink.");
    }
}