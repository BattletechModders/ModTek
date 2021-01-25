# Developing ModTek

This page lists some common knowledge useful if you're extending or improving ModTek. 


# Build ModTek

After checking out the project, you must update a configuration file with the full path to your BattleTech Game directory. Copy the file from `CHANGEME.Directory.Build.props` to `Directory.Build.props`. Open `Directory.Build.props` in the editor of your choice, and replace `CHANGEME_TO_FULL_PATH_TO_BTG_DIR` with the full path to your BattleTech Game (BTG) directory (one example - `E:\steam\SteamApps\common\BATTLETECH`). Close and save. `Directory.Build.props` is excluded in `.gitignore` so you changes will not affect other developers, only you.

:information_source: Linux users should note that `Directory.Build.props` is case-sensitive. If you find the project won't compile for you, make sure the case is correct.

Once you can updated the configuration, open the VS solution and restore NuGet dependencies. You should be able to build ModTek once these steps are complete.

# Releasing ModTek

