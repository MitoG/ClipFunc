# ClipFunc

### **_No Support! Provided "as is"_**

This is a small tool to call a discord webhook when a clip has been created for a linked twitch channel.

## Usage

1. Pull Repository
2. Run Project

Can also be run with docker or with docker compose from git root:

````bash
docker compose up
````

## Configuration

### TwitchCredentials

| Key          | Usage                               |
|--------------|-------------------------------------|
| ClientId     | Client-ID for Twitch api access     |
| ClientSecret | Client-Secret for Twitch api access |

### WatchedChannels

This is an array which needs to have at least 1 item

| Key                       | Usage                                                    |
|---------------------------|----------------------------------------------------------|
| BroadcasterId             | Twitch User ID of the channel which should be watched    |
| DiscordWebhookProfileName | The Username used for the Discord webhook (only viusual) |
| DiscordWebhookUrl         | The URL of the Discord webhook                           |

### ConnectionStrings

| Key      | Usage                                     |
|----------|-------------------------------------------|
| ClipFunc | Connection string for the SQLite database |