# Tikoi

This is a very simple Telegram CLI Application written in C#. You need to be a registered user of Telegram to use this application.

## API Key Registration

You need to update `tikoi/Program.cs` with the `ApiHash` & `ApiId` from [my.telegram.org/apps](https://my.telegram.org/apps).

## Required Environmennt Variables

You may substitute your own values and deploy the following in your `$HOME/.zshrc` or `$HOME/.bashrc`.

```bash
export TIKOI_API_ID="9999999"
export TIKOI_API_HASH="abcdefghjijklmnopqrstuvwxyz12345"
export TIKOI_PHONE_NUMBER="+999999999999"
export TIKOI_PASSWORD="mypassword"
export TIKOI_HOME="/opt/tikoi" # This is where tdlib stuff are stored
```

## Commands

```
Description:
  This is a very simple Telegram CLI application.

Usage:
  tikoi [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  login    Login using phone number and an authorization code sent to the app.
  logout   Logout.
  chat     Show all chats.
  message  Show all messages in a Chat.

```

## Login

```
Description:
  Login using phone number and an authorization code sent to the app

Usage:
  tikoi login [options]

Options:
  -?, -h, --help                   Show help and usage information
```

## Message

```
Description:
  Show all messages in a Chat.

Usage:
  tikoi message [options]

Options:
  -d, --download                     Download flag.
  -c, --chat-id <chat-id>            The Id of the chat conversation.
  -n, --max-messages <max-messages>  The maximum number of messages to download from chat conversation. [default: 25]
  -?, -h, --help                     Show help and usage information
```

## Chat

```
Description:
  Show all chats.

Usage:
  tikoi chat [options]

Options:
  -?, -h, --help  Show help and usage information
```

## References

- [Telegram TDLIB](https://core.tlgr.org/tdlib/docs/)
- [Building TDLIB](https://tdlib.github.io/td/build.html)
