#!/bin/sh
set -eux
# Doorstop run script copied from BepInEx and optimized for ModTek

executable_name="BattleTech"
steam_launch_wrapper_name="steam-launch-wrapper" # TODO how does MacOS work?

# Use POSIX-compatible way to get the directory of this script
a="/$0"; a=${a%/*}; a=${a#/}; a=${a:-.}; BASEDIR=$(cd "$a" || exit; pwd -P)

# If launched via Steam, makes sure to delay preloading/lib insertion to just before calling BattleTech,
# in order to avoid interfering with steam wrappers.
# > steam -> run (this) -> steam wrappers -> run (continuation) -> BattleTech
# Steam debugging
# ./run.sh %command% > ~/steam_command_output.txt 2>&1
# POSIX shell can't find and insert in the middle of the argument list
if [ -n "${1:-}" ] && [ "$1" != "${1%"/${steam_launch_wrapper_name}"}" ]
then
  steam_first_wrapper="$1"
  set -- "$@" "$1" ; shift # rotates the first argument to the back
  # continue to rotate until the steam wrapper is again the first argument
  while [ "$1" != "$steam_first_wrapper" ]
  do
    if [ "$1" != "${1%"/${executable_name}"}" ] # insert this script to before BattleTech gets rotated
    then
      set -- "$@" "${BASEDIR}/run.sh"
    fi
    set -- "$@" "$1" ; shift # rotates the first argument to the back
  done
  exec "$@"
fi

# get doorstop settings for linux/mac from the ini (why does doorstop not do this for us?)
doorstop_config() { grep "^${1}=" "${BASEDIR}/doorstop_config.ini" | cut -d= -f2- ; }
doorstop_convert_bool() { sed s@true@1@ | sed s@false@0@ ; }
doorstop_convert_path() { tr '\\' '/' | tr ';' ':' ; }
export DOORSTOP_MONO_DEBUG_ENABLED="$(doorstop_config debug_enabled | doorstop_convert_bool)"
export DOORSTOP_MONO_DEBUG_ADDRESS="$(doorstop_config debug_address)"
export DOORSTOP_MONO_DEBUG_SUSPEND="$(doorstop_config debug_suspend | doorstop_convert_bool)"
export DOORSTOP_ENABLED="$(doorstop_config enabled | doorstop_convert_bool)"
export DOORSTOP_TARGET_ASSEMBLY="$(doorstop_config target_assembly | doorstop_convert_path)"
export DOORSTOP_MONO_DLL_SEARCH_PATH_OVERRIDE="$(doorstop_config dll_search_path_override | doorstop_convert_path)"

# check if the first parameter is the executable, e.g. as forwarded through Steam
if [ -n "${1:-}" ] && [ -x "$1" ]
then
  executable_path="$1"
  shift
fi

os_type="$(uname -s)"
case ${os_type} in
  Linux*)
    # guess executable path if launched without specifying an executable
    if [ -z "${executable_path:-}" ]
    then
      executable_path="${BASEDIR}/${executable_name}"
      cd "${BASEDIR}"
    fi
    set -- "$executable_path" "$@"

    # TERM=xterm
    #Fix for Mono error On Ubuntu 22.04 LTS and probably others 'System.ConsoleDriver' threw an exception. ---> System.Exception: Magic number is wrong: 542
    #Fix discussion at https://stackoverflow.com/questions/49242075/mono-bug-magic-number-is-wrong-542

    set -- env \
      TERM="xterm" \
      LD_LIBRARY_PATH="${BASEDIR}" \
      LD_PRELOAD="libdoorstop.so${LD_PRELOAD:+:${LD_PRELOAD}}" \
      "$@"

    # disable ASLR which causes issues with runtime patching on some systems
    if [ "${MODTEK_DISABLE_ASLR:-}" = "true" ]
    then
      set -- "setarch" "-R" "$@"
    fi
  ;;
  Darwin*)
    # why is this suddenly necessary? see https://github.com/NeighTools/UnityDoorstop/issues/67
    # we have: BASEDIR="/..../BATTLETECH/BattleTech.app/Contents/Resources"
    # we want: additional_path="BattleTech.app/Contents/Resources"
    contents_path="$(dirname "$BASEDIR")"
    app_dir="$(basename "$(dirname "$contents_path")")"
    additional_path="$app_dir/Contents/Resources"
    DOORSTOP_TARGET_ASSEMBLY="$additional_path/$DOORSTOP_TARGET_ASSEMBLY"
    DOORSTOP_MONO_DLL_SEARCH_PATH_OVERRIDE="$additional_path/$DOORSTOP_MONO_DLL_SEARCH_PATH_OVERRIDE"

    # guess executable path if launched without specifying an executable
    if [ -z "${executable_path:-}" ]
    then
      # BASEDIR should be the Resources directory
      executable_path="$contents_path/MacOS/${executable_name}"
    fi
    set -- "$executable_path" "$@"

    doorstop_library_path="${BASEDIR}${DYLD_LIBRARY_PATH:+:${DYLD_LIBRARY_PATH}}"
    doorstop_insert_libraries="libdoorstop.dylib${DYLD_INSERT_LIBRARIES:+:${DYLD_INSERT_LIBRARIES}}"
    if [ "$(uname -m)" = "x86_64" ]
    then
      set -- env \
        DYLD_LIBRARY_PATH="${doorstop_library_path}" \
        DYLD_INSERT_LIBRARIES="${doorstop_insert_libraries}" \
        "$@"
    else
      # on Apple Silicon, run the game in x86_64 mode and pass in all required env variables
      set -- arch -x86_64 \
        -e DYLD_LIBRARY_PATH="${doorstop_library_path}" \
        -e DYLD_INSERT_LIBRARIES="${doorstop_insert_libraries}" \
        "$@"
    fi

    # fix mods wanting BattleTech_Data
    (
      cd "$BASEDIR" || exit 99
      ln -fs Data BattleTech_Data
    )
  ;;
  *)
    # alright who is running games on freebsd
    echo "Unknown operating system (${os_type})"
    echo "Make an issue at https://github.com/NeighTools/UnityDoorstop"
    exit 1
  ;;
esac

echo "args3:" "$@"
exec "$@"
