# Atlas OS

Atlas OS is an AI operating system designed to understand, remember, reason, and automate tasks through voice, vision, and intelligent plugins.

## Goals

- Natural conversations
- Voice interaction
- Long-term memory
- Computer automation
- Vision
- Plugin system
- Local and cloud AI support

## Current Version

v0.1 (Foundation)

## Running it (Windows)

1. Install the .NET 10 SDK and [Ollama](https://ollama.com), then pull a model: `ollama pull gemma3:4b`.
2. `dotnet build AtlasOS.slnx`
3. Desktop app: `dotnet run --project src/Atlas.UI`
   Console: `dotnet run --project src/Atlas.Console` (type `voice` for hands-free mode).

The app has three pages: **Chat** (streaming replies, history, voice), **Memory** (what Atlas remembers)
and **Settings** (Ollama address and model, voice options, where files are stored).

## Using Atlas from your phone

Atlas keeps running on your PC; your phone is a remote screen for it.

1. On the PC (Ollama running), start the phone server:
   `dotnet run --project src/Atlas.Server`
2. It prints one or more links like `http://192.168.1.23:5050/#t=...`. Open one on your phone
   (same Wi-Fi). The link contains the password, so treat it like one. The page remembers you after that.
3. Windows may ask to allow Atlas through the firewall the first time: allow it on **Private** networks only.

What you get: chat (replies stream in), recent chats, and the memory list. Use the speaker button to have
replies read aloud. For speech input use the microphone on your phone keyboard: browsers only give web pages
the microphone over HTTPS, and a plain Wi-Fi address is not HTTPS.

Security: every request needs the password (stored in `server.json`, make a new one with
`dotnet run --project src/Atlas.Server -- --new-password`). Only devices on private networks (home Wi-Fi,
Tailscale) are accepted, repeated wrong passwords lock that device out for 10 minutes, and nothing is
exposed to the internet unless you turn `AllowPublicInternet` on in `server.json`. Do not do that.

The PC must be on and the server running. Chats from the phone are saved in the same history as the desktop app.

## Voice models

Voice needs the Sherpa-ONNX wake word, speech-to-text and text-to-speech models.
Atlas looks for them under `Models\Speech` in the folder named by the `ATLAS_HOME` environment variable,
a parent of the app folder, or `%USERPROFILE%\AtlasOS`. Settings shows what is missing.

## Data

| What | Where | Override |
|---|---|---|
| Memories | `%LOCALAPPDATA%\AtlasOS\atlas.db` | `ATLAS_DB` |
| Settings | `%LOCALAPPDATA%\AtlasOS\settings.json` | `ATLAS_SETTINGS` |
| Conversations | `%LOCALAPPDATA%\AtlasOS\conversations` (old `Data\*.json` is copied over once) | `ATLAS_DATA` |
| Phone server | `%LOCALAPPDATA%\AtlasOS\server.json` (port, password) | `ATLAS_SERVER_SETTINGS` |

## Tests

`dotnet test tests/Atlas.Tests`
