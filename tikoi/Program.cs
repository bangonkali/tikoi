using System.CommandLine;
using TdLib;
using tikoi.Core;

namespace tikoi;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var engine = new Engine();
        var rootCommand = new RootCommand("This is a very simple Telegram CLI application.");

        #region Login Command

        var loginCommand = new Command("login", "Login using phone number and an authorization code sent to the app.")
        {
            IsHidden = false,
            TreatUnmatchedTokensAsErrors = false,
            Handler = null
        };

        loginCommand.SetHandler(
            async () => { await engine.Login(); });

        #endregion

        #region Logout Command

        var logoutCommand = new Command("logout", "Logout.");
        logoutCommand.SetHandler(async () => { await engine.Logout(); });

        #endregion

        #region Chat Command

        var chatCommand = new Command("chat", "Show all chats.");

        chatCommand.SetHandler(
            async () => { await engine.Chats(); });

        #endregion

        #region Message Command

        var maxMessageOption = new Option<int>(
            name: "--max-messages",
            description: "The maximum number of messages to download from chat conversation.",
            getDefaultValue: () => 25);
        maxMessageOption.AddAlias("-n");

        var downloadOption = new Option<bool>(
            name: "--download",
            description: "Download flag.");
        downloadOption.AddAlias("-d");

        var chatIdOption = new Option<long>(
            name: "--chat-id",
            description: "The Id of the chat conversation.");
        chatIdOption.AddAlias("-c");

        var messageCommand = new Command("message", "Show all messages in a Chat.");
        messageCommand.AddOption(downloadOption);
        messageCommand.AddOption(chatIdOption);
        messageCommand.AddOption(maxMessageOption);

        messageCommand.SetHandler(
            async (chatId, maxMessage, download) => { await engine.ChatMessages(chatId, maxMessage, download); },
            chatIdOption, maxMessageOption, downloadOption);

        #endregion

        rootCommand.AddCommand(loginCommand);
        rootCommand.AddCommand(logoutCommand);
        rootCommand.AddCommand(chatCommand);
        rootCommand.AddCommand(messageCommand);

        #region Execute Command

        var result = -1;
        try
        {
            result = await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error {ex.Message}");
        }
        finally
        {
            Console.WriteLine("Exited");
        }

        return result;

        #endregion
    }
}