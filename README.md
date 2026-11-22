# League Client Tool

A native Windows WPF desktop companion for the League of Legends client. The application uses the local League Client API (LCU) and Riot's public Data Dragon assets.

## Features

### Dashboard and connection
- Detects the running League client by locating its lockfile.
- Authenticates against the local LCU endpoint and checks the API connection.
- Refreshes connection status automatically every five seconds.
- Displays the connected player's Riot ID, summoner level and region when available.

### Profile
- Change the profile icon, including the classic icon gallery (IDs 50–78) and manual icon IDs.
- Change the profile status message.
- Change the Riot ID game name and tagline.
- Set or clear the visible rank shown in League chat and the friends list.
- Apply the last-rank profile banner.
- Clear equipped challenge tokens or copy the first equipped token into all three slots.

### Customisation
- Browse champion profile backgrounds using Data Dragon.
- Browse a champion's base skin and available skins.
- Preview skin artwork before applying the background.

### Friends
- Accept all incoming friend requests.
- Delete all pending friend requests.
- Browse friend folders and delete friends in a selected folder.

### Client tools
- Send custom GET, POST, PUT, PATCH and DELETE requests to local LCU API paths.
- Inspect the HTTP status and response body.
- Restart the League client UX and renderer without intentionally terminating the game process.

Destructive friend, challenge-token and restart actions ask for confirmation.

## Requirements

- Windows 10 or Windows 11
- .NET 10 SDK
- League of Legends client for live LCU functionality

## Build and run

From the repository root:

```powershell
dotnet restore LeagueClientTool.slnx
dotnet build LeagueClientTool.slnx --configuration Release
dotnet run --project LeagueClientTool.App
```

Run the automated Core service and parser tests:

```powershell
dotnet test LeagueClientTool.slnx --configuration Release
```

The WPF application targets `net10.0-windows`. The Core project remains independent of WPF so its API, parsing and feature services can be tested separately.

## Security and privacy

- The LCU connection is made to the loopback interface using credentials read from the League client lockfile.
- The local self-signed certificate exception is restricted to loopback requests.
- Custom LCU requests must use a local absolute-path endpoint; absolute URLs, network-path URLs and path traversal segments are rejected.
- The application does not send lockfile credentials to Data Dragon or other public services.
- Data Dragon is used only to retrieve public champion and skin artwork.

## Project structure

- `LeagueClientTool.App` — WPF UI, navigation, MVVM view models and dependency injection.
- `LeagueClientTool.Core` — lockfile detection, authenticated LCU client, profile, challenge, friend and Data Dragon services.
- `LeagueClientTool.Tests` — xUnit tests for Core parsing, API requests and feature services.
