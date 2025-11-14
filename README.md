# ClipFunc

### **_No Support! Provided "AS IS"_**

A small tool to call a discord webhook when a clip has been created for a linked twitch channel.

![License][license_shield] ![built using dotnet][dotnet_shield]

![discord][discord_shield] ![twitch][twitch_shield] ![coffee][coffee_shield]

## Support

This project is provided **_AS IS_**. Do **_NOT_** expect support.

## Configuration

see [.env.example](.env.example)

## Deployment

### docker run

````bash
docker build . -t clipfunc:latest
````

````bash
docker volume create clipfunc-db
````

````bash
docker run --rm \
--name clipfunc \
-v clipfunc-db:/app/db \
-e TWITCH_CLIENT_ID=your-twitch-client-id \
-e TWITCH_CLIENT_SECRET=your-twitch-client-secret \
-e TWITCH_BROADCASTER_ID=26610234 \
-e DISCORD_WEBHOOK_PROFILE_NAME=ClipFunc \
-e DISCORD_WEBHOOK_URL=https://discord.com/api/webhooks/122333444455555666666/ABBCCCDDDDEEEEEFFFFFF \
-e PREVENT_WEBHOOK_ON_FIRST_LOAD=true \
-e SECONDS_BETWEEN_RUNS=10 \
-e DATABASE_PATH=/app/db/clipfunc.db \
-e LOG_LEVEL=Information \
clipfunc:latest
````

### docker compose

see [example compose file](compose.example.yaml)

## Links

Originally built for [MalfuncGaming](https://twitch.tv/malfuncgaming)

[license_shield]: https://img.shields.io/github/license/MitoG/ClipFunc?style=for-the-badge

[coffee_shield]: https://img.shields.io/badge/built_with-coffee-brown?style=for-the-badge

[dotnet_shield]: https://img.shields.io/badge/built_with-.NET-512BD4?style=for-the-badge

[discord_shield]: https://img.shields.io/badge/built_for-discord-5865F2?style=for-the-badge

[twitch_shield]: https://img.shields.io/badge/built_for-twitch-9146FF?style=for-the-badge
