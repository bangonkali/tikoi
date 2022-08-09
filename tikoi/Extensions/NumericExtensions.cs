namespace tikoi.Extensions;

public static class NumericExtensions
{
    public static string PadByMax(this int value, int max)
    {
        return value.ToString().PadLeft(max.ToString().Length);
    }
}