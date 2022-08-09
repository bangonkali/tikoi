using TdLib;

namespace tikoi.Extensions;

public static class FileExtension
{
    public static async Task Download(this TdApi.File file, TdClient client, int priority = 32)
    {
        await client.DownloadFileAsync(file.Id, priority, 0, 0, true);
    }
}