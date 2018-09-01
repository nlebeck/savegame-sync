# savegame-sync

A utility to synchronize savegame files for PC games using Google Drive.

## Goal

The goal of this project is to make a lightweight application that allows users
to easily synchronize their savegame files for PC games. This app maintains
cloud saves in a user's Google Drive account and provides a simple interface
for uploading and downloading saves.

Originally, I intended this app to be used for GOG games, but since I started
working on it, GOG implemented its own cloud save feature. There are still some
GOG games that don't support cloud saves, though (and maybe some Steam games
too?), so I'm hoping it will still be a useful app. In any case, it has been
fun to make.

## Future work

This project is pretty close to being complete, but there are a few more things
I want to take care of before I declare it finished.

1. Error handling: right now, this app is not very robust to errors, whether
they're caused by Google Drive request failures, unexpected states in the local
filesystem, or corrupted metadata files. Broadly speaking, I think the risk of
losing data stored in the cloud is very low -- even if the master savegame-list
file gets corrupted, the Orphaned Save window can be used to recover the files.
However, many errors will currently result in unhandled exceptions that crash
the app, so I'd like to have a more pleasant user experience that avoids making
the user restart the app and tells the user 1) why an error happened, and 2)
how the error affected the user's data (if at all).

2. Currently, the app assumes users will only use it on one computer at a time.
Bad things could happen if a user opens two instances of the app and
simultaneously uploads and/or deletes cloud saves in both instances, since the
savegame-list is not read and written transactionally. I think the worst that
could happen is that a newly uploaded save could end up orphaned, or a save
that was deleted might end up remaining in the savegame-list. The solution
would be to add some sort of lock that is set when starting an operation and
released when finishing the operation. Of course, then there are problems that
could arise if one instance of the app grabs the lock but then fails to release
the lock due to a crash/premature exit/Google Drive connection failure, so
there would need to be some sort of timeout/abort mechanism.

## Limitations

There are some limitations that I don't have any good ideas of how to solve.

1. Currently, this app uses the latest LastWriteTime of any file included in a
save to determine the timestamp of the overall save. This method of calculating
the timestamp works in most cases, but if the user only deletes some files and
does not add or modify any files, the timestamp will be incorrect.

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
