using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnityShaderCompilerListener
{
	internal class Program
	{
		static char[] hexChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
		
		static string ToHex(byte b)
		{
			return $"{hexChars[b / 16]}{hexChars[b % 16]}";
		}

		static async Task Main(string[] args)
		{
			string compilerPath = @"C:\Program Files\Unity\Hub\Editor\2022.3.28f1\Editor\Data\Tools\_UnityShaderCompiler.exe";
			Console.Write("working dir: ");
			string workingDir = Console.ReadLine();

			string basePath = @"C:/Program Files/Unity/Hub/Editor/2022.3.28f1/Editor/Data";
			string logPath = Path.Combine(Directory.GetCurrentDirectory(), "comp_out.txt");
			int port = 0;
			string pluginsPath = @"C:/Program Files/Unity/Hub/Editor/2022.3.28f1/Editor/Data/PlaybackEngines";
			string localIpcStreamName = $"ShaderCompilerIPC-{Process.GetCurrentProcess().Id}-21668";

			using (NamedPipeServerStream server = new NamedPipeServerStream($"Unity-{localIpcStreamName}"))
			{
				ProcessStartInfo info = new ProcessStartInfo(compilerPath);
				info.Arguments = $"\"{basePath}\" \"{logPath}\" {port} -local-ipc-stream={localIpcStreamName} \"{pluginsPath}\"";
				info.WorkingDirectory = workingDir;
				info.CreateNoWindow = true;
				info.WindowStyle = ProcessWindowStyle.Hidden;
				info.UseShellExecute = false;
				info.RedirectStandardOutput = true;
				info.RedirectStandardError = true;

				Process compProcess = Process.Start(info);
				StringBuilder procOut = new StringBuilder();
				StringBuilder errOut = new StringBuilder();

				compProcess.OutputDataReceived += (o, e) =>
				{
					procOut.Append(e.Data);
				};

				compProcess.ErrorDataReceived += (o, e) =>
				{
					errOut.Append(e.Data);
				};

				await server.WaitForConnectionAsync();

				Console.WriteLine("Compiler connected!");

				CancellationTokenSource compilerExitSource = new CancellationTokenSource();
				compProcess.EnableRaisingEvents = true;
				compProcess.Exited += (o, e) => compilerExitSource.Cancel();

				byte[] buff = new byte[4096];
				int written = 0;
				while (!compProcess.HasExited)
				{
					int read = await server.ReadAsync(buff, 0, buff.Length, compilerExitSource.Token);
					if (compProcess.HasExited)
						break;

					for (int i = 0; i < read; i++)
					{
						byte b = buff[i];
						Console.Write($"{ToHex(b)} ");

						if (++written == 8)
						{
							Console.WriteLine();
							written = 0;
						}
					}

					Console.WriteLine();
					Console.WriteLine();
					written = 0;
				}

				Console.WriteLine();
				Console.WriteLine($"Compiler process exited with code {compProcess.ExitCode}");
				Console.WriteLine();

				Console.WriteLine($"Process output:");
				Console.WriteLine(procOut.ToString());
				Console.WriteLine();

				Console.WriteLine("Process error:");
				Console.WriteLine(errOut.ToString());
				Console.WriteLine();

				if (File.Exists(logPath))
				{
					Console.WriteLine("Process log:");
					Console.WriteLine(File.ReadAllText(logPath));
				}
			}
		}
	}
}
