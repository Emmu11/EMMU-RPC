# EMMU RPC

EMMU RPC is a small native Windows launcher intended for Discord Desktop's manual **Registered Games > Add it!** flow.

## Download

Download the ready-to-run Windows executable from the [latest release](https://github.com/Emmu11/EMMU-RPC/releases/latest).

> EMMU RPC is currently unsigned, so Windows SmartScreen may show an unknown-publisher warning. Review the source and build it yourself if preferred.

## Use

1. Open `EMMU RPC.exe`.
2. Enter the game/application name you want (for example, `GTA V`).
3. Select **Launch**.
4. In Discord Desktop, open **User Settings > Registered Games > Add it!** and select the running generated process.
5. The generated window is independent of EMMU RPC. You may close EMMU RPC and keep the generated application open.
6. Close the generated application when finished. Its temporary executable and directory are removed automatically shortly afterward.

Discord controls how manually registered processes are classified and displayed. This tool improves compatibility by matching the temporary executable filename, Windows version metadata, process, and window title to the requested name; it does not create an official game entry or Discord Rich Presence integration.

## Naming and cleanup

- The visible application/window name preserves the text entered by the user.
- Characters Windows forbids in filenames (`< > : " / \\ | ? *`), control characters, trailing dots/spaces, and reserved device names (`CON`, `NUL`, `COM1`, and similar) are safely normalized only for the temporary executable filename.
- Each launch uses a unique directory under `%TEMP%\EMMU-RPC`.
- Normal window exit schedules deletion of that directory. EMMU RPC also removes abandoned directories older than 24 hours on startup (for example after a force-kill or power loss).

## Build from source

Run `build.ps1` in Windows PowerShell. It uses the .NET Framework C# compiler included with Windows when available. The editable project files target .NET Framework 4.7.2 and can also be opened in Visual Studio.

The output is `build\EMMU RPC.exe`. The launcher embeds `GeneratedApp.exe`, so the final launcher is a single distributable file.

Run `test.ps1` to verify filename safety, preserved display/version names, launcher/child independence, normal window close, and temporary-file cleanup.
