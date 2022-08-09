using TdLib;

namespace tikoi.Extensions;

public static class MessageVideoExtension
{
    public static string Preview(this TdApi.MessageContent.MessageVideo content)
    {
        return $"[{content.Video.Duration}] {content.Caption.Preview()}";
    }
}