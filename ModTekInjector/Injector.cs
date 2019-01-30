using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Ionic.Zip;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Options;
using Newtonsoft.Json;
using static System.Console;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace ModTekInjector
{
    internal static class Injector
    {
        // return codes
        private const int RC_NORMAL = 0;
        private const int RC_UNHANDLED_STATE = 1;
        private const int RC_BAD_OPTIONS = 2;
        private const int RC_MISSING_BACKUP_FILE = 3;
        private const int RC_BACKUP_FILE_INJECTED = 4;
        private const int RC_BAD_MANAGED_DIRECTORY_PROVIDED = 5;
        private const int RC_MISSING_MODTEK_ASSEMBLY = 6;
        private const int RC_REQUIRED_GAME_VERSION_MISMATCH = 7;
        private const int RC_MISSING_FACTION_FILE = 8;

        private const string MODTEK_DLL_FILE_NAME = "ModTek.dll";
        private const string INJECT_TYPE = "ModTek.Injection";
        private const string INJECT_METHOD = "LoadModTek";

        private const string GAME_DLL_FILE_NAME = "Assembly-CSharp.dll";
        private const string HOOK_TYPE = "BattleTech.Main";
        private const string HOOK_METHOD = "Start";

        private const string BACKUP_FILE_EXT = ".orig";

        private const string GAME_VERSION_TYPE = "VersionInfo";
        private const string GAME_VERSION_CONST = "CURRENT_VERSION_NUMBER";

        private const int FACTION_ENUM_STARTING_ID = 5000;
        private const FieldAttributes ENUM_ATTRIBUTES = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault;

        private static ManagedAssemblyResolver managedAssemblyResolver;

        // ReSharper disable once InconsistentNaming
        private static readonly ReceivedOptions OptionsIn = new ReceivedOptions();

        // ReSharper disable once InconsistentNaming
        private static readonly OptionSet Options = new OptionSet
        {
            {
                "d|detect",
                "Detect if the game assembly is already injected",
                v => OptionsIn.Detecting = v != null
            },
            {
                "g|gameversion",
                "Print the game version number",
                v => OptionsIn.GameVersion = v != null
            },
            {
                "h|?|help",
                "Print this useful help message",
                v => OptionsIn.Helping = v != null
            },
            {
                "i|install",
                "Inject ModTek without doing anything else",
                v => OptionsIn.Installing = v != null
            },
            {
                "manageddir=",
                "specify managed dir where BattleTech's Assembly-CSharp.dll is located",
                v => OptionsIn.ManagedDir = v
            },
            {
                "y|nokeypress",
                "Anwser prompts affirmatively",
                v => OptionsIn.RequireKeyPress = v == null
            },
            {
                "reqmismatchmsg=",
                "Print msg if required version check fails",
                v => OptionsIn.RequiredGameVersionMismatchMessage = v
            },
            {
                "requiredversion=",
                "Don't continue with /install, /update, etc. if the game version does not match given argument",
                v => OptionsIn.RequiredGameVersion = v
            },
            {
                "r|restore",
                "Restore non-injected backup game assembly",
                v => OptionsIn.Restoring = v != null
            },
            {
                "u|update",
                "Try to update to the latest ModTek injection (this is the default behaviour)",
                v => OptionsIn.Updating = v != null
            },
            {
                "v|version",
                "Print the ModTekInjector version number",
                v => OptionsIn.Versioning = v != null
            },
            {
                "factionsPath=",
                "Specify a zip file with factions to inject",
                v => OptionsIn.FactionsPath = v
            }
        };


        // ENTRY POINT
        private static int Main(string[] args)
        {
            try
            {
                try
                {
                    Options.Parse(args);
                }
                catch (OptionException e)
                {
                    SayOptionException(e);
                    return RC_BAD_OPTIONS;
                }

                if (OptionsIn.Helping)
                {
                    SayHelp(Options);
                    return RC_NORMAL;
                }

                if (OptionsIn.Versioning)
                {
                    SayVersion();
                    return RC_NORMAL;
                }

                var managedDirectory = Directory.GetCurrentDirectory();
                if (!string.IsNullOrEmpty(OptionsIn.ManagedDir))
                {
                    if (!Directory.Exists(OptionsIn.ManagedDir))
                    {
                        SayManagedDirMissingError(OptionsIn.ManagedDir);
                        return RC_BAD_MANAGED_DIRECTORY_PROVIDED;
                    }

                    managedDirectory = Path.GetFullPath(OptionsIn.ManagedDir);
                }

                // look for missing assemblies in the managed folder
                managedAssemblyResolver = new ManagedAssemblyResolver(managedDirectory);

                var gameDLLPath = Path.Combine(managedDirectory, GAME_DLL_FILE_NAME);
                var gameDLLBackupPath = Path.Combine(managedDirectory, GAME_DLL_FILE_NAME + BACKUP_FILE_EXT);
                var modTekDLLPath = Path.Combine(Directory.GetCurrentDirectory(), MODTEK_DLL_FILE_NAME);

                if (!File.Exists(gameDLLPath))
                {
                    SayGameAssemblyMissingError(OptionsIn.ManagedDir);
                    return RC_BAD_MANAGED_DIRECTORY_PROVIDED;
                }

                if (!File.Exists(modTekDLLPath))
                {
                    SayModTekAssemblyMissingError(modTekDLLPath);
                    return RC_MISSING_MODTEK_ASSEMBLY;
                }

                var factionsPath = "";
                if (!string.IsNullOrEmpty(OptionsIn.FactionsPath))
                {
                    factionsPath = OptionsIn.FactionsPath;
                    if (!Path.IsPathRooted(factionsPath))
                        factionsPath = Path.Combine(managedDirectory, factionsPath);

                    if (!File.Exists(factionsPath))
                    {
                        SayFactionsFileMissing(factionsPath);
                        return RC_MISSING_FACTION_FILE;
                    }
                }

                bool btmlInjected;
                bool modTekInjected;
                bool anyInjected;
                using (var game = ModuleDefinition.ReadModule(gameDLLPath))
                {
                    if (OptionsIn.GameVersion)
                    {
                        SayGameVersion(GetGameVersion(game));
                        return RC_NORMAL;
                    }

                    if (!string.IsNullOrEmpty(OptionsIn.RequiredGameVersion))
                    {
                        var gameVersion = GetGameVersion(game);
                        if (gameVersion != OptionsIn.RequiredGameVersion)
                        {
                            SayRequiredGameVersion(gameVersion, OptionsIn.RequiredGameVersion);
                            SayRequiredGameVersionMismatchMessage(OptionsIn.RequiredGameVersionMismatchMessage);
                            PromptForKey(OptionsIn.RequireKeyPress);
                            return RC_REQUIRED_GAME_VERSION_MISMATCH;
                        }
                    }

                    btmlInjected = IsBTMLInjected(game);
                    modTekInjected = IsModTekInjected(game);
                    anyInjected = btmlInjected || modTekInjected;
                }

                if (OptionsIn.Detecting)
                {
                    SayInjectedStatus(btmlInjected, modTekInjected);
                    return RC_NORMAL;
                }

                SayHeader();

                if (OptionsIn.Restoring)
                {
                    if (anyInjected)
                        Restore(gameDLLPath, gameDLLBackupPath);
                    else
                        SayAlreadyRestored();

                    PromptForKey(OptionsIn.RequireKeyPress);
                    return RC_NORMAL;
                }

                if (OptionsIn.Installing)
                {
                    if (!anyInjected)
                    {
                        Backup(gameDLLPath, gameDLLBackupPath);
                        Inject(gameDLLPath, modTekDLLPath, factionsPath);
                    }
                    else
                    {
                        SayAlreadyInjected(modTekInjected);
                    }

                    PromptForKey(OptionsIn.RequireKeyPress);
                    return RC_NORMAL;
                }

                if (OptionsIn.Updating)
                {
                    if (btmlInjected)
                    {
                        SayUpdatingFromBTML();
                        Restore(gameDLLPath, gameDLLBackupPath);
                        Inject(gameDLLPath, modTekDLLPath, factionsPath);
                    }
                    else if (modTekInjected)
                    {
                        if (PromptForUpdateYesNo(OptionsIn.RequireKeyPress))
                        {
                            Restore(gameDLLPath, gameDLLBackupPath);
                            Inject(gameDLLPath, modTekDLLPath, factionsPath);
                        }
                        else
                        {
                            SayUpdateCanceled();
                        }
                    }
                    else
                    {
                        Inject(gameDLLPath, modTekDLLPath, factionsPath);
                    }

                    PromptForKey(OptionsIn.RequireKeyPress);
                    return RC_NORMAL;
                }
            }
            catch (BackupFileNotFound e)
            {
                SayException(e);
                SayHowToRecoverMissingBackup(e.BackupFileName);
                return RC_MISSING_BACKUP_FILE;
            }
            catch (BackupFileInjected e)
            {
                SayException(e);
                SayHowToRecoverInjectedBackup(e.BackupFileName);
                return RC_BACKUP_FILE_INJECTED;
            }
            catch (Exception e)
            {
                SayException(e);
            }

            return RC_UNHANDLED_STATE;
        }


        // UTIL
        private static string GetGameVersion(ModuleDefinition game)
        {
            foreach (var type in game.Types)
            {
                if (type.FullName != GAME_VERSION_TYPE)
                    continue;

                var fieldInfo = type.Fields.First(x => x.IsLiteral && !x.IsInitOnly && x.Name == GAME_VERSION_CONST);
                if (fieldInfo != null)
                    return fieldInfo.Constant.ToString();
            }

            return null;
        }


        // INJECTION
        private static bool IsBTMLInjected(ModuleDefinition game)
        {
            foreach (var type in game.Types)
            {
                // check if btml is attached to any method
                foreach (var methodDefinition in type.Methods)
                {
                    if (IsMethodCalledInMethod(methodDefinition, "System.Void BattleTechModLoader.BTModLoader::Init()"))
                        return true;
                }

                // also have to check in places like IEnumerator generated methods (Nested)
                foreach (var nestedType in type.NestedTypes)
                foreach (var methodDefinition in nestedType.Methods)
                {
                    if (IsMethodCalledInMethod(methodDefinition, "System.Void BattleTechModLoader.BTModLoader::Init()"))
                        return true;
                }
            }

            return false;
        }

        private static bool IsModTekInjected(ModuleDefinition game)
        {
            return game.GetType(HOOK_TYPE).Methods.Any(x => x.Name == INJECT_METHOD);
        }

        private static bool IsMethodCalledInMethod(MethodDefinition methodDefinition, string methodSignature)
        {
            if (methodDefinition.Body == null)
                return false;

            foreach (var instruction in methodDefinition.Body.Instructions)
            {
                if (instruction.OpCode.Equals(OpCodes.Call) &&
                    instruction.Operand.ToString().Equals(methodSignature))
                    return true;
            }

            return false;
        }

        private static void Inject(string hookFilePath, string injectFilePath, string factionsFilePath)
        {
            WriteLine($"Injecting {Path.GetFileName(hookFilePath)} with {INJECT_TYPE}.{INJECT_METHOD} at {HOOK_TYPE}.{HOOK_METHOD}");

            using (var game = ModuleDefinition.ReadModule(hookFilePath, new ReaderParameters { ReadWrite = true, AssemblyResolver = managedAssemblyResolver }))
            using (var injecting = ModuleDefinition.ReadModule(injectFilePath))
            {
                var success = InjectLoadFunction(game, injecting);
                success &= InjectFunctionCall(game);

                if (!string.IsNullOrEmpty(factionsFilePath))
                    success &= InjectNewFactions(game, factionsFilePath);

                success &= WriteNewAssembly(game, hookFilePath);

                if (!success)
                    WriteLine("Failed to inject the game assembly.");
            }
        }

        private static bool InjectLoadFunction(ModuleDefinition game, ModuleDefinition injecting)
        {
            var type = game.GetType(HOOK_TYPE);
            var copyMethodBody = injecting.GetType(INJECT_TYPE).Methods.Single(x => x.Name == INJECT_METHOD);
            var injectedMethod = new MethodDefinition(INJECT_METHOD, MethodAttributes.Private | MethodAttributes.Static, game.ImportReference(typeof(void)));

            foreach (var instruction in copyMethodBody.Body.Instructions)
            {
                if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                {
                    injectedMethod.Body.GetILProcessor().Append(Instruction.Create(instruction.OpCode, game.ImportReference((MethodReference)instruction.Operand)));
                }
                else
                {
                    injectedMethod.Body.GetILProcessor().Append(instruction);
                }
            }

            type.Methods.Add(injectedMethod);

            return true;
        }

        private static bool InjectFunctionCall(ModuleDefinition game)
        {
            // get the methods that we're hooking and injecting
            var injectedMethod = game.GetType(HOOK_TYPE).Methods.Single(x => x.Name == INJECT_METHOD);
            var hookedMethod = game.GetType(HOOK_TYPE).Methods.First(x => x.Name == HOOK_METHOD);

            // If the return type is an iterator -- need to go searching for its MoveNext method which contains the actual code you'll want to inject
            if (hookedMethod.ReturnType.Name.Equals("IEnumerator"))
            {
                var nestedIterator = game.GetType(HOOK_TYPE).NestedTypes.First(x =>
                    x.Name.Contains(HOOK_METHOD) && x.Name.Contains("Iterator"));
                hookedMethod = nestedIterator.Methods.First(x => x.Name.Equals("MoveNext"));
            }

            // As of BattleTech v1.1 the Start() iterator method of BattleTech.Main has this at the end
            //
            //  ...
            //
            //      Serializer.PrepareSerializer();
            //      this.activate.enabled = true;
            //      yield break;
            //
            //  }
            //

            // We want to inject after the PrepareSerializer call -- so search for that call in the CIL
            var targetInstruction = -1;
            for (var i = 0; i < hookedMethod.Body.Instructions.Count; i++)
            {
                var instruction = hookedMethod.Body.Instructions[i];
                if (instruction.OpCode.Code.Equals(Code.Call) && instruction.OpCode.OperandType.Equals(OperandType.InlineMethod))
                {
                    var methodReference = (MethodReference)instruction.Operand;
                    if (methodReference.Name.Contains("PrepareSerializer"))
                        targetInstruction = i;
                }
            }

            if (targetInstruction == -1)
                return false;

            hookedMethod.Body.GetILProcessor().InsertAfter(hookedMethod.Body.Instructions[targetInstruction],
                Instruction.Create(OpCodes.Call, game.ImportReference(injectedMethod)));

            return true;
        }

        private static bool WriteNewAssembly(ModuleDefinition game, string hookFilePath)
        {
            // save the modified assembly
            WriteLine($"Writing back to {Path.GetFileName(hookFilePath)}...");
            game.Write();
            WriteLine("Injection complete!");
            return true;
        }


        // FACTIONS
        private struct FactionStub
        {
            public string Name;
            public int Id;
        }

        private static List<FactionStub> ReadFactions(string path)
        {
            var factionDefinition = new { Faction = "" };
            var factions = new List<FactionStub>();
            var id = FACTION_ENUM_STARTING_ID;

            using (var archive = ZipFile.Read(path))
            {
                foreach (var entry in archive)
                {
                    if (!entry.FileName.StartsWith("faction_", StringComparison.OrdinalIgnoreCase)
                        || !entry.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    using (var reader = new StreamReader(entry.OpenReader()))
                    {
                        var faction = JsonConvert.DeserializeAnonymousType(reader.ReadToEnd(), factionDefinition);
                        factions.Add(new FactionStub { Name = faction.Faction, Id = id });
                        id++;
                    }
                }
            }

            return factions;
        }

        private static bool InjectNewFactions(ModuleDefinition game, string path)
        {
            Write("Injecting factions... ");

            var factions = ReadFactions(path);
            var factionBase = game.GetType("BattleTech.Faction");

            foreach (var faction in factions)
                factionBase.Fields.Add(new FieldDefinition(faction.Name, ENUM_ATTRIBUTES, factionBase) { Constant = faction.Id });

            WriteLine($"Injected {factions.Count} factions.");
            return true;
        }


        // BACKUP
        private static void Backup(string filePath, string backupFilePath)
        {
            File.Copy(filePath, backupFilePath, true);
            WriteLine($"{Path.GetFileName(filePath)} backed up to {Path.GetFileName(backupFilePath)}");
        }

        private static void Restore(string filePath, string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
                throw new BackupFileNotFound();

            using (var backup = ModuleDefinition.ReadModule(backupFilePath))
            if (IsBTMLInjected(backup) || IsModTekInjected(backup))
                throw new BackupFileInjected();

            File.Copy(backupFilePath, filePath, true);
            WriteLine($"{Path.GetFileName(backupFilePath)} restored to {Path.GetFileName(filePath)}");
        }


        // CONSOLE UTIL
        private static string GetProductVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        private static bool PromptForUpdateYesNo(bool requireKeyPress)
        {
            if (!requireKeyPress)
                return true;

            WriteLine("ModTek injection detected. Would you like to re-inject? (y/n)");
            return ReadKey().Key == ConsoleKey.Y;
        }

        private static void PromptForKey(bool requireKeyPress)
        {
            if (!requireKeyPress)
                return;

            WriteLine("Press any key to continue.");
            ReadKey();
        }


        // CONSOLE OUTPUT
        private static void SayUpdatingFromBTML()
        {
            WriteLine("BTML injection detected, restoring and injecting");
        }

        private static void SayInjectedStatus(bool btmlInjected, bool modTekInjected)
        {
            SayHeader();

            if (btmlInjected)
                WriteLine("BTML Injected");

            if (modTekInjected)
                WriteLine("ModTek Injected");
        }

        private static void SayHelp(OptionSet p)
        {
            SayHeader();
            WriteLine("Usage: ModTekInjector.exe [OPTIONS]+");
            WriteLine("Inject the BattleTech game assembly with an entry point for mod loading.");
            WriteLine("If no options are specified, the program assumes you want to /install.");
            WriteLine();
            WriteLine("Options:");
            p.WriteOptionDescriptions(Out);
        }

        private static void SayGameVersion(string version)
        {
            WriteLine(version);
        }

        private static void SayRequiredGameVersion(string version, string expectedVersion)
        {
            WriteLine($"Expected BTG v{expectedVersion}");
            WriteLine($"Actual BTG v{version}");
        }

        private static void SayRequiredGameVersionMismatchMessage(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
                WriteLine(msg);
        }

        private static void SayVersion()
        {
            WriteLine(GetProductVersion());
        }

        private static void SayOptionException(OptionException e)
        {
            SayHeader();
            Write("ModTekInjector.exe: ");
            WriteLine(e.Message);
            WriteLine("Try `ModTekInjector.exe --help' for more information.");
        }

        private static void SayManagedDirMissingError(string givenManagedDir)
        {
            SayHeader();
            WriteLine($"ERROR: We could not find the directory '{givenManagedDir}'. Are you sure it exists?");
        }

        private static void SayGameAssemblyMissingError(string givenManagedDir)
        {
            SayHeader();
            WriteLine($"ERROR: We could not find the BTG assembly {GAME_DLL_FILE_NAME} in directory '{givenManagedDir}'.\n" +
                "Are you sure that is the correct directory?");
        }

        private static void SayModTekAssemblyMissingError(string expectedModLoaderAssemblyPath)
        {
            SayHeader();
            WriteLine($"ERROR: We could not find the BTG assembly {MODTEK_DLL_FILE_NAME} at '{expectedModLoaderAssemblyPath}'.\n" +
                $"Is {MODTEK_DLL_FILE_NAME} in the correct place? It should be in the same directory as this injector executable.");
        }

        private static void SayFactionsFileMissing(string factionsFilePath)
        {
            SayHeader();
            WriteLine($"ERROR: We could not find the provided factions zip at '{factionsFilePath}'");
        }

        private static void SayHeader()
        {
            WriteLine("ModTek Injector");
            WriteLine("---------------");
        }

        private static void SayHowToRecoverMissingBackup(string backupFileName)
        {
            WriteLine("----------------------------");
            WriteLine($"The backup game assembly file must be in the directory with the injector for /restore to work. The backup file should be named \"{backupFileName}\".");
            WriteLine("You may need to reinstall or use Steam/GOG's file verification function if you have no other backup.");
        }

        private static void SayHowToRecoverInjectedBackup(string backupFileName)
        {
            WriteLine("----------------------------");
            WriteLine($"The backup game assembly file named \"{backupFileName}\" was already injected. Something has gone wrong.");
            WriteLine("You may need to reinstall or use Steam/GOG's file verification function if you have no other backup.");
        }

        private static void SayAlreadyInjected(bool isModTekInjection)
        {
            WriteLine(isModTekInjection
                ? $"ERROR: {GAME_DLL_FILE_NAME} already ModTek injected."
                : $"ERROR: {GAME_DLL_FILE_NAME} injected with BattleTechModLoader (BTML).  Please revert the file and re-run injector!");
        }

        private static void SayAlreadyRestored()
        {
            WriteLine($"{GAME_DLL_FILE_NAME} already restored.");
        }

        private static void SayUpdateCanceled()
        {
            WriteLine($"{GAME_DLL_FILE_NAME} update cancelled.");
        }

        private static void SayException(Exception e)
        {
            WriteLine($"ERROR: An exception occured: {e}");
        }
    }

    public class BackupFileInjected : Exception
    {
        public BackupFileInjected(string backupFileName = "Assembly-CSharp.dll.orig") : base(FormulateMessage(backupFileName))
        {
            BackupFileName = backupFileName;
        }

        public string BackupFileName { get; }

        private static string FormulateMessage(string backupFileName)
        {
            return $"The backup file \"{backupFileName}\" was injected by BTML or ModTek.";
        }
    }

    public class BackupFileNotFound : FileNotFoundException
    {
        public BackupFileNotFound(string backupFileName = "Assembly-CSharp.dll.orig") : base(FormulateMessage(backupFileName))
        {
            BackupFileName = backupFileName;
        }

        public string BackupFileName { get; }

        private static string FormulateMessage(string backupFileName)
        {
            return $"The backup file \"{backupFileName}\" could not be found.";
        }
    }
}
