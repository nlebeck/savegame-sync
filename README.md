# SavegameSync

SavegameSync is an app for synchronizing and storing PC game save files in the
cloud. The app maintains cloud saves in the user's Google Drive account and
provides a simple interface for uploading and downloading saves.

SavegameSync provides somewhat similar functionality to Steam Cloud or GOG's
cloud save feature, with a few key differences:

1. Unlike Steam Cloud and GOG's automatic synchronization, SavegameSync
provides a more granular, manual interface for managing save files.
SavegameSync only uploads and downloads files when instructed to by the user,
supports storing multiple cloud saves for each game, and lets the user choose
which cloud save to download.

2. SavegameSync can be extended to support any game by defining a custom
XML "SaveSpec" for that game.

Originally, I intended this app to be used for GOG games, but since I started
working on it, GOG implemented its own cloud save feature. There are still some
GOG games that don't support cloud saves, though (and maybe some Steam games
too?), so I'm hoping it will still be a useful app. In any case, it has been
fun to make.

SavegameSync is not production-ready! If you want to use this app to
synchronize save data that you care about, please make other backups of that
data occasionally. I think data loss is unlikely, and the app has features for
recovering from data corruption, but I don't know enough about the Google Drive
API's failure modes to make any confident claims of what's possible or not
possible when using this app.

## Instructions

SavegameSync's save file synchronization functionality is split across a few
different windows. Here's how to use the different windows to manage your
saves:

### Main window

When the app first opens, you'll see the main window. The main window is where
you do most of your uploading and downloading. The main window shows a list of
"local games" (games that you have installed on the running computer) on the
left, and if you select a game, it will show the list of cloud saves for that
game on the right.

To add a game, click the "add game" button. You'll need to first select the
game's install directory and then choose the game's name from a list (the list
shows the names of all games with defined SaveSpecs).

Once you've added a game, you can upload saves to the cloud, download saves
from the cloud, and delete saves in the cloud for that game using the
appropriate buttons. Each cloud save shows the timestamp at which its files
were last modified, and the app also shows you the timestamp of your local save
files for the game.

### Cloud game list window

The cloud game list window, accessed by clicking the "view games in cloud
storage" button, lets you view any game for which you have cloud saves. You can
download and delete individual saves or delete all saves for the game from the
cloud. This window lets you manage your cloud data even if you don't have
copies of all your games installed on your local computer.

### Repair files window

This window lets you deal with error states that can arise due to Google Drive
errors, connection issues, or other unexpected circumstances. In general, there
are two types of error cases: a save file can end up missing from the master
"savegame list" but still have the actual file present in Google Drive, or the
actual file might disappear while the save's entry remains in the savegame
list. The "orphaned saves" list on the left side of the window shows saves that
wound up in the first state, while the list on the right side of the window
shows saves in the second state. If a save is orphaned, you can still recover
the actual file using the "download save" button, but for saves whose files are
missing but that have entries present in the savegame list, the only thing to
be done is to delete those entries from the savegame list.

This window also has options to download or delete all of the files stored in
SavegameSync's Google Drive app folder. These options are mostly provided for
convenience, but they might come in handy for troubleshooting problems or for
recovering your save data in the case of catastrophic file corruption.

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

4. Rename the downloaded JSON file `google-drive-client-secret.json`.

5. If you're running a binary release of SavegameSync, copy the JSON file into
the directory holding `SavegameSync.exe`. If you're building SavegameSync from
source, copy the JSON file into the SavegameSync project folder
(`SavegameSync\SavegameSync` from the repo root).

## Future work

These are some things that could be done to make this app more robust and
polished:

1. Right now, this app's error handling is incomplete. The classes for the
three different windows have top-level exception-catching code that displays an
error dialogue box for most of their operations (with the exception of
initialization/login), but that exception-catching code only catches custom
SavegameSyncExceptions defined by me, since I want to make sure I understand
what's failing if I'm going to let the app continue to run after an error. I've
defined SavegameSyncExceptions for some error cases related to the states of
local files, but I haven't defined SavegameSyncExceptions for all of the
possible failure conditions yet. In particular, I haven't defined
SavegameSyncExceptions for failures caused by either Google Drive connection
errors or unexpected states of the files in Google Drive. As a result, those
failures will generally cause the app to crash with an unhandled exception.

2. There should be some documentation in this README about the format of the
`SaveSpecRepository.xml` file used to populate the SaveSpecRepository.

3. Right now, all timestamps show up in UTC. This is probably confusing for
users, so it would be good to have the timestamps show up in the user's current
time zone (or let the user set the time zone).

4. Currently, when you use the app to manually download files from the cloud
instead of synchronizing with a specific locally installed game (e.g., from the
cloud game list window or the repair files window), the app just dumps the
downloaded files in the directory from which the app was launched. It would be
good to give the user a file picker and let the user choose where to put
downloaded files.

5. I've mostly just tested this app with two real games (Medal of Honor: Allied
Assault and Diablo II) and various forms of hand-crafted dummy data. It would
be good to test this app with a variety of different games, to make sure the
SaveSpec format is flexible enough to handle different save file layouts.

6. The dialog for adding a local game should have some text with instructions
and information about the choices that it is asking the user to make.

7. Some games store save files in a separate directory from the main game
installation (e.g., a subdirectory of `C:\Users\<username>\Saved Games`). That
directory is the one that we want users to select when adding a local game.
There should be a field in the SaveSpec that provides information about which
directory should be selected as the game's "install directory," and there
should be instructions when adding a local game that convey that information to
the user.

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

4. Currently, the app assumes users will only use it on one computer at a time.
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

## OneDrive bugs

I had originally planned to use OneDrive to sync data, but I ran into some bugs
trying to create files in OneDrive from either a UWP or WPF app. This issue
describes the problems I encountered:
https://github.com/OneDrive/onedrive-sdk-csharp/issues/204.
