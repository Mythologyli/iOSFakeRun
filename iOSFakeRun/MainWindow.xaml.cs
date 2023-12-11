using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using iOSFakeRun.FakeRun;
using Newtonsoft.Json.Linq;

namespace iOSFakeRun;

public partial class MainWindow
{
    private readonly IiDeviceApi _ideviceInstance = LibiMobileDevice.Instance.iDevice;
    private readonly ILockdownApi _lockdownInstance = LibiMobileDevice.Instance.Lockdown;
    private iDeviceHandle? _idevice;
    private bool _isRunning, _isPaused;
    private LockdownClientHandle? _lockdownClient;
    private int tmpPoint, tmpTime,totalRunTimes;
    private double metersPerSecond, barPercentage;
    private List<double[]> pointFixedList, pointList;
    
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
        _lockdownInstance.lockdownd_client_new_with_handshake(_idevice, out _lockdownClient, "iOSFakeRun")
            .ThrowOnError();

        if (!DeviceUtils.GetName(_lockdownClient, out var deviceName) ||
            !DeviceUtils.GetVersion(_lockdownClient, out var iosVersion))
        {
            _idevice?.Dispose();
            _lockdownClient?.Dispose();

            _idevice = null;
            _lockdownClient = null;

            MessageBox.Show("读取设备信息失败");
            return;
        }

        StatusBarTextBlock.Text = $"设备名称: {deviceName}    iOS 版本: {iosVersion}";

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

    public void RunThreadLogic(List<double[]> inPointFixedList, List<double[]> inPointList,double metersPerSecond, int totalRunTimes)
    {
        StatusBarTextBlock.Dispatcher.BeginInvoke((ThreadStart)delegate
        {
            ButtonRun.Visibility = Visibility.Hidden;
            ButtonStop.Visibility = Visibility.Visible;
            ButtonPause.Visibility = Visibility.Visible;
            IntegerUpDownRunTimes.IsEnabled = false;
            DoubleUpDownSpeed.IsEnabled = false;
            StatusBarTextBlock.Text = "正在跑步中...";
            ProgressBarRun.Value = 0.0;
        });

        var pointFixedNumber = inPointFixedList.Count;

        var pointTrueList = new List<double[]> { inPointFixedList[0] };

        for (var i = 0; i < pointFixedNumber - 1;)
        {
            var distance = CoordinateUtils.CalcDistance(inPointFixedList[i], inPointFixedList.Last());

            var j = i + 1;
            for (; j < pointFixedNumber; j++)
            {
                distance = CoordinateUtils.CalcDistance(inPointFixedList[i], inPointFixedList[j]);

                if (!(distance > metersPerSecond))
                {
                    continue;
                }

                break;
            }

            inPointList = CoordinateUtils.CutLineToPoints(inPointFixedList[i], inPointFixedList[j],
                (int)(distance / metersPerSecond));
            pointTrueList.AddRange(inPointList);

            i = j;
        }

        var pointTrueNumber = pointTrueList.Count;
        for (var time = tmpTime; time < totalRunTimes; time++)
        {
            for (var i = tmpPoint; i < pointTrueNumber; i++)
            {
                tmpPoint = i;
                tmpTime = time;
                if (!_isRunning ||
                    !Location.SetLocation(_idevice, _lockdownClient, pointTrueList[i][0], pointTrueList[i][1]))
                {
                    if (_isRunning)
                    {
                        _isRunning = false;
                        MessageBox.Show("修改定位失败\n请检查是否连接并尝试再次点击连接按钮");
                    }

                    if (_isPaused)
                    {
                        StatusBarTextBlock.Dispatcher.BeginInvoke((ThreadStart)delegate
                        {
                            StatusBarTextBlock.Text = "未在跑步状态";
                            ButtonRun.Visibility = Visibility.Hidden;
                            ButtonPause.Visibility = Visibility.Hidden;
                            ButtonStop.Visibility = Visibility.Visible;
                            ButtonResume.Visibility = Visibility.Visible;
                            IntegerUpDownRunTimes.IsEnabled = true;
                            DoubleUpDownSpeed.IsEnabled = true;
                            ProgressBarRun.Value = barPercentage;

                        });
                    }
                    else
                    {
                        StatusBarTextBlock.Dispatcher.BeginInvoke((ThreadStart)delegate
                        {
                            StatusBarTextBlock.Text = "未在跑步状态";
                            ButtonRun.Visibility = Visibility.Visible;
                            ButtonPause.Visibility = Visibility.Hidden;
                            ButtonStop.Visibility = Visibility.Hidden;
                            IntegerUpDownRunTimes.IsEnabled = true;
                            DoubleUpDownSpeed.IsEnabled = true;
                            ProgressBarRun.Value = barPercentage;
                        });
                    }

                    return;
                }

                Thread.Sleep(1000);
                i++;
                var iOut = i;
                var timeNow = time;
                ProgressBarRun.Dispatcher.BeginInvoke((ThreadStart)delegate
                {
                    ProgressBarRun.Value =
                        ((double)iOut / (pointTrueNumber * totalRunTimes) + (double)timeNow / totalRunTimes) *
                        ProgressBarRun.Maximum;
                        barPercentage =ProgressBarRun.Value;
                });
            }
        }

        _isRunning = false;
        StatusBarTextBlock.Dispatcher.BeginInvoke((ThreadStart)delegate
        {
            StatusBarTextBlock.Text = "跑步完成";
            ButtonRun.Visibility = Visibility.Visible;
            ButtonStop.Visibility = Visibility.Hidden;
            ButtonPause.Visibility = Visibility.Hidden;
            ButtonResume.Visibility = Visibility.Hidden;
            IntegerUpDownRunTimes.IsEnabled = true;
            DoubleUpDownSpeed.IsEnabled = true;
        });
    }

    private void StartRun(object sender, RoutedEventArgs e)
    {
        var pointText = TextBoxRoute.Text;
        pointList = new List<double[]>();
        tmpTime = 0;
        tmpPoint = 0;
        barPercentage = 0.0;
        try
        {
            if (!pointText.Substring(0, 1).Equals("["))
            {
                pointText = "[" + pointText + "]";
            }

            var array = JArray.Parse(pointText);
            foreach (var position in array)
            {
                var latitudeToken = position.SelectToken("lat");
                var longitudeToken = position.SelectToken("lng");

                if (latitudeToken == null || longitudeToken == null)
                {
                    var errorLogWriter = new StreamWriter("./error.log", false);
                    errorLogWriter.WriteLine("Time: " + DateTime.Now);
                    errorLogWriter.WriteLine("Windows Version: " + Environment.OSVersion);
                    errorLogWriter.WriteLine("Windows Language: " + CultureInfo.InstalledUICulture.Name);
                    errorLogWriter.WriteLine("Point Text: " + pointText);
                    errorLogWriter.WriteLine("Current Latitude Token: " + latitudeToken);
                    errorLogWriter.WriteLine("Current Longitude Token: " + longitudeToken);

                    errorLogWriter.Close();

                    MessageBox.Show("解析路径数据失败\n请将路径从网页复制到左侧文本框内并确保数据格式合法");
                    return;
                }

                var latitude = double.Parse(latitudeToken.ToString());
                var longitude = double.Parse(longitudeToken.ToString());

                double[] route = { latitude, longitude };
                pointList.Add(route);
            }
        }
        catch (Exception exception)
        {
            var errorLogWriter = new StreamWriter("./error.log", false);
            errorLogWriter.WriteLine("Time: " + DateTime.Now);
            errorLogWriter.WriteLine("Windows Version: " + Environment.OSVersion);
            errorLogWriter.WriteLine("Windows Language: " + CultureInfo.InstalledUICulture.Name);
            errorLogWriter.WriteLine("Point Text: " + pointText);
            errorLogWriter.WriteLine("Exception: " + exception.Message);

            errorLogWriter.Close();

            MessageBox.Show("解析路径数据失败\n请将路径从网页复制到左侧文本框内并确保数据格式合法");
            return;
        }

        var writer = new StreamWriter("./route.save", false);
        writer.WriteLine(pointText);
        writer.Close();

        pointFixedList = pointList.Select(point => CoordinateUtils.Bd09ToWgs84(point[0], point[1])).ToList();
        
        metersPerSecond = DoubleUpDownSpeed.Value ?? 0.0;
        totalRunTimes = IntegerUpDownRunTimes.Value ?? 1;

        if (totalRunTimes > 1)
        {
            pointFixedList.Add(pointFixedList[0]);
        }
        
        var runThread = new Thread(() => RunThreadLogic(pointFixedList, pointList, metersPerSecond, totalRunTimes));

        _isRunning = true;
        _isPaused = false;
        runThread.Start();
    }

    private void PauseRun(object sender, RoutedEventArgs e)
    {
        _isRunning = false;
        _isPaused = true;
        ButtonResume.Visibility = Visibility.Visible;
        ButtonPause.Visibility = Visibility.Hidden;
        ButtonRun.Visibility = Visibility.Hidden;
    }

    private void ResumeRun(object sender, RoutedEventArgs e)
    {
        _isRunning = true;
        _isPaused = false;
        ButtonPause.Visibility = Visibility.Visible;
        ButtonResume.Visibility = Visibility.Hidden;
        ButtonRun.Visibility = Visibility.Hidden;
        
        var runThread = new Thread(() => RunThreadLogic(pointFixedList, pointList, metersPerSecond, totalRunTimes));
        runThread.Start();
    }

    private void StopRun(object sender, RoutedEventArgs e)
    {
        _isRunning = false;
        _isPaused = false;
        ButtonRun.Visibility = Visibility.Visible;
        ButtonStop.Visibility = Visibility.Hidden;
        ButtonPause.Visibility = Visibility.Hidden;
        ButtonResume.Visibility = Visibility.Hidden;
    }

    private void About(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("iOS Fake Run\n版本: v0.9\n作者: Myth\n\n请勿将本工具用于任何非法用途");
    }
}