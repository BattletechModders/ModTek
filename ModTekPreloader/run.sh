#!/bin/sh
# Doorstop start script (heavily modified and cleaned up for ModTek use case)

export DOORSTOP_MONO_DEBUG_ENABLED="0"
export DOORSTOP_MONO_DEBUG_ADDRESS="127.0.0.1:55555"
export DOORSTOP_MONO_DEBUG_SUSPEND="0"

################################################################################
# Everything past this point is the actual script

# Special case: program is launched via Steam
# In that case rerun the script via their bootstrapper to ensure Steam overlay works
if [ "$2" = "SteamLaunch" ]; then
    script_path="$0"
    # code found here: https://stackoverflow.com/questions/63864755/remove-last-argument-in-shell-script-posix/69952637#69952637
    i=0
    while [ $((i+=1)) -lt $# ]; do
        set -- "$@" "$1"
        shift
    done # 1 2 3 -> 3 1 2
    game_path="$1" # last argument
    shift # $@ is now without last argument
    "$@" "$script_path" "$game_path"
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
        
        #Fix for Mono error On Ubuntu 22.04 LTS and probably others 'System.ConsoleDriver' threw an exception. ---> System.Exception: Magic number is wrong: 542
        #Fix discussion at https://stackoverflow.com/questions/49242075/mono-bug-magic-number-is-wrong-542
        #Work around used as it is a bug that is patched out in newer versions of mono.
        export TERM=xterm
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
            export DYLD_INSERT_LIBRARIES="${BASEDIR}/libdoorstop.dylib"
        else
            export DYLD_INSERT_LIBRARIES="${BASEDIR}/libdoorstop.dylib:${DYLD_INSERT_LIBRARIES}"
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
