using System.Numerics;
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

    public static string FormatAbbreviated(BigInteger value)
    {
        if (value < 1000)
        {
            return value.ToString();
        }

        int unitIndex = 0;
        BigInteger divisor = BigInteger.One;
        BigInteger thousand = 1000;

        while (value >= divisor * thousand && unitIndex < s_unitCache.Length - 1)
        {
            divisor *= thousand;
            unitIndex++;
        }

        BigInteger wholePart = value / divisor;
        BigInteger remainder = value % divisor;
        BigInteger decimalPart = (remainder * 100) / divisor;

        string unit = s_unitCache[unitIndex];

        if (decimalPart > 0)
        {
            string decimalStr = decimalPart.ToString().PadLeft(2, '0').TrimEnd('0');
            if (!string.IsNullOrEmpty(decimalStr))
            {
                return $"{wholePart}.{decimalStr}{unit}";
            }
        }

        return $"{wholePart}{unit}";
    }

    public static string FormatWithComma(BigInteger value)
    {
        return value.ToString("N0");
    }

    public static string FormatKorean(BigInteger value)
    {
        if (value == 0)
        {
            return "0";
        }

        string[] units = { "", "만", "억", "조", "경", "해" };
        var sb = new StringBuilder();
        int unitIndex = 0;
        BigInteger tenThousand = 10000;

        while (value > 0 && unitIndex < units.Length)
        {
            BigInteger part = value % tenThousand;
            if (part > 0)
            {
                sb.Insert(0, part.ToString() + units[unitIndex]);
            }
            value /= tenThousand;
            unitIndex++;
        }

        return sb.ToString();
    }
}
