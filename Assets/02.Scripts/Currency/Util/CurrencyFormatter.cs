using System;
using System.Text;

public static class CurrencyFormatter
{
    private static readonly string[] s_unitCache = GenerateUnits(100);

    private static string[] GenerateUnits(int count)
    {
        var units = new string[count];
        units[0] = "";

        for (int i = 1; i < count; i++)
        {
            units[i] = GetUnitString(i);
        }

        return units;
    }

    private static string GetUnitString(int index)
    {
        if (index <= 0)
        {
            return "";
        }

        var sb = new StringBuilder();
        int current = index;

        while (current > 0)
        {
            current--;
            char c = (char)('A' + (current % 26));
            sb.Insert(0, c);
            current /= 26;
        }

        return sb.ToString();
    }

    public static string FormatAbbreviated(double value)
    {
        if (value < 1000)
        {
            return value.ToString("F0");
        }

        int unitIndex = 0;
        double divisor = 1;
        double thousand = 1000;

        while (value >= divisor * thousand && unitIndex < s_unitCache.Length - 1)
        {
            divisor *= thousand;
            unitIndex++;
        }

        double wholePart = Math.Floor(value / divisor);
        double remainder = value % divisor;
        double decimalPart = Math.Floor((remainder * 100) / divisor);

        string unit = s_unitCache[unitIndex];

        if (decimalPart > 0)
        {
            string decimalStr = ((int)decimalPart).ToString().PadLeft(2, '0').TrimEnd('0');
            if (!string.IsNullOrEmpty(decimalStr))
            {
                return $"{(long)wholePart}.{decimalStr}{unit}";
            }
        }

        return $"{(long)wholePart}{unit}";
    }

    public static string FormatWithComma(double value)
    {
        return value.ToString("N0");
    }

    public static string FormatKorean(double value)
    {
        if (value == 0)
        {
            return "0";
        }

        string[] units = { "", "만", "억", "조", "경", "해" };
        var sb = new StringBuilder();
        int unitIndex = 0;
        double remaining = value;
        double tenThousand = 10000;

        while (remaining > 0 && unitIndex < units.Length)
        {
            double part = remaining % tenThousand;
            if (part > 0)
            {
                sb.Insert(0, ((long)part).ToString() + units[unitIndex]);
            }
            remaining /= tenThousand;
            unitIndex++;
        }

        return sb.ToString();
    }
}
