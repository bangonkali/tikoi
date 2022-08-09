using TdLib;

namespace tikoi.Extensions;

public static class FormattedTextExtension
{
    public static string Preview(this TdApi.FormattedText text, int length = 24)
    {
        var value = text.Text.Replace(Environment.NewLine, " ");
        return value.Length > length ? value[..length] : value;
    }
}