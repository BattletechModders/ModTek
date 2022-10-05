#!/bin/sh
# Doorstop start script (heavily modified and cleaned up for ModTek use case)

export DOORSTOP_MONO_DEBUG_ENABLED="0"
export DOORSTOP_MONO_DEBUG_ADDRESS="127.0.0.1:10000"
export DOORSTOP_MONO_DEBUG_SUSPEND="0"

################################################################################
# Everything past this point is the actual script

# Special case: program is launched via Steam
# In that case rerun the script via their bootstrapper to ensure Steam overlay works
if [ "$2" = "SteamLaunch" ]; then
    steam="$1 $2 $3 $4 $0 $5"
    shift 5
    $steam "$@"
    exit
fi

# Use POSIX-compatible way to get the directory of the executable
a="/$0"; a=${a%/*}; a=${a#/}; a=${a:-.}; BASEDIR=$(cd "$a" || exit; pwd -P)

if [ -x "$1" ]
then
    executable_path="$1"
    shift
fi

os_type="$(uname -s)"
case ${os_type} in
    Linux*)
        if [ -z "$executable_path" ]
        then
            executable_path="${BASEDIR}/BattleTech"
        fi
        export LD_LIBRARY_PATH="${BASEDIR}/:${LD_LIBRARY_PATH}"
        if [ -z "$LD_PRELOAD" ]; then
            export LD_PRELOAD="libdoorstop.so"
        else
            export LD_PRELOAD="libdoorstop.so:${LD_PRELOAD}"
        fi
    ;;
    Darwin*)
        # BASEDIR should be the Resources directory
        if [ -z "$executable_path" ]
        then
            contents_path=$(dirname "$BASEDIR")
            real_app_name=$(defaults read "${contents_path}/Info" CFBundleExecutable)
            executable_path="${contents_path}/MacOS/${real_app_name}"
        fi
        # fix mods wanting BattleTech_Data
        (
            cd "$BASEDIR"
            ln -fs Data BattleTech_Data
        )
        export DYLD_LIBRARY_PATH="${BASEDIR}/:${DYLD_LIBRARY_PATH}"
        if [ -z "$DYLD_INSERT_LIBRARIES" ]; then
            export DYLD_INSERT_LIBRARIES="libdoorstop.dylib"
        else
            export DYLD_INSERT_LIBRARIES="libdoorstop.dylib:${DYLD_INSERT_LIBRARIES}"
        fi
    ;;
    *)
        # alright who is running games on freebsd
        echo "Unknown operating system (${os_type})"
        echo "Make an issue at https://github.com/NeighTools/UnityDoorstop"
        exit 1
    ;;
esac

export DOORSTOP_ENABLED="1"
export DOORSTOP_TARGET_ASSEMBLY="${BASEDIR}/Mods/ModTek/ModTekPreloader.dll"

exec "$executable_path" "$@"
