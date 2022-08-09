using tikoi.Extensions;

namespace tikoi.Core;

using System.Diagnostics;
using TdLib;

public class Engine
{
    private readonly TdClient _client;
    private string? _phoneNumber;
    private string? _password;
    private readonly TdApi.TdlibParameters _libParams;

    private volatile bool _isOnAuthLoop = true;

    public Engine(TdClient client, TdApi.TdlibParameters libParams)
    {
        _client = client;
        _libParams = libParams;

        Subscribe();
    }

    private void Subscribe()
    {
        _client.UpdateReceived += async (sender, update) =>
        {
            Debug.WriteLine($"DEBUG1: {update.DataType} - Extra: {update.Extra}");

            if (update is TdApi.Update.UpdateAuthorizationState)
            {
                var authState = await _client.GetAuthorizationStateAsync();
                Debug.WriteLine($"DEBUG2: {authState.DataType} - Extra: {authState.Extra}");
            }
        };
    }

    private void Clean()
    {
        Directory.Delete(_libParams.FilesDirectory, true);
    }

    private async Task AuthorizeLoop()
    {
        while (_isOnAuthLoop)
        {
            var authState = await _client.GetAuthorizationStateAsync();
            switch (authState)
            {
                case TdApi.AuthorizationState.AuthorizationStateWaitEncryptionKey:
                    await _client.CheckDatabaseEncryptionKeyAsync();
                    Console.WriteLine($"Set Encryption Key");
                    break;
                case TdApi.AuthorizationState.AuthorizationStateWaitTdlibParameters:
                    await _client.SetTdlibParametersAsync(_libParams);
                    Console.WriteLine($"Set Library Params");
                    break;
                case TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber:
                    await _client.SetAuthenticationPhoneNumberAsync(_phoneNumber);
                    Console.WriteLine($"Set Phone Number {_phoneNumber}");
                    break;
                case TdApi.AuthorizationState.AuthorizationStateWaitPassword:
                    await _client.CheckAuthenticationPasswordAsync(_password);
                    Console.WriteLine("Set Password. Ok.");
                    break;
                case TdApi.AuthorizationState.AuthorizationStateWaitCode:
                {
                    Console.Write("Enter the code sent to the device: ");
                    var code = Console.ReadLine();
                    await _client.CheckAuthenticationCodeAsync(code);
                    Console.WriteLine("Set Authentication Code Ok");
                    break;
                }
                case TdApi.AuthorizationState.AuthorizationStateReady:
                {
                    _isOnAuthLoop = false;
                    Console.WriteLine("Authentication Ok");
                    break;
                }
                case TdApi.AuthorizationState.AuthorizationStateClosed:
                {
                    _isOnAuthLoop = false;
                    Console.WriteLine("Authentication Closed");
                    break;
                }
                case TdApi.AuthorizationState.AuthorizationStateLoggingOut:
                {
                    _isOnAuthLoop = false;
                    Console.WriteLine("Authentication Logging Out");
                    break;
                }
                default:
                    _isOnAuthLoop = false;
                    break;
            }
        }
    }

    private async Task SkipOrDownload(TdApi.File file, bool download)
    {
        if (download)
        {
            await file.Download(_client);
            Console.WriteLine("Downloaded");
        }
        else
        {
            Console.WriteLine("Skipped download");
        }
    }

    private async Task IntrospectMessage(TdApi.Chat chat, long messageId, bool download)
    {
        var msg = await _client.GetMessageAsync(chat.Id, messageId);
        Console.Write($"[{msg.Id}:{msg.Content.DataType}] - ");
        switch (msg.Content)
        {
            case TdApi.MessageContent.MessageVideo content:
                Console.Write($"{content.Preview()} ... ");
                await SkipOrDownload(content.Video.Video_, download);
                break;
            case TdApi.MessageContent.MessagePhoto content:
                Console.Write($"{content.Preview()} ... ");
                if (download) await content.Download(_client);
                Console.WriteLine(download ? "Downloaded" : "Skipped download");
                break;
            case TdApi.MessageContent.MessageVideoNote content:
                Console.Write($"[{content.VideoNote.Duration}] ... ");
                await SkipOrDownload(content.VideoNote.Video, download);
                break;
            case TdApi.MessageContent.MessageText content:
                Console.WriteLine($"{content.Text.Preview()}");
                break;
            default:
                Console.WriteLine("");
                break;
        }
    }

    public async Task ChatMessages(long chatId, int max, bool download)
    {
        await AuthorizeLoop();

        Console.WriteLine($"Downloading messages from {chatId}");
        var chat = await _client.GetChatAsync(chatId);
        var lastMessage = chat.LastMessage;
        var collected = 0;
        var indexId = 0;
        while (collected < max)
        {
            var offset = collected == 0 ? -1 : 0;

            var page = 10;
            if (collected + page > max) page = max - collected;

            var messages = await _client.GetChatHistoryAsync(chatId, lastMessage.Id, offset, page, false);
            var messageIds = messages.Messages_; // get 10 messages from lastMessage

            if (!(messages.Messages_?.Length > 0)) continue; // stop loop if no more content

            lastMessage = messageIds.Last(); // isolate last message
            collected += messageIds.Length; // offset collected with new stuff

            foreach (var message in messageIds)
            {
                Console.Write($"{indexId.PadByMax(max)} ");
                await IntrospectMessage(chat, message.Id, download);
                indexId++;
            }
        }
    }

    public async Task Chats()
    {
        await AuthorizeLoop();

        Console.WriteLine($"Downloading chats");
        var groups = await _client.GetChatsAsync(null, 100);
        var chatIds = groups.ChatIds;
        foreach (var chatId in chatIds)
        {
            var chat = await _client.GetChatAsync(chatId);
            if (chat is not null)
            {
                Console.WriteLine($"{chat.Type} [{chatId}] -> {chat.Title}");
            }
        }
    }

    public async Task Login(string phoneNumber, string password)
    {
        _phoneNumber = phoneNumber;
        _password = password;

        await AuthorizeLoop();
    }

    public async Task Logout()
    {
        await AuthorizeLoop();
        await _client.LogOutAsync();
        Clean();
    }
}