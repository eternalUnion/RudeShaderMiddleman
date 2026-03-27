using RudeShaderMiddleman.Common.Middleman;
using RudeShaderMiddleman.Common.ShaderTable;
using System.Diagnostics;
using System.IO.Compression;
using System.IO.Pipes;
using System.Text.RegularExpressions;

namespace RudeShaderMiddleman.DotnetCore;

public class Program
{
    // Streams
    private static NamedPipeClientStream unityPipeStream;
	private static NamedPipeServerStream compilerPipeStream;
	private static StreamWriter middlemanOutputLog;

	// Compiler process
	private static Process compProc;

	// Data
	private static Dictionary<string, ShaderEntry> shaders = new Dictionary<string, ShaderEntry>();
	private static ZipArchive blobs;

	static int Main(string[] args)
	{
		int procId = Process.GetCurrentProcess().Id;

		string workingDir = Directory.GetCurrentDirectory();
		string currentDir = Directory.GetParent(Environment.ProcessPath).FullName;
		string compilerPath = Path.Combine(currentDir, "_UnityShaderCompiler");

		if (!Directory.Exists("Logs"))
			Directory.CreateDirectory("Logs");

		using (middlemanOutputLog = new StreamWriter(File.Open(Path.Combine("Logs", $"middleman-{procId}.txt"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)))
		{
			middlemanOutputLog.AutoFlush = true;

			try
			{
				middlemanOutputLog.WriteLine($"Working directory: '{workingDir}'");
				middlemanOutputLog.WriteLine($"Current directory: '{currentDir}'");

				if (!File.Exists(compilerPath))
				{
					middlemanOutputLog.WriteLine($"Unity shader compiler not found at '{compilerPath}'");
					Console.WriteLine($"Unity shader compiler not found at '{compilerPath}'");
					return 1;
				}

				string helpLine = "UnityShaderCompiler-RudeMiddleman: Usage: UnityShaderCompiler <base folder> <log path> <port number> [pluginsPath] [-force-plugins-load] [-local-ipc-stream=<stream name>]";

				if (args.Length < 4)
				{
					Console.WriteLine(helpLine);
					middlemanOutputLog.WriteLine(helpLine);
					return 1;
				}

				string baseFolderPath = args[0];
				string logPath = args[1];
				if (!int.TryParse(args[2], out int port))
				{
					Console.WriteLine("UnityShaderCompiler-RudeMiddleman: Invalid port number");
					middlemanOutputLog.WriteLine("UnityShaderCompiler-RudeMiddleman: Invalid port number");
					return 1;
				}

				Regex ipcStreamNamePattern = new Regex("^-local-ipc-stream=(.*)");

				string pluginsPath = null;
				string? streamName = null;
				bool forceLoadPlugins = false;

				for (int i = 3; i < args.Length; i++)
				{
					string arg = args[i];
					var match = ipcStreamNamePattern.Match(arg);

					if (match.Success)
					{
						streamName = match.Groups[1].Value;
					}
					if (arg == "-force-plugins-load")
					{
						forceLoadPlugins = true;
					}
					else
					{
						pluginsPath = arg;
					}
				}

				void StartCompilerProcess(string? localStreamName)
				{
					ProcessStartInfo info = new ProcessStartInfo();
					info.FileName = compilerPath;
					info.WorkingDirectory = workingDir;

					string compilerLogPath = Path.Combine("Logs", $"unityshader-{procId}.txt");
					info.Arguments = $"\"{baseFolderPath}\" \"{compilerLogPath}\" {port}{(localStreamName == null ? "" : $" -local-ipc-stream={localStreamName}")} \"{pluginsPath}\"{(forceLoadPlugins ? " -force-plugins-load" : "")}";
					info.CreateNoWindow = true;
					info.WindowStyle = ProcessWindowStyle.Hidden;

					compProc = Process.Start(info);
				}

				string tablePath = Path.Combine(currentDir, "table.bin");
				if (!File.Exists(tablePath))
				{
					middlemanOutputLog.WriteLine($"ERROR: Could not locate table.bin at '{tablePath}'");
					Console.WriteLine($"ERROR: Could not locate table.bin at '{tablePath}'");

					StartCompilerProcess(streamName);
					compProc.WaitForExit();
					return compProc.ExitCode;
				}

				string blobPath = Path.Combine(currentDir, "blobs.zip");
				if (!File.Exists(blobPath))
				{
					middlemanOutputLog.WriteLine($"ERROR: Could not locate blobs.zip at '{blobPath}'");
					Console.WriteLine($"ERROR: Could not locate blobs.zip at '{blobPath}'");

					StartCompilerProcess(streamName);
					compProc.WaitForExit();
					return compProc.ExitCode;
				}

				using (BinaryReader reader = new BinaryReader(File.Open(tablePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
				{
					int shaderCnt = reader.ReadInt32();
					for (int i = 0; i < shaderCnt; i++)
					{
						ShaderEntry entry = new ShaderEntry(reader);
						shaders[entry.guid] = entry;
					}
				}

				using (blobs = new ZipArchive(File.Open(blobPath, FileMode.Open, FileAccess.Read, FileShare.Read), ZipArchiveMode.Read))
				{
					using (unityPipeStream = new NamedPipeClientStream($"/tmp/Unity-{streamName}.sock"))
					{
						string compilerStreamName = $"shader-middleman-{procId}";
						using (compilerPipeStream = new NamedPipeServerStream($"/tmp/Unity-{compilerStreamName}.sock"))
						{
							StartCompilerProcess(compilerStreamName);

							compilerPipeStream.WaitForConnection();
							if (!compilerPipeStream.IsConnected)
							{
								middlemanOutputLog.WriteLine("Failed to connect to the compiler!");
								middlemanOutputLog.Flush();
								return 1;
							}

							middlemanOutputLog.WriteLine("Compiler connected!");

							unityPipeStream.Connect(5000);
							if (!unityPipeStream.IsConnected)
							{
								middlemanOutputLog.WriteLine();
								middlemanOutputLog.WriteLine($"Failed to open pipe stream!");
								return 1;
							}

							middlemanOutputLog.WriteLine("Unity connected!");

							compProc.EnableRaisingEvents = true;
							compProc.Exited += (o, e) =>
							{
								middlemanOutputLog.WriteLine($"Compiler process exited with code {compProc.ExitCode}");
								middlemanOutputLog.Flush();
								Environment.Exit(compProc.ExitCode);
							};

							CompilerMiddleman middleman = new CompilerMiddleman(
								unityPipeStream,
								() => unityPipeStream.IsConnected,
								compilerPipeStream,
								() => compilerPipeStream.IsConnected,
								middlemanOutputLog,
								shaders,
								blobs
							);

							middleman.Start();

							// We should never reach this point
							return 1;
						}
					}
				}
			}
			catch (Exception e)
			{
				middlemanOutputLog.WriteLine();
				middlemanOutputLog.WriteLine($"{e.GetType().Name} : {e.Message}");
				middlemanOutputLog.WriteLine(e.StackTrace);
				middlemanOutputLog.Flush();

				unityPipeStream.Dispose();
				compilerPipeStream.Dispose();

				if (compProc != null && !compProc.HasExited)
				{
					compProc.WaitForExit(1000);
				}

				return 1;
			}
		}
	}
}