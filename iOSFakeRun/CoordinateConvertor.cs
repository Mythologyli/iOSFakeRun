using System;


namespace iOSFakeRun;

internal static class CoordinateConvertor
{
    private const double XPi = 3.14159265358979324 * 3000.0 / 180.0;
    private const double Pi = 3.1415926535897932384626;
    private const double A = 6378245.0;
    private const double E = 0.00669342162296594323;

    public static double[] Bd09ToWgs84(double latitude, double longitude)
    {
        var x = longitude - 0.0065;
        var y = latitude - 0.006;
        var z = Math.Sqrt(x * x + y * y) - 0.00002 * Math.Sin(y * XPi);
        var theta = Math.Atan2(y, x) - 0.000003 * Math.Cos(x * XPi);

        var gcjLatitude = z * Math.Sin(theta);
        var gcjLongitude = z * Math.Cos(theta);

        var dLatitude = TransformLatitude(gcjLatitude - 35.0, gcjLongitude - 105.0);
        var dLongitude = TransformLongitude(gcjLatitude - 35.0, gcjLongitude - 105.0);

        var radLatitude = gcjLatitude / 180.0 * Pi;
        var magic = Math.Sin(radLatitude);
        magic = 1 - E * magic * magic;
        var sqrtMagic = Math.Sqrt(magic);

        dLongitude = dLongitude * 180.0 / (A / sqrtMagic * Math.Cos(radLatitude) * Pi);
        dLatitude = dLatitude * 180.0 / (A * (1 - E) / (magic * sqrtMagic) * Pi);

        var mgLatitude = gcjLatitude + dLatitude;
        var mgLongitude = gcjLongitude + dLongitude;

        var wgsLatitude = gcjLatitude * 2 - mgLatitude;
        var wgsLongitude = gcjLongitude * 2 - mgLongitude;

        double[] wgs = {wgsLatitude, wgsLongitude};
        return wgs;
    }

    private static double TransformLatitude(double latitude, double longitude)
    {
        var ret = -100.0 + 2.0 * longitude + 3.0 * latitude + 0.2 * latitude * latitude + 0.1 * longitude * latitude + 0.2 * Math.Sqrt(Math.Abs(longitude));
        ret += (20.0 * Math.Sin(6.0 * longitude * Pi) + 20.0 * Math.Sin(2.0 * longitude * Pi)) * 2.0 / 3.0;
        ret += (20.0 * Math.Sin(latitude * Pi) + 40.0 * Math.Sin(latitude / 3.0 * Pi)) * 2.0 / 3.0;
        ret += (160.0 * Math.Sin(latitude / 12.0 * Pi) + 320 * Math.Sin(latitude * Pi / 30.0)) * 2.0 / 3.0;

        return ret;
    }

    private static double TransformLongitude(double latitude, double longitude)
    {
        var ret = 300.0 + longitude + 2.0 * latitude + 0.1 * longitude * longitude + 0.1 * longitude * latitude + 0.1 * Math.Sqrt(Math.Abs(longitude));
        ret += (20.0 * Math.Sin(6.0 * longitude * Pi) + 20.0 * Math.Sin(2.0 * longitude * Pi)) * 2.0 / 3.0;
        ret += (20.0 * Math.Sin(longitude * Pi) + 40.0 * Math.Sin(longitude / 3.0 * Pi)) * 2.0 / 3.0;
        ret += (150.0 * Math.Sin(longitude / 12.0 * Pi) + 300.0 * Math.Sin(longitude / 30.0 * Pi)) * 2.0 / 3.0;

        return ret;
    }
}