using System.Globalization;
using System.Text.RegularExpressions;

namespace JC.Colors;
public static class ColorConverter
{
    private static readonly Regex RegexHEX = new(@"^#?([0-9a-fA-F]{3}){1,2}$", RegexOptions.Compiled);

    private static readonly Regex RegexRGB = new(@"^rgb\((?:([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5]),?\s?)(?:([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5]),?\s?)(?:([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5]))\)$", RegexOptions.Compiled);

    private static readonly Regex RegexHSL = new(@"^hsl\((?:([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-9][0-9]|3[0-5][0-9]|360),?\s?)(?:([0-9]|[1-9][0-9]|100)%,?\s?)(?:([0-9]|[1-9][0-9]|100)%,?\s?)\)$", RegexOptions.Compiled);

    private const double TOLERANCE = 0.0000001;

    public static RGBColor? Parse(string input)
    {

        Match matchHEX = RegexHEX.Match(input);
        if (matchHEX.Success)
        {
            return HEX2RGB(matchHEX.Groups[0].Value);
        }

        Match matchRGB = RegexRGB.Match(input);
        if (matchRGB.Success && int.TryParse(matchRGB.Groups[1].Value, out int r) && int.TryParse(matchRGB.Groups[2].Value, out int g) && int.TryParse(matchRGB.Groups[3].Value, out int b))
        {
            return new RGBColor(r, g, b);
        }

        Match matchHSL = RegexHSL.Match(input);
        if (matchHSL.Success && double.TryParse(matchHSL.Groups[1].Value, out double h) && double.TryParse(matchHSL.Groups[2].Value, out double s) && double.TryParse(matchHSL.Groups[3].Value, out double l))
        {
            return HSL2RGB(new HSLColor(h, s, l));
        }

        return null;
    }

    public static RGBColor? HEX2RGB(string hex)
    {
        if (hex[0] == '#')
        {
            hex = hex[1..];
        }

        return hex.Length switch
        {
            3 when int.TryParse(new string(new[] { hex[0], hex[0] }), NumberStyles.HexNumber, null, out int r) &&
                   int.TryParse(new string(new[] { hex[1], hex[1] }), NumberStyles.HexNumber, null, out int g) &&
                   int.TryParse(new string(new[] { hex[2], hex[2] }), NumberStyles.HexNumber, null, out int b) => new RGBColor(r, g, b),
            6 when int.TryParse(new string(new[] { hex[0], hex[1] }), NumberStyles.HexNumber, null, out int r) &&
                   int.TryParse(new string(new[] { hex[2], hex[3] }), NumberStyles.HexNumber, null, out int g) &&
                   int.TryParse(new string(new[] { hex[4], hex[5] }), NumberStyles.HexNumber, null, out int b) => new RGBColor(r, g, b),
            _ => null
        };
    }

    public static HSLColor? HEX2HSL(string hex)
    {
        RGBColor? rgbColor = HEX2RGB(hex);
        return rgbColor == null ? null : RGB2HSL(rgbColor.Value);
    }

    public static string? RGB2HEX(RGBColor rgbColor)
    {
        int r = Math.Max(Math.Min(rgbColor.R, 255), 0);
        int g = Math.Max(Math.Min(rgbColor.G, 255), 0);
        int b = Math.Max(Math.Min(rgbColor.B, 255), 0);
        
        string hexR = r > 15 ? r.ToString("X") : "0" + r.ToString("X");
        string hexG = g > 15 ? g.ToString("X") : "0" + g.ToString("X");
        string hexB = b > 15 ? b.ToString("X") : "0" + b.ToString("X");

        return $"#{hexR}{hexG}{hexB}";
    }

    
    public static HSLColor? RGB2HSL(RGBColor rgbColor)
    {
        double r = Math.Max(Math.Min(rgbColor.R / 255d, 1), 0);
        double g = Math.Max(Math.Min(rgbColor.G / 255d, 1), 0);
        double b = Math.Max(Math.Min(rgbColor.B / 255d, 1), 0);
        double max = Math.Max(Math.Max(r, g), b);
        double min = Math.Min(Math.Min(r, g), b);
        double l = (max + min) / 2;
        double h, s;

        if (Math.Abs(max - min) > TOLERANCE)
        {
            double d = max - min;
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            if (Math.Abs(max - r) < TOLERANCE)
            {
                h = (g - b) / d + (g < b ? 6 : 0);
            } 
            else if (Math.Abs(max - g) < TOLERANCE)
            {
                h = (b - r) / d + 2;
            }
            else
            {
                h = (r - g) / d + 4;
            }

            h = h / 6;
        }
        else
        {
            h = s = 0;
        }

        return new HSLColor(Math.Round(h * 360), Math.Round(s * 100), Math.Round(l * 100));
    }

    public static string? HSL2HEX(HSLColor hslColor)
    {
        var rgbColor = HSL2RGB(hslColor);
        return rgbColor == null ? null : RGB2HEX(rgbColor.Value);
    }

    public static RGBColor? HSL2RGB(HSLColor hslColor)
    {
        double h = Math.Max(Math.Min(hslColor.H, 360), 0) / 360d;
        double s = Math.Max(Math.Min(hslColor.S, 100), 0) / 100d;
        double l = Math.Max(Math.Min(hslColor.L, 100), 0) / 100d;

        double v;

        if (l <= 0.5)
        {
            v = l * (1 + s);
        }
        else
        {
            v = l + s - l * s;
        }

        if (v == 0)
        {
            return new RGBColor(0, 0, 0);
        }

        double min = 2 * l - v;
        double sv = (v - min) / v;
        h = 6 * h;
        double six = Math.Floor(h);
        double fract = h - six;
        double vsfract = v * sv * fract;
        double r;
        double g;
        double b;
        switch (six)
        {
            case 1:
                {
                    r = v - vsfract;
                    g = v;
                    b = min;
                    break;
                }
            case 2:
                {
                    r = min;
                    g = v;
                    b = min + vsfract;
                    break;
                }
            case 3:
                {
                    r = min;
                    g = v - vsfract;
                    b = v;
                    break;
                }
            case 4:
                {
                    r = min + vsfract;
                    g = min;
                    b = v;
                    break;
                }
            case 5:
                {
                    r = v;
                    g = min;
                    b = v - vsfract;
                    break;
                }
            default:
                {
                    r = v;
                    g = min + vsfract;
                    b = min;
                    break;
                }
        }

        return new RGBColor((int)Math.Round(r * 255), (int)Math.Round(g * 255), (int)Math.Round(b * 255));
    }

}

public struct RGBColor
{
    public int R { get; set; }

    public int G { get; set; }

    public int B { get; set; }

    public RGBColor(int r, int g, int b)
    {
        R = r;
        G = g;
        B = b;
    }
}


public struct HSLColor
{
    public double H { get; set; }

    public double S { get; set; }

    public double L { get; set; }

    public HSLColor(double h, double s, double l)
    {
        H = h;
        S = s;
        L = l;
    }
}
