# savegame-sync
A utility to synchronize savegame files for GOG games using Google Drive.

## Goal

The goal of this project is to make a lightweight application that allows users
to easily synchronize their savegame files for GOG games. Right now, GOG does
not provide a cloud save utility like Steam Cloud, so users have to manually
copy savegame files between computers. This application will maintain cloud
saves in a user's Google Drive account and provide a simple interface for
synchronizing those save files with local files.

## Plan

1. First, I'll make a simple utility that allows the user to copy save files
for a particular game (Medal of Honor: Allied Assault) to and from the user's
Google Drive account.
2. Then, I'll work on making the game-specific functionality extensible, so
that the program can be modified to support different games with a minimum of
additional code.
3. Finally, I'll think about how to make the synchronization process automatic
while still protecting users' data at all costs.

## Repository organization
SavegameSync is a Visual Studio solution directory. The subdirectory
SavegameSync holds the version of the app that I'm currently working on,
while the SavegameSyncUWP subdirectory holds the UWP version of the app that I
started on but abandoned due to difficulties fixing dependency issues between
UWP and the Google Drive NuGet package.

## OneDrive bugs

I had originally planned to use OneDrive to sync data, but I ran into some bugs
trying to create files in OneDrive from either the UWP or WPF version of my
app. This issue describes the problems I encountered:
https://github.com/OneDrive/onedrive-sdk-csharp/issues/204.
