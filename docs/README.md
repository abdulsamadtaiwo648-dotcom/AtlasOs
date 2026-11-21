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

## Voice models

Voice needs the Sherpa-ONNX wake word, speech-to-text and text-to-speech models.
Atlas looks for them under `Models\Speech` in the folder named by the `ATLAS_HOME` environment variable,
a parent of the app folder, or `%USERPROFILE%\AtlasOS`. Settings shows what is missing.

## Data

| What | Where | Override |
|---|---|---|
| Memories | `%LOCALAPPDATA%\AtlasOS\atlas.db` | `ATLAS_DB` |
| Settings | `%LOCALAPPDATA%\AtlasOS\settings.json` | `ATLAS_SETTINGS` |
| Conversations | `Data\*.json` next to the app | |

## Tests

`dotnet test tests/Atlas.Tests`
