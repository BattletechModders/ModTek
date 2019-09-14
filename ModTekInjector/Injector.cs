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

        private static readonly List<string> MANAGED_DIRECTORY_SEEK_LIST = new List<string>
        {
            "../../BattleTech_Data/Managed",
            "../../Data/Managed"
        };

        private static readonly List<string> MANAGED_DIRECTORY_OLD_FILES = new List<string> { "0Harmony.dll", "BattleTechModLoader.dll", "BattleTechModLoaderInjector.exe", "Mono.Cecil.dll", "rt-factions.zip" };
        private static readonly List<string> MOD_DIRECTORY_OLD_FILES = new List<string> { "ModTek.dll", "modtekassetbundle", "BTModLoader.log" };

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
                v => OptionsIn.PerformOperation = ReceivedOptions.Operation.Detect
            },
            {
                "g|gameversion",
                "Print the game version number",
                v => OptionsIn.PerformOperation = ReceivedOptions.Operation.GameVersion
            },
            {
                "h|?|help",
                "Print this useful help message",
                v => OptionsIn.PerformOperation = ReceivedOptions.Operation.Help
            },
            {
                "i|install",
                "Inject ModTek into game assembly (this is the default behavior)",
                v => OptionsIn.PerformOperation = ReceivedOptions.Operation.Install
            },
            {
                "m=|manageddir=",
                "specify managed dir where BattleTech's Assembly-CSharp.dll is located",
                v => OptionsIn.ManagedDirectory = v
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
                v => OptionsIn.PerformOperation = ReceivedOptions.Operation.Restore
            },
            {
                "v|version",
                "Print the ModTekInjector version number",
                v => OptionsIn.PerformOperation = ReceivedOptions.Operation.Version
            },
            {
                "f=|factions=",
                "Specify a zip file with factions to inject",
                v => OptionsIn.FactionsPath = v
            }
        };


        // ENTRY POINT
        private static int Main(string[] args)
        {
            try
            {
                // parse options
                try
                {
                    Options.Parse(args);
                }
                catch (OptionException e)
                {
                    SayOptionException(e);
                    return RC_BAD_OPTIONS;
                }

                // handle operations that don't require reading the assembly
                switch (OptionsIn.PerformOperation)
                {
                    case ReceivedOptions.Operation.Help:
                        SayHelp(Options);
                        return RC_NORMAL;

                    case ReceivedOptions.Operation.Version:
                        SayVersion();
                        return RC_NORMAL;
                }

                SayHeader();

                // find managed directory, setup assembly resolver to look there
                var managedDirectory = GetManagedDirectoryPath(OptionsIn.ManagedDirectory);
                if (managedDirectory == null)
                {
                    SayManagedDirMissingError(OptionsIn.ManagedDirectory);
                    return RC_BAD_MANAGED_DIRECTORY_PROVIDED;
                }
                managedAssemblyResolver = new ManagedAssemblyResolver(managedDirectory);

                // setup paths to DLLs and backups
                var gameDLLPath = Path.Combine(managedDirectory, GAME_DLL_FILE_NAME);
                var gameDLLBackupPath = Path.Combine(managedDirectory, GAME_DLL_FILE_NAME + BACKUP_FILE_EXT);
                var modTekDLLPath = Path.Combine(Directory.GetCurrentDirectory(), MODTEK_DLL_FILE_NAME);
                var modDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..");

                if (!File.Exists(gameDLLPath))
                {
                    SayGameAssemblyMissingError(OptionsIn.ManagedDirectory);
                    return RC_BAD_MANAGED_DIRECTORY_PROVIDED;
                }

                if (!File.Exists(modTekDLLPath))
                {
                    SayModTekAssemblyMissingError(modTekDLLPath);
                    return RC_MISSING_MODTEK_ASSEMBLY;
                }

                // setup factionsPath
                var factionsPath = GetFactionPath(OptionsIn.FactionsPath);
                if (!string.IsNullOrEmpty(OptionsIn.FactionsPath) && !File.Exists(factionsPath))
                {
                    SayFactionsFileMissing(factionsPath);
                    return RC_MISSING_FACTION_FILE;
                }

                if (string.IsNullOrEmpty(factionsPath) && OptionsIn.RequireKeyPress)
                {
                    var path = GetSingleZipFilePath(Directory.GetCurrentDirectory());

                    if (!string.IsNullOrEmpty(path))
                    {
                        SayMaybeInjectFactionZip(path);
                        if (PromptForYesNo(OptionsIn.RequireKeyPress))
                            factionsPath = path;
                    }
                }

                // read the assembly for game version and injected status
                bool btmlInjected, modTekInjected, anyInjected;
                string gameVersion;
                using (var game = ModuleDefinition.ReadModule(gameDLLPath))
                {
                    gameVersion = GetGameVersion(game);
                    btmlInjected = IsBTMLInjected(game);
                    modTekInjected = IsModTekInjected(game);
                    anyInjected = btmlInjected || modTekInjected;
                }

                // check game version vs. required game version if given
                if (!string.IsNullOrEmpty(OptionsIn.RequiredGameVersion) && gameVersion != OptionsIn.RequiredGameVersion)
                {
                    SayRequiredGameVersion(gameVersion, OptionsIn.RequiredGameVersion);
                    SayRequiredGameVersionMismatchMessage(OptionsIn.RequiredGameVersionMismatchMessage);
                    PromptForKey(OptionsIn.RequireKeyPress);
                    return RC_REQUIRED_GAME_VERSION_MISMATCH;
                }

                // handle operations that require reading the assembly
                switch (OptionsIn.PerformOperation)
                {
                    case ReceivedOptions.Operation.Detect:
                        SayInjectedStatus(btmlInjected, modTekInjected);
                        return RC_NORMAL;

                    case ReceivedOptions.Operation.GameVersion:
                        SayGameVersion(gameVersion);
                        return RC_NORMAL;

                    case ReceivedOptions.Operation.Restore:
                        if (anyInjected)
                            Restore(gameDLLPath, gameDLLBackupPath);
                        else
                            SayAlreadyRestored();

                        PromptForKey(OptionsIn.RequireKeyPress);
                        return RC_NORMAL;

                    case ReceivedOptions.Operation.Install:
                        if (btmlInjected)
                        {
                            SayUpdatingFromBTML();
                            Restore(gameDLLPath, gameDLLBackupPath);
                            gameDLLBackupPath = null;
                        }
                        else if (modTekInjected)
                        {
                            SayAlreadyInjected();

                            if (PromptForYesNo(OptionsIn.RequireKeyPress))
                            {
                                Restore(gameDLLPath, gameDLLBackupPath);
                                gameDLLBackupPath = null;
                            }
                            else
                            {
                                SayUpdateCanceled();
                                return RC_NORMAL;
                            }
                        }

                        // have restored a non-injected assembly or have a non injected assembly at this point
                        // if backups restored, path to backup nullified, so not backed up again
                        if (!string.IsNullOrEmpty(gameDLLBackupPath))
                            Backup(gameDLLPath, gameDLLBackupPath);

                        Inject(gameDLLPath, modTekDLLPath, factionsPath);

                        if (HasOldFiles(modDirectory, managedDirectory))
                        {
                            SayHasOldFiles();

                            if (PromptForYesNo(OptionsIn.RequireKeyPress))
                                DeleteOldFiles(modDirectory, managedDirectory);
                        }

                        PromptForKey(OptionsIn.RequireKeyPress);
                        return RC_NORMAL;
                }
            }
            catch (BackupFileNotFound e)
            {
                SayException(e);
                SayHowToRecoverMissingBackup(e.BackupFileName);
                PromptForKey(true);
                return RC_MISSING_BACKUP_FILE;
            }
            catch (BackupFileInjected e)
            {
                SayException(e);
                SayHowToRecoverInjectedBackup(e.BackupFileName);
                PromptForKey(true);
                return RC_BACKUP_FILE_INJECTED;
            }
            catch (Exception e)
            {
                SayException(e);
                PromptForKey(true);
            }

            return RC_UNHANDLED_STATE;
        }


        // PATHS / FILES
        private static string GetManagedDirectoryPath(string pathIn)
        {
            if (!string.IsNullOrEmpty(pathIn))
            {
                var path = pathIn;

                if (!Path.IsPathRooted(pathIn))
                    path = Path.Combine(Directory.GetCurrentDirectory(), pathIn);

                return File.Exists(Path.Combine(path, GAME_DLL_FILE_NAME)) ? Path.GetFullPath(path) : null;
            }

            var currentDirectory = Directory.GetCurrentDirectory();
            foreach (var seek in MANAGED_DIRECTORY_SEEK_LIST)
            {
                var seekPath = Path.Combine(currentDirectory, seek);
                var fileSeekPath = Path.Combine(seekPath, GAME_DLL_FILE_NAME);
                var fullPath = Path.GetFullPath(fileSeekPath);
                if (File.Exists(fullPath))
                    return seekPath;
            }

            return null;
        }

        private static string GetFactionPath(string pathIn)
        {
            if (string.IsNullOrEmpty(pathIn))
                return null;

            var factionsPath = pathIn;
            if (!Path.IsPathRooted(factionsPath))
                factionsPath = Path.Combine(Directory.GetCurrentDirectory(), factionsPath);

            return factionsPath;
        }

        private static bool HasOldFiles(string modDirectory, string managedDirectory)
        {
            foreach (var oldFile in MOD_DIRECTORY_OLD_FILES)
            {
                var path = Path.Combine(modDirectory, oldFile);
                if (File.Exists(path))
                    return true;
            }

            foreach (var oldFile in MANAGED_DIRECTORY_OLD_FILES)
            {
                var path = Path.Combine(managedDirectory, oldFile);
                if (File.Exists(path))
                    return true;
            }

            return false;
        }

        private static void DeleteOldFiles(string modDirectory, string managedDirectory)
        {
            foreach (var oldFile in MOD_DIRECTORY_OLD_FILES)
            {
                var path = Path.Combine(modDirectory, oldFile);
                if (File.Exists(path))
                    File.Delete(path);
            }

            foreach (var oldFile in MANAGED_DIRECTORY_OLD_FILES)
            {
                var path = Path.Combine(managedDirectory, oldFile);
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static string GetSingleZipFilePath(string directory)
        {
            var paths = Directory.GetFiles(directory).Where(file => Path.GetExtension(file).ToLowerInvariant() == ".zip").ToList();
            return paths.Count == 1 ? paths[0] : null;
        }


        // UTIL
        private static string GetInjectorVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        private static string GetGameVersion(ModuleDefinition game)
        {
            var gameVersionType = game.GetType(GAME_VERSION_TYPE);
            return gameVersionType?.Fields.First(x => x.IsLiteral && !x.IsInitOnly && x.Name == GAME_VERSION_CONST)?.Constant.ToString();
        }

        private static MethodDefinition CopyMethod(TypeDefinition copyToTypedef, MethodDefinition sourceMethod)
        {
            // adapted from: https://groups.google.com/forum/#!msg/mono-cecil/uoMLJEZrQ1Q/ewthqjEk-jEJ
            // create a new MethodDefinition; all the content of sourceMethod will be copied to this new MethodDefinition
            var targetModule = copyToTypedef.Module;
            var newMethod = new MethodDefinition(sourceMethod.Name, sourceMethod.Attributes, targetModule.ImportReference(sourceMethod.ReturnType));

            // copy the parameters
            foreach (var parameter in sourceMethod.Parameters)
            {
                newMethod.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, targetModule.ImportReference(parameter.ParameterType)));
            }

            // copy the body
            var newBody = newMethod.Body;
            var oldBody = sourceMethod.Body;
            newBody.InitLocals = oldBody.InitLocals;

            // copy the local variable definition
            foreach (var variable in oldBody.Variables)
            {
                newBody.Variables.Add(new VariableDefinition(targetModule.ImportReference(variable.VariableType)));
            }

            // copy the IL; we only need to take care of reference and method definitions
            var newInstructions = newBody.Instructions;
            foreach (var instruction in oldBody.Instructions)
            {
                var operand = instruction.Operand;

                switch (operand)
                {
                    case MethodDefinition method:
                        // for any methodDef that this method calls, we will copy it
                        newInstructions.Add(Instruction.Create(instruction.OpCode, CopyMethod(copyToTypedef, method)));
                        continue;
                    case FieldReference field:
                        // for member reference, import it
                        newInstructions.Add(Instruction.Create(instruction.OpCode, targetModule.ImportReference(field)));
                        continue;
                    case TypeReference type:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, targetModule.ImportReference(type)));
                        continue;
                    case MethodReference method:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, targetModule.ImportReference(method)));
                        continue;
                    case byte b:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, b));
                        continue;
                    case double d:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, d));
                        continue;
                    case float f:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, f));
                        continue;
                    case int i:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, i));
                        continue;
                    case long l:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, l));
                        continue;
                    case sbyte sb:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, sb));
                        continue;
                    case string str:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, str));
                        continue;
                    case CallSite callSite:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, callSite));
                        continue;
                    case Instruction instruct:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, instruct));
                        continue;
                    case Instruction[] instructArray:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, instructArray));
                        continue;
                    case ParameterDefinition parameter:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, new ParameterDefinition(parameter.Name, parameter.Attributes, targetModule.ImportReference(parameter.ParameterType))));
                        continue;
                    case VariableDefinition variableDefinition:
                        newInstructions.Add(Instruction.Create(instruction.OpCode, new VariableDefinition(targetModule.ImportReference(variableDefinition.VariableType))));
                        continue;
                    case null:
                        newInstructions.Add(Instruction.Create(instruction.OpCode));
                        continue;
                    default:
                        WriteLine($"UNHANDLED OPERAND -- {instruction.OpCode} {instruction.Operand}");
                        break;
                }
            }

            // copy the exception handler blocks
            foreach (var oldExceptionHandler in oldBody.ExceptionHandlers)
            {
                var newExceptionHandler = new ExceptionHandler(oldExceptionHandler.HandlerType);
                newExceptionHandler.CatchType = targetModule.ImportReference(oldExceptionHandler.CatchType);

                // we need to setup neh.Start and End; these are instructions; we need to locate it in the source by index
                if (oldExceptionHandler.TryStart != null)
                    newExceptionHandler.TryStart = newInstructions[oldBody.Instructions.IndexOf(oldExceptionHandler.TryStart)];

                if (oldExceptionHandler.TryEnd != null)
                    newExceptionHandler.TryEnd = newInstructions[oldBody.Instructions.IndexOf(oldExceptionHandler.TryEnd)];

                if (oldExceptionHandler.FilterStart != null)
                    newExceptionHandler.FilterStart = newInstructions[oldBody.Instructions.IndexOf(oldExceptionHandler.FilterStart)];

                if (oldExceptionHandler.HandlerStart != null)
                    newExceptionHandler.HandlerStart = newInstructions[oldBody.Instructions.IndexOf(oldExceptionHandler.HandlerStart)];

                if (oldExceptionHandler.HandlerEnd != null)
                    newExceptionHandler.HandlerEnd = newInstructions[oldBody.Instructions.IndexOf(oldExceptionHandler.HandlerEnd)];

                newBody.ExceptionHandlers.Add(newExceptionHandler);
            }

            // add this method to the target typedef
            copyToTypedef.Methods.Add(newMethod);
            newMethod.DeclaringType = copyToTypedef;

            return newMethod;
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


        // INJECTION
        private static bool IsBTMLInjected(ModuleDefinition game)
        {
            var searchTypes = new List<TypeDefinition>
            {
                game.GetType("BattleTech.Main"),
                game.GetType("BattleTech.GameInstance")
            };

            foreach (var type in searchTypes)
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

        private static void Inject(string hookFilePath, string injectFilePath, string factionsFilePath)
        {
            using (var game = ModuleDefinition.ReadModule(hookFilePath, new ReaderParameters { ReadWrite = true, AssemblyResolver = managedAssemblyResolver }))
            using (var injecting = ModuleDefinition.ReadModule(injectFilePath))
            {
                WriteLine($"Injecting {Path.GetFileName(hookFilePath)} with {INJECT_TYPE}.{INJECT_METHOD} at {HOOK_TYPE}.{HOOK_METHOD}");

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
            CopyMethod(game.GetType(HOOK_TYPE), injecting.GetType(INJECT_TYPE).Methods.Single(x => x.Name == INJECT_METHOD));
            return true;
        }

        private static bool InjectFunctionCall(ModuleDefinition game)
        {
            // get the methods that we're hooking and injecting
            var hookType = game.GetType(HOOK_TYPE);
            var methods = hookType.Methods;
            var injectedMethod = methods.Single(x => x.Name == INJECT_METHOD);
            var hookedMethod = methods.First(x => x.Name == HOOK_METHOD);

            // if the return type is an iterator -- need to go searching for its MoveNext method which contains the actual code you'll want to inject
            if (hookedMethod.ReturnType.Name.Equals("IEnumerator"))
            {
                var nestedIterator = hookType.NestedTypes.First(x => x.Name.Contains(HOOK_METHOD));
                hookedMethod = nestedIterator.Methods.First(x => x.Name.Equals("MoveNext"));
            }

            // As of BattleTech v1.1 the Start() iterator method of BattleTech.Main has this at the end
            //  ...
            //  Serializer.PrepareSerializer();
            //  this.activate.enabled = true;
            //  yield break;
            //}

            // we want to inject after the PrepareSerializer call -- so search for that call in the CIL
            var targetInstruction = -1;
            for (var i = 0; i < hookedMethod.Body.Instructions.Count; i++)
            {
                var instruction = hookedMethod.Body.Instructions[i];

                if (!instruction.OpCode.Code.Equals(Code.Call) || !instruction.OpCode.OperandType.Equals(OperandType.InlineMethod))
                    continue;

                var methodReference = (MethodReference)instruction.Operand;
                if (methodReference.Name.Contains("PrepareSerializer"))
                    targetInstruction = i;
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
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

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
            {
                if (IsBTMLInjected(backup) || IsModTekInjected(backup))
                    throw new BackupFileInjected();
            }

            File.Copy(backupFilePath, filePath, true);
            WriteLine($"{Path.GetFileName(backupFilePath)} restored to {Path.GetFileName(filePath)}");
        }


        // CONSOLE UTIL
        private static bool PromptForYesNo(bool requireKeyPress)
        {
            if (!requireKeyPress)
                return true;

            Write("(y/N): ");

            var yes = ReadKey().Key == ConsoleKey.Y;

            WriteLine();

            return yes;
        }

        private static void PromptForKey(bool requireKeyPress)
        {
            if (!requireKeyPress)
                return;

            WriteLine("Press any key to continue.");
            ReadKey();
            WriteLine();
        }


        // CONSOLE OUTPUT
        private static void SayUpdatingFromBTML()
        {
            WriteLine("BTML injection detected, restoring and injecting");
        }

        private static void SayInjectedStatus(bool btmlInjected, bool modTekInjected)
        {
            if (btmlInjected)
                WriteLine("BTML Injected");

            if (modTekInjected)
                WriteLine("ModTek Injected");

            if (!btmlInjected && !modTekInjected)
                WriteLine("No injection detected. Game assembly appears unmodified.");
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
            WriteLine($"Game Version: {version}");
        }

        private static void SayRequiredGameVersion(string version, string expectedVersion)
        {
            WriteLine("Version mismatch!");
            WriteLine($"Expected BattleTech Version: {expectedVersion}");
            WriteLine($"Actual BattleTech Version: {version}");
        }

        private static void SayRequiredGameVersionMismatchMessage(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
                WriteLine(msg);
        }

        private static void SayVersion()
        {
            WriteLine($"Injector Version: {GetInjectorVersion()}");
        }

        private static void SayOptionException(OptionException e)
        {
            SayHeader();
            Write("ModTekInjector.exe: ");
            WriteLine(e.Message);
            WriteLine("Try `ModTekInjector.exe --help' for more information.");
        }

        private static void SayManagedDirMissingError(string managedDir)
        {
            SayHeader();

            WriteLine(!string.IsNullOrEmpty(managedDir)
                ? $"ERROR: Could not find the directory '{managedDir}'. Are you sure it exists?"
                : "ERROR: Could not find managed directory from current location. Is the injector in the correct location?");
        }

        private static void SayGameAssemblyMissingError(string managedDir)
        {
            SayHeader();
            WriteLine($"ERROR: Could not find the BTG assembly {GAME_DLL_FILE_NAME} in directory '{managedDir}'.\n" +
                "Are you sure that is the correct directory?");
        }

        private static void SayModTekAssemblyMissingError(string modTekPath)
        {
            SayHeader();
            WriteLine($"ERROR: Could not find the ModTek assembly {MODTEK_DLL_FILE_NAME} at '{modTekPath}'.\n" +
                $"Is {MODTEK_DLL_FILE_NAME} in the correct place? It should be in the same directory as this injector executable.");
        }

        private static void SayMaybeInjectFactionZip(string path)
        {
            Write($"Found {Path.GetFileName(path)}, which could be a factions zip. Did you want to inject the factions in this zip? ");
        }

        private static void SayFactionsFileMissing(string factionsPath)
        {
            SayHeader();
            WriteLine($"ERROR: Could not find the provided factions zip at '{factionsPath}'");
        }

        private static void SayHeader()
        {
            WriteLine("ModTek Injector");
            WriteLine("---------------");
            WriteLine();
        }

        private static void SayHasOldFiles()
        {
            Write("Would you like to remove old files from previous ModTek/BTML versions? ");
        }

        private static void SayHowToRecoverMissingBackup(string backupFileName)
        {
            WriteLine("----------------------------");
            WriteLine($"Could not find assembly backup file. The backup file should be named \"{backupFileName}\", and should be in the managed folder");
            WriteLine("You may need to reinstall or use Steam/GOG's file verification function if you have no other backup.");
        }

        private static void SayHowToRecoverInjectedBackup(string backupFileName)
        {
            WriteLine("----------------------------");
            WriteLine($"The backup game assembly file named \"{backupFileName}\" was already injected. Something has gone wrong.");
            WriteLine("You may need to reinstall or use Steam/GOG's file verification function if you have no other backup.");
        }

        private static void SayAlreadyInjected()
        {
            Write("ModTek already injected. Would you like to re-inject: ");
        }

        private static void SayAlreadyRestored()
        {
            WriteLine($"{GAME_DLL_FILE_NAME} is not injected, not restoring from backup.");
        }

        private static void SayUpdateCanceled()
        {
            WriteLine($"{GAME_DLL_FILE_NAME} not changed.");
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
