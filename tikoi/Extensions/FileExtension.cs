using TdLib;

namespace tikoi.Extensions;

public static class FileExtension
{
    public static async Task Download(this TdApi.File file, TdClient client, int priority = 32)
    {
        if (file.Local.IsDownloadingCompleted)
        {
            Console.Write(".. Was downloaded. ");
        }
        else
        {
            await client.DownloadFileAsync(file.Id, priority, 0, 0, true);
        }
    }
}