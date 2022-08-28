using tikoi.Extensions;

namespace tikoi.Core;

using TdLib;

public class Engine
{
    private TdClient? _client;
    private string? _phoneNumber;
    private string? _password;
    private string? _dir;
    private TdApi.TdlibParameters? _libParams;

    private volatile bool _isOnAuthLoop = true;

    private async Task InitParams()
    {
        if (_libParams == null)
        {
            _dir = Environment.GetEnvironmentVariable(variable: "TIKOI_HOME");
            if (_dir != null && string.IsNullOrEmpty(value: _dir) && _dir.Length < 3)
                throw new Exception(message: "Undefined environment variable TIKOI_HOME.");
            if (!Directory.Exists(path: _dir))
                Directory.CreateDirectory(path: _dir!);
            if (!Directory.Exists(path: _dir))
                throw new Exception(message: "Path not exist environment variable TIKOI_HOME.");

            _password = Environment.GetEnvironmentVariable(variable: "TIKOI_PASSWORD");
            if (_password != null && string.IsNullOrEmpty(value: _password) && _password.Length < 3)
                throw new Exception(message: "Undefined environment variable TIKOI_PASSWORD.");

            _phoneNumber = Environment.GetEnvironmentVariable(variable: "TIKOI_PHONE_NUMBER");
            if (_phoneNumber != null && string.IsNullOrEmpty(value: _phoneNumber) && _phoneNumber.Length < 10)
                throw new Exception(message: "Undefined environment variable TIKOI_PHONE_NUMBER.");

            string? strAppId = Environment.GetEnvironmentVariable(variable: "TIKOI_API_ID");
            if (string.IsNullOrEmpty(value: strAppId))
                throw new Exception(message: "Undefined environment variable TIKOI_API_ID.");
            if (!int.TryParse(s: strAppId, result: out var appId))
                throw new Exception(message: "Invalid environment variable TIKOI_API_ID.");

            string? strApiHash = Environment.GetEnvironmentVariable(variable: "TIKOI_API_HASH");
            if (strApiHash != null && string.IsNullOrEmpty(value: strApiHash) && strApiHash.Length < 10)
                throw new Exception(message: "Undefined environment variable TIKOI_API_HASH.");
            _libParams = new()
            {
                DatabaseDirectory = _dir,
                SystemLanguageCode = "en",
                DeviceModel = "Desktop",
                UseSecretChats = true,
                UseMessageDatabase = true,
                EnableStorageOptimizer = true,
                ApplicationVersion = "0.0.1",
                ApiHash = strApiHash,
                ApiId = appId,
            };
        }

        _client ??= new TdClient();
        await _client.SetLogVerbosityLevelAsync();

        // _client!.UpdateReceived += async (_, update) =>
        // {
        //     // Console.WriteLine($"DEBUG1: {update.DataType} - Extra: {update.Extra}");
        //
        //     if (update is TdApi.Update.UpdateAuthorizationState)
        //     {
        //         var authState = await _client.GetAuthorizationStateAsync();
        //         Console.WriteLine($"DEBUG2: {authState.DataType} - Extra: {authState.Extra}");
        //     }
        // };
    }

    private void Clean()
    {
        Directory.Delete(path: _dir!, recursive: true);
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
                    Console.WriteLine(value: $"Set Encryption Key");
                    break;
                case TdApi.AuthorizationState.AuthorizationStateWaitTdlibParameters:
                    await _client.SetTdlibParametersAsync(parameters: _libParams);
                    Console.WriteLine(value: $"Set Library Params");
                    break;
                case TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber:
                    await _client.SetAuthenticationPhoneNumberAsync(phoneNumber: _phoneNumber);
                    Console.WriteLine(value: $"Set Phone Number {_phoneNumber}");
                    break;
                case TdApi.AuthorizationState.AuthorizationStateWaitPassword:
                    await _client.CheckAuthenticationPasswordAsync(password: _password);
                    Console.WriteLine(value: "Set Password. Ok.");
                    break;
                case TdApi.AuthorizationState.AuthorizationStateWaitCode:
                {
                    Console.Write(value: "Enter the code sent to the device: ");
                    var code = Console.ReadLine();
                    await _client.CheckAuthenticationCodeAsync(code: code);
                    Console.WriteLine(value: "Set Authentication Code Ok");
                    break;
                }
                case TdApi.AuthorizationState.AuthorizationStateReady:
                {
                    _isOnAuthLoop = false;
                    Console.WriteLine(value: "Authentication Ok");
                    break;
                }
                case TdApi.AuthorizationState.AuthorizationStateClosed:
                {
                    _isOnAuthLoop = false;
                    Console.WriteLine(value: "Authentication Closed");
                    break;
                }
                case TdApi.AuthorizationState.AuthorizationStateLoggingOut:
                {
                    _isOnAuthLoop = false;
                    Console.WriteLine(value: "Authentication Logging Out");
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
            await file.Download(client: _client!);
            Console.Write(value: "Downloaded. ");
        }
        else
        {
            Console.Write(value: "Skipped download. ");
        }
    }

    private async Task DownloadMessage(TdApi.Message msg, bool download)
    {
        switch (msg.Content)
        {
            case TdApi.MessageContent.MessageVideo content:

                Console.Write(value: $"{content.Preview()} ... ");
                await SkipOrDownload(file: content.Video.Video_, download: download);
                break;
            case TdApi.MessageContent.MessagePhoto content:
                Console.Write(value: $"{content.Preview()} ... ");
                if (download) await content.Download(client: _client!);
                Console.Write(value: download ? "Downloaded" : "Skipped download");
                break;
            case TdApi.MessageContent.MessageVideoNote content:
                Console.Write(value: $"[{content.VideoNote.Duration}] ... ");
                await SkipOrDownload(file: content.VideoNote.Video, download: download);
                break;
            case TdApi.MessageContent.MessageText content:
                Console.Write(value: $"{content.Text.Preview()}");
                break;
            default:
                Console.Write(value: "");
                break;
        }
    }

    private async Task IntrospectMessage(TdApi.Chat chat, long messageId, bool download, int max)
    {
        var msg = await _client.GetMessageAsync(chatId: chat.Id, messageId: messageId);
        var logTag = $"[{msg.Id}:{msg.Content.DataType}] - ";
        Console.Write(value: $"{logTag}");

        await DownloadMessage(msg: msg, download: download);

        if (msg.CanGetMessageThread)
        {
            try
            {
                var thread = await _client.GetMessageThreadAsync(chatId: chat.Id, messageId: messageId);
                var lastMessage = thread.ReplyInfo.LastMessageId;
                if (thread.ReplyInfo.ReplyCount > 0)
                {
                    Console.WriteLine(value: $"Downloading Replies {thread.ReplyInfo.ReplyCount}... ");
                    var collected = 0;
                    var indexId = 0;
                    while (indexId < thread.ReplyInfo.ReplyCount)
                    {
                        var offset = collected == 0 ? -1 : 0;
                        var page = 10;

                        var messages = await _client.GetMessageThreadHistoryAsync(
                            chatId: chat.Id,
                            messageId: messageId,
                            fromMessageId: lastMessage,
                            offset: offset,
                            limit: page
                        );
                        var threadMessageIds = messages.Messages_; // get 10 messages from lastMessage
                        if (!(messages.Messages_?.Length > 0)) break; // stop loop if no more content

                        lastMessage = threadMessageIds[^1].Id; // isolate last message
                        collected += threadMessageIds.Length; // offset collected with new stuff

                        foreach (var threadMessage in threadMessageIds)
                        {
                            Console.Write(value: $"\t{indexId.PadByMax(max: max)} ");
                            await DownloadMessage(msg: threadMessage, download: download);
                            indexId++;
                            Console.WriteLine();
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        Console.WriteLine();
    }

    public async Task ChatMessages(long chatId, int max, bool download)
    {
        await InitParams();
        await AuthorizeLoop();

        Console.WriteLine(value: $"Downloading messages from {chatId}");
        var chat = await _client.GetChatAsync(chatId: chatId);
        var lastMessage = chat.LastMessage;
        var collected = 0;
        var indexId = 0;
        while (collected < max)
        {
            var offset = collected == 0 ? -1 : 0;

            var page = 10;
            if (collected + page > max) page = max - collected;

            var messages = await _client.GetChatHistoryAsync(chatId: chatId, fromMessageId: lastMessage.Id,
                offset: offset, limit: page);
            var messageIds = messages.Messages_; // get 10 messages from lastMessage

            if (!(messages.Messages_?.Length > 0)) break; // stop loop if no more content

            lastMessage = messageIds.Last(); // isolate last message
            collected += messageIds.Length; // offset collected with new stuff

            foreach (var message in messageIds)
            {
                Console.Write(value: $"{indexId.PadByMax(max: max)} ");
                try
                {
                    await IntrospectMessage(chat: chat, messageId: message.Id, download: download, max: max);
                }
                catch
                {
                    Console.WriteLine("Unable to find Message");
                }

                indexId++;
            }
        }
    }

    public async Task Update()
    {
        await InitParams();
        await AuthorizeLoop();
    }

    public async Task Chats()
    {
        await InitParams();
        await AuthorizeLoop();

        Console.WriteLine(value: $"Downloading chats");
        var mainList = new TdApi.ChatList.ChatListMain();
        var groups = await _client.GetChatsAsync(chatList: mainList, limit: 100);
        var chatIds = groups.ChatIds;
        foreach (var chatId in chatIds)
        {
            var chat = await _client.GetChatAsync(chatId: chatId);
            if (chat is not null)
            {
                Console.WriteLine(value: $"{chat.Type} [{chatId}] -> {chat.Title}");
            }
        }
    }

    public async Task Login()
    {
        await InitParams();
        await AuthorizeLoop();
    }

    public async Task Logout()
    {
        await InitParams();
        await AuthorizeLoop();
        await _client.LogOutAsync();
        Clean();
    }
}