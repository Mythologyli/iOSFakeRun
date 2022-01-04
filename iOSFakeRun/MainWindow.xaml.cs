using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Windows;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using iOSFakeRun.FakeRun;
using Newtonsoft.Json.Linq;

namespace iOSFakeRun;

public partial class MainWindow : Window
{
    private readonly IiDeviceApi _ideviceInstance = LibiMobileDevice.Instance.iDevice;
    private readonly ILockdownApi _lockdownInstance = LibiMobileDevice.Instance.Lockdown;
    private iDeviceHandle? _idevice;
    private bool _isRunning;
    private LockdownClientHandle? _lockdownClient;

    public MainWindow()
    {
        InitializeComponent();

        NativeLibraries.Load();

        if (!File.Exists("./route.save"))
        {
            return;
        }

        var reader = new StreamReader("./route.save");
        TextBoxRoute.Text = reader.ReadLine();
        reader.Close();
    }

    private void Link(object sender, RoutedEventArgs e)
    {
        var count = 0;
        _ideviceInstance.idevice_get_device_list(out var udids, ref count);
        if (udids.Count == 0)
        {
            MessageBox.Show("无设备连接");
            return;
        }

        _ideviceInstance.idevice_new(out _idevice, udids[0]).ThrowOnError();
        _lockdownInstance.lockdownd_client_new_with_handshake(_idevice, out _lockdownClient, "iOSFakeRun").ThrowOnError();

        if (!Utils.GetVersion(_lockdownClient, out var iosVersion))
        {
            _idevice?.Dispose();
            _lockdownClient?.Dispose();

            _idevice = null;
            _lockdownClient = null;

            MessageBox.Show("获取 iOS 版本失败");
            return;
        }

        if (!Image.MountImage(_idevice, _lockdownClient, iosVersion))
        {
            _idevice?.Dispose();
            _lockdownClient?.Dispose();

            _idevice = null;
            _lockdownClient = null;

            MessageBox.Show($"挂载开发者镜像失败\n请检查 DeveloperDiskImage 文件夹下是否有 {iosVersion} 版本的开发者镜像\n请确保设备未处于锁屏状态");
            return;
        }

        MessageBox.Show("成功连接");
    }

    private void ResetLocation(object sender, RoutedEventArgs e)
    {
        if (_idevice == null)
        {
            MessageBox.Show("请先连接设备");
            return;
        }

        if (!Location.ResetLocation(_idevice, _lockdownClient))
        {
            MessageBox.Show("重置定位失败\n请检查是否连接并尝试再次点击连接按钮");
            return;
        }

        MessageBox.Show("成功重置定位");
    }

    private void UnLink(object sender, RoutedEventArgs e)
    {
        _idevice?.Dispose();
        _lockdownClient?.Dispose();

        _idevice = null;
        _lockdownClient = null;

        MessageBox.Show("成功断开连接");
    }

    private void Quit(object sender, RoutedEventArgs e)
    {
        _idevice?.Dispose();
        _lockdownClient?.Dispose();

        _idevice = null;
        _lockdownClient = null;

        Close();
    }

    private void StartRun(object sender, RoutedEventArgs e)
    {
        var routeText = TextBoxRoute.Text;
        var routeList = new ArrayList();

        var writer = new StreamWriter("./route.save", false);
        writer.WriteLine(routeText);
        writer.Close();

        try
        {
            if (!routeText.Substring(0, 1).Equals("["))
            {
                routeText = "[" + routeText + "]";
            }

            var array = JArray.Parse(routeText);
            foreach (var position in array)
            {
                var latitudeToken = position.SelectToken("lat");
                var longitudeToken = position.SelectToken("lng");

                if (latitudeToken == null || longitudeToken == null)
                {
                    MessageBox.Show("解析路径数据失败\n请确保路径格式合法");
                    return;
                }

                var latitude = double.Parse(latitudeToken.ToString());
                var longitude = double.Parse(longitudeToken.ToString());

                double[] route = {latitude, longitude};
                routeList.Add(route);
            }
        }
        catch (Exception)
        {
            MessageBox.Show("解析路径数据失败\n请确保路径格式合法");
            return;
        }

        var routeFixedList = new ArrayList();
        foreach (double[] route in routeList)
        {
            routeFixedList.Add(CoordinateConvertor.Bd09ToWgs84(route[0], route[1]));
        }

        var runThread = new Thread(() =>
        {
            LabelRun.Dispatcher.BeginInvoke((ThreadStart) delegate
            {
                ButtonRun.Visibility = Visibility.Hidden;
                ButtonStop.Visibility = Visibility.Visible;
                LabelRun.Content = "正在跑步中...";
                ProgressBarRun.Value = 0.0;
            });
            var routeNumber = routeFixedList.Count;
            var i = 0;

            foreach (double[] route in routeFixedList)
            {
                if (!_isRunning || !Location.SetLocation(_idevice, _lockdownClient, route[0], route[1]))
                {
                    if (_isRunning)
                    {
                        _isRunning = false;
                        MessageBox.Show("修改定位失败\n请检查是否连接并尝试再次点击连接按钮");
                    }

                    LabelRun.Dispatcher.BeginInvoke((ThreadStart) delegate
                    {
                        LabelRun.Content = "未在跑步状态";
                        ButtonRun.Visibility = Visibility.Visible;
                        ButtonStop.Visibility = Visibility.Hidden;
                        ProgressBarRun.Value = 0.0;
                    });

                    return;
                }

                Thread.Sleep(2000);
                i++;
                var iOut = i;
                LabelRun.Dispatcher.BeginInvoke((ThreadStart) delegate { ProgressBarRun.Value = (double) iOut / routeNumber * ProgressBarRun.Maximum; });
            }

            _isRunning = false;
            LabelRun.Dispatcher.BeginInvoke((ThreadStart) delegate
            {
                LabelRun.Content = "跑步完成";
                ButtonRun.Visibility = Visibility.Visible;
                ButtonStop.Visibility = Visibility.Hidden;
            });
        });

        _isRunning = true;
        runThread.Start();
    }

    private void StopRun(object sender, RoutedEventArgs e)
    {
        _isRunning = false;
    }
}