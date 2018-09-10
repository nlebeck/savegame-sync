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

## Google APIs client setup

This app needs to have the client secret of a Google APIs app in order to
upload and download data from its Google Drive app folder on the user's
account. I've created a SavegameSync Google APIs project, but from what I can
tell, there's no good way to package a client secret with a client application
that will be distributed directly to users -- there's always the risk that
malicious users could extract the client secret from the binary and use it to
let arbitrary apps spoof the SavegameSync app. As a result, I think the best
way to run this app right now is to create your own Google APIs project, create
a client secret for it, and use that client secret to run SavegameSync. Here's
the procedure for doing that:

1. Go to the [Google APIs Console](https://console.developers.google.com/apis/)
and create a new project.

2. Go to the Credentials tab and click "Create credentials." Select the "OAuth
client ID" option.

3. In the "OAuth 2.0 client IDs" list, click on the entry that you created,
then click the "Download JSON" button to download a JSON file containing the
client secret.

4. Copy that JSON file into the SavegameSync project folder
(`SavegameSync\SavegameSync` from the repo root) and rename it
`google-drive-client-secret.json`.

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

There are some limitations that I don't have plans to address at this time.

1. Currently, this app uses the latest LastWriteTime of any file included in a
save to determine the timestamp of the overall save. This method of calculating
the timestamp works in most cases, but if the user only deletes some files and
does not add or modify any files, the timestamp will be incorrect.

2. When adding a game to your local game list, the app has no way of knowing if
the install directory that you selected actually belongs to an installation of
the game that you chose. One potential solution for verifying that the install
directory matches the game would be to add some information to each game's
SaveSpec about its expected install directory contents and check against that
information when adding the game to the local game list.

3. The app is currently unable to detect duplicate saves. Each save is treated
as distinct, even if it is an exact copy of another save. We could detect
duplicate saves by taking a hash of the zipped save directory or something, but
then we would need to decide what policy to follow when a duplicate is
detected. (Do we prevent the user from uploading a duplicate save? Or just
display a warning message?)

## OneDrive bugs

I had originally planned to use OneDrive to sync data, but I ran into some bugs
trying to create files in OneDrive from either a UWP or WPF app. This issue
describes the problems I encountered:
https://github.com/OneDrive/onedrive-sdk-csharp/issues/204.
