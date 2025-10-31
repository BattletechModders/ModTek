#!/usr/bin/env bash
# Doorstop run script copied from BepInEx and optimized for ModTek
# macOS: bash 3.2
# Steam on Linux: bash 4+
set -eux

executable_name="BattleTech"

# Use POSIX-compatible way to get the directory of this script
a="/$0"; a=${a%/*}; a=${a#/}; a=${a:-.}; BASEDIR=$(cd "$a" || exit; pwd -P)

echo "cmdline:"
echo "$@"

if (( $# > 0 ))
then # only do this complicated logic if there are arguments
  argc=$#
  i=0
  inserted_run=0

  while (( i < argc ))
  do  # continue rotating until all args processed
    arg="$1"
    shift
    (( ++i ))

    if [[ -x "$arg" && "$(basename "$arg")" == "$executable_name" ]]
    then  # found executable
      if (( i == 1 ))
      then # just set executable_path and break, no need to delay startup
        executable_path="$arg"
        break
      else # need to delay startup, so insert run.sh before executable
        inserted_run=1
        set -- "$@" "${BASEDIR}/run.sh" "$arg"
      fi
    else
      # rotate arguments
      set -- "$@" "$arg"
    fi
  done

  if (( inserted_run ))
  then
    echo "new cmdline:"
    echo "$@"
    exec "$@"
  fi
fi

# Setup logging to file and terminal
#logfile="${BASEDIR}/run.log"
#exec > >(tee -a "$logfile") 2>&1

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

    #Fix for Mono error On Ubuntu 22.04 LTS and probably others 'System.ConsoleDriver' threw an exception. ---> System.Exception: Magic number is wrong: 542
    #Fix discussion at https://stackoverflow.com/questions/49242075/mono-bug-magic-number-is-wrong-542
    export TERM="xterm"

    export LD_LIBRARY_PATH="${BASEDIR}${LD_LIBRARY_PATH:+:${LD_LIBRARY_PATH}}"
    export LD_PRELOAD="libdoorstop.so${LD_PRELOAD:+:${LD_PRELOAD}}"

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

    doorstop_insert_libraries="${BASEDIR}/libdoorstop.dylib${DYLD_INSERT_LIBRARIES:+:${DYLD_INSERT_LIBRARIES}}"
    if [ "$(uname -m)" = "x86_64" ]
    then
      set -- env \
        DYLD_INSERT_LIBRARIES="${doorstop_insert_libraries}" \
        "$@"
    else
      # on Apple Silicon, run the game in x86_64 mode and pass in all required env variables
      set -- arch -x86_64 \
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

exec "$@"
