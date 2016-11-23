# savegame-sync
A utility to synchronize savegame files for GOG games with OneDrive.

The goal of this project is to make a lightweight application that allows users to easily synchronize their savegame files for GOG games. Right now, GOG does not provide a cloud save utility like Steam Cloud, so users have to manually copy savegame files between computers. This application will maintain cloud saves in a user's OneDrive account and provide a simple interface for synchronizing those save files with local files.

## Plan

1. First, I'll make a simple utility that allows the user to copy save files for a particular game (Medal of Honor: Allied Assault) to and from the user's OneDrive account.
2. Then, I'll work on making the game-specific functionality extensible, so that the program can be modified to support different games with a minimum of additional code.
3. Finally, I'll think about how to make the synchronization process automatic while still protecting users' data at all costs.
