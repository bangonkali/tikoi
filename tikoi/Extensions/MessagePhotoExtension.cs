using TdLib;

namespace tikoi.Extensions;

public static class MessagePhotoExtension
{
    public static async Task Download(this TdApi.MessageContent.MessagePhoto content, TdClient client)
    {
        var photo = content.Photo.Sizes.MaxBy(c => c.Height * c.Width);
        if (photo is not null) await photo.Photo.Download(client);
    }

    public static string Preview(this TdApi.MessageContent.MessagePhoto content)
    {
        var photo = content.Photo.Sizes.MaxBy(c => c.Height * c.Width);
        if (photo is not null) 
            return $"[{photo.Width}x{photo.Height}] {content.Caption.Preview()}";
        return content.Caption.Preview();
    }
}