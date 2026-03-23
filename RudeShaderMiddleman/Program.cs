//#define VERBOSE
using RudeShaderMiddleman.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace RudeShaderMiddleman
{
	public enum VertexComponent : uint
	{
		None = 0xFFFFFFFF,
		Vertex = 0x0,
		Color = 0x1,
		Normal = 0x2,
		TexCoord = 0x3,
		TexCoord0 = 0x4,
		TexCoord1 = 0x5,
		TexCoord2 = 0x6,
		TexCoord3 = 0x7,
		TexCoord4 = 0x8,
		TexCoord5 = 0x9,
		TexCoord6 = 0xA,
		TexCoord7 = 0xB,
		Attrib0 = 0xC,
		Attrib1 = 0xD,
		Attrib2 = 0xE,
		Attrib3 = 0xF,
		Attrib4 = 0x10,
		Attrib5 = 0x11,
		Attrib6 = 0x12,
		Attrib7 = 0x13,
		Attrib8 = 0x14,
		Attrib9 = 0x15,
		Attrib10 = 0x16,
		Attrib11 = 0x17,
		Attrib12 = 0x18,
		Attrib13 = 0x19,
		Attrib14 = 0x1A,
		Attrib15 = 0x1B,
		Count = 0x1C,
	}

	public enum ShaderGpuProgramType
	{
		Unknown = 0,
		GLLegacy = 1,
		GLES31AEP = 2,
		GLES31 = 3,
		GLES3 = 4,
		GLES = 5,
		GLCore32 = 6,
		GLCore41 = 7,
		GLCore43 = 8,
		DX9VertexSM20 = 9,
		DX9VertexSM30 = 10,
		DX9PixelSM20 = 11,
		DX9PixelSM30 = 12,
		DX10Level9Vertex = 13,
		DX10Level9Pixel = 14,
		DX11VertexSM40 = 15,
		DX11VertexSM50 = 16,
		DX11PixelSM40 = 17,
		DX11PixelSM50 = 18,
		DX11GeometrySM40 = 19,
		DX11GeometrySM50 = 20,
		DX11HullSM50 = 21,
		DX11DomainSM50 = 22,
		MetalVS = 23,
		MetalFS = 24,
		SPIRV = 25,
		Console = 26,
		ConsoleVS = 26,
		ConsoleFS = 27,
		ConsoleHS = 28,
		ConsoleDS = 29,
		ConsoleGS = 30,
		RayTracing = 31,
		PS5NGGC = 32
	}

	struct BindChannel
	{
		public int Source;
		public VertexComponent Target;

		public BindChannel(int source, VertexComponent target)
		{
			Source = source;
			Target = target;
		}

		public BindChannel(int source, int target)
		{
			Source = source;
			Target = (VertexComponent)target;
		}

		public BindChannel(BinaryReader reader)
		{
			Source = reader.ReadInt32();
			Target = (VertexComponent)reader.ReadInt32();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Source);
			writer.Write((int)Target);
		}
	}

	class VariantEntry
	{
		// Info
		public int type;
		public long keywords;
		public int sourceMap;

		// Blob location
		public int offset;
		public int length;

		// Status
		public int statsAlu;
		public int statsTex;
		public int statsFlow;
		public int statsTempRegister;

		// Bindings
		public List<BindChannel> inputBindings;
		public ShaderParameters parameters;

		public VariantEntry() { }

		public VariantEntry(BinaryReader reader)
		{
			type = reader.ReadInt32();
			keywords = reader.ReadInt64();
			sourceMap = reader.ReadInt32();
			offset = reader.ReadInt32();
			length = reader.ReadInt32();
			statsAlu = reader.ReadInt32();
			statsTex = reader.ReadInt32();
			statsFlow = reader.ReadInt32();
			statsTempRegister = reader.ReadInt32();

			inputBindings = new List<BindChannel>();
			int inputCount = reader.ReadInt32();
			for (int i = 0; i < inputCount; i++)
			{
				inputBindings.Add(new BindChannel(reader));
			}

			parameters = new ShaderParameters(reader);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(type);
			writer.Write(keywords);
			writer.Write(sourceMap);
			writer.Write(offset);
			writer.Write(length);
			writer.Write(statsAlu);
			writer.Write(statsTex);
			writer.Write(statsFlow);
			writer.Write(statsTempRegister);

			writer.Write((int)inputBindings.Count);
			foreach (BindChannel input in inputBindings)
			{
				input.Serialize(writer);
			}

			parameters.Serialize(writer);
		}
	}

	class ShaderPass
	{
		public int passNum;
		public long vertexKeywordMask;
		public long fragmentKeywordMask;

		public ShaderParameters vertexCommonParameters;
		public ShaderParameters fragmentCommonParameters;

		public List<VariantEntry> vertexVariants = new List<VariantEntry>();

		public List<VariantEntry> fragmentVariants = new List<VariantEntry>();

		public ShaderPass() { }

		public ShaderPass(BinaryReader reader)
		{
			passNum = reader.ReadInt32();
			vertexKeywordMask = reader.ReadInt64();
			fragmentKeywordMask = reader.ReadInt64();

			vertexCommonParameters = new ShaderParameters(reader);
			fragmentCommonParameters = new ShaderParameters(reader);

			vertexVariants = new List<VariantEntry>();
			int vertexVarCount = reader.ReadInt32();
			for (int i = 0; i < vertexVarCount; i++)
			{
				vertexVariants.Add(new VariantEntry(reader));
			}

			fragmentVariants = new List<VariantEntry>();
			int fragmentVarCount = reader.ReadInt32();
			for (int i = 0; i < fragmentVarCount; i++)
			{
				fragmentVariants.Add(new VariantEntry(reader));
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(passNum);
			writer.Write(vertexKeywordMask);
			writer.Write(fragmentKeywordMask);

			vertexCommonParameters.Serialize(writer);
			fragmentCommonParameters.Serialize(writer);

			writer.Write((int)vertexVariants.Count);
			foreach (var vertexVar in vertexVariants)
			{
				vertexVar.Serialize(writer);
			}

			writer.Write((int)fragmentVariants.Count);
			foreach (var fragmentVar in fragmentVariants)
			{
				fragmentVar.Serialize(writer);
			}
		}
	}

	class ShaderEntry
	{
		public string guid = "";
		public List<string> shaderKeywords = new List<string>();
		public List<ShaderPass> shaderPasses = new List<ShaderPass>();

		public ShaderEntry() { }

		public ShaderEntry(BinaryReader reader)
		{
			guid = reader.ReadString();

			shaderKeywords = new List<string>();
			int keywordCount = reader.ReadInt32();
			for (int i = 0; i < keywordCount; i++)
			{
				shaderKeywords.Add(reader.ReadString());
			}

			shaderPasses = new List<ShaderPass>();
			int passCount = reader.ReadInt32();
			for (int i = 0; i < passCount; i++)
			{
				shaderPasses.Add(new ShaderPass(reader));
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(guid);

			writer.Write((int)shaderKeywords.Count);
			foreach (string keyword in shaderKeywords)
			{
				writer.Write(keyword);
			}

			writer.Write((int)shaderPasses.Count);
			foreach (ShaderPass pass in shaderPasses)
			{
				pass.Serialize(writer);
			}
		}
	}

	class Program
	{
		static char[] hexChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

		static string ToHex(byte b)
		{
			return $"{hexChars[b / 16]}{hexChars[b % 16]}";
		}

		private static NamedPipeClientStream unityPipeStream;
		private static NamedPipeServerStream compilerPipeStream;
		private static Dictionary<string, ShaderEntry> shaders = new Dictionary<string, ShaderEntry>();
		private static ZipArchive blobs;

		private static StreamWriter middlemanOutputLog;

		private static Process compProc;

		static int Main(string[] args)
		{
			int procId = Process.GetCurrentProcess().Id;

#if VERBOSE
			using (middlemanOutputLog = new StreamWriter(File.Open(Path.Combine("Logs", $"middleman-{procId}.txt"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)))
#else
			using (middlemanOutputLog = new StreamWriter(new DummyStream()))
#endif
			{
				middlemanOutputLog.AutoFlush = true;

				try
				{
					middlemanOutputLog.WriteLine($"Working directory: {Directory.GetCurrentDirectory()}");

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
					string streamName = null;
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

					string currentPath = Assembly.GetEntryAssembly().Location;
					string currentDir = Path.GetDirectoryName(currentPath);
					middlemanOutputLog.WriteLine($"Current directory: '{currentDir}'");

					string tablePath = Path.Combine(currentDir, "table.bin");
					if (!File.Exists(tablePath))
					{
						middlemanOutputLog.WriteLine($"ERROR: Could not locate table.bin at '{tablePath}'");

						ProcessStartInfo info = new ProcessStartInfo();
						info.FileName = Path.Combine(currentDir, "_UnityShaderCompiler.exe");
						info.WorkingDirectory = Directory.GetCurrentDirectory();

						string compilerLogPath = Path.Combine("Logs", $"unityshader-{procId}.txt");
						info.Arguments = $"\"{baseFolderPath}\" \"{logPath}\" {port} -local-ipc-stream={streamName} \"{pluginsPath}\"{(forceLoadPlugins ? " -force-plugins-load" : "")}";
						info.CreateNoWindow = true;
						info.WindowStyle = ProcessWindowStyle.Hidden;

						compProc = Process.Start(info);
						compProc.WaitForExit();
						return compProc.ExitCode;
					}

					string blobPath = Path.Combine(currentDir, "blobs.zip");
					if (!File.Exists(blobPath))
					{
						middlemanOutputLog.WriteLine($"ERROR: Could not locate blobs.bin.lz4 at '{blobPath}'");

						ProcessStartInfo info = new ProcessStartInfo();
						info.FileName = Path.Combine(currentDir, "_UnityShaderCompiler.exe");
						info.WorkingDirectory = Directory.GetCurrentDirectory();

						string compilerLogPath = Path.Combine("Logs", $"unityshader-{procId}.txt");
						info.Arguments = $"\"{baseFolderPath}\" \"{logPath}\" {port} -local-ipc-stream={streamName} \"{pluginsPath}\"{(forceLoadPlugins ? " -force-plugins-load" : "")}";
						info.CreateNoWindow = true;
						info.WindowStyle = ProcessWindowStyle.Hidden;

						compProc = Process.Start(info);
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
						using (unityPipeStream = new NamedPipeClientStream($"Unity-{streamName}"))
						{
							using (compilerPipeStream = new NamedPipeServerStream($"Unity-shader-middleman-{procId}"))
							{
								try
								{
									ProcessStartInfo info = new ProcessStartInfo();
									info.FileName = Path.Combine(currentDir, "_UnityShaderCompiler.exe");
									info.WorkingDirectory = Directory.GetCurrentDirectory();

									string compilerLogPath = Path.Combine("Logs", $"unityshader-{procId}.txt");
									info.Arguments = $"\"{baseFolderPath}\" \"{compilerLogPath}\" {port} -local-ipc-stream=shader-middleman-{procId} \"{pluginsPath}\"{(forceLoadPlugins ? " -force-plugins-load" : "")}";
									info.CreateNoWindow = true;
									info.WindowStyle = ProcessWindowStyle.Hidden;

									compProc = Process.Start(info);
									compilerPipeStream.WaitForConnection();

									middlemanOutputLog.WriteLine("Compiler connected!");

									unityPipeStream.Connect(5000);
									if (!unityPipeStream.IsConnected)
									{
										middlemanOutputLog.WriteLine();
										middlemanOutputLog.WriteLine($"Failed to open pipe stream!");
										return 1;
									}

									middlemanOutputLog.WriteLine("Unity connected!");
								}
								catch (Exception e)
								{
									Console.ForegroundColor = ConsoleColor.Red;
									Console.WriteLine($"{e.GetType().Name} : {e.Message}");
									Console.WriteLine(e.StackTrace);

									middlemanOutputLog.WriteLine();
									middlemanOutputLog.WriteLine($"{e.GetType().Name} : {e.Message}");
									middlemanOutputLog.WriteLine(e.StackTrace);

									return 1;
								}

								compProc.EnableRaisingEvents = true;
								compProc.Exited += (o, e) =>
								{
									middlemanOutputLog.WriteLine($"Compiler process exited with code {compProc.ExitCode}");
									middlemanOutputLog.Flush();
									Environment.Exit(compProc.ExitCode);
								};

								InterceptTask();

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

					if (unityPipeStream != null)
						unityPipeStream.Close();

					if (compilerPipeStream != null)
						compilerPipeStream.Close();

					if (compProc != null && !compProc.HasExited)
					{
						compProc.WaitForExit(1000);
					}

					return 1;
				}
			}
		}

		struct Header
		{
			public int first;
			public int second;

			public Header(int first)
			{
				this.first = first;
				this.second = 0;
			}

			public Header(int first, int second)
			{
				this.first = first;
				this.second = second;
			}
		}

		private static byte[] buff = new byte[32768];

		private static void ExpandBuff(int minSize)
		{
			int newCap = buff.Length;
			while (newCap < minSize)
				newCap *= 2;

			if (buff.Length == newCap) return;

			byte[] newBuff = new byte[newCap];
			Array.Copy(buff, 0, newBuff, 0, buff.Length);
			buff = newBuff;
		}

		private static void PrintTransmission(bool compilerToUnity, byte[] buff, int len)
		{
#if VERBOSE
			StringBuilder s = new StringBuilder();
			for (int i = 0; i < len; i++)
			{
				if (i % 16 != 0) s.Append(' ');
				else if (i != 0) s.Append('\n');

				s.Append(ToHex(buff[i]));
			}
			s.Append("\n\n");

			middlemanOutputLog.Write(compilerToUnity ? "Compiler ==> Unity " : "Unity ==> Compiler ");
			middlemanOutputLog.WriteLine($"({len})\n");
			middlemanOutputLog.Write(s.ToString());
#endif
		}

		private static void SendShutdownMessage()
		{
			if (!compilerPipeStream.IsConnected)
				return;

			byte[] header = new byte[12];
			Array.Copy(magic, 0, header, 0, 4);
			Array.Copy(BitConverter.GetBytes((int)"c:shutdown".Length), 0, header, 4, 4);
			Array.Copy(BitConverter.GetBytes((int)0), 0, header, 8, 4);

			compilerPipeStream.Write(header, 0, 12);

			byte[] shutdownBytes = Encoding.UTF8.GetBytes("c:shutdown");

			compilerPipeStream.Write(shutdownBytes, 0, shutdownBytes.Length);
		}

		private static readonly byte[] magic = new byte[] { 0xE4, 0xD1, 0x0B, 0x0C };

		private static byte[] headerBuff = new byte[12];
		private static Header ReadHeader(PipeStream input, PipeStream output, bool readSecond, bool writeToOutput = true)
		{
			if (input == output)
			{
				throw new ArgumentException("input == output");
			}

			int readBytes = 0;
			do
			{
				readBytes += input.Read(headerBuff, readBytes, (readSecond ? 12 : 8) - readBytes);

				if (!input.IsConnected)
				{
					middlemanOutputLog.WriteLine($"{(input == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

					if (input == unityPipeStream)
						SendShutdownMessage();

					compProc.WaitForExit(500);

					if (output.IsConnected)
						output.Close();

					Environment.Exit(1);
				}
			} while (readBytes < (readSecond ? 12 : 8));

			if (!writeToOutput)
				middlemanOutputLog.Write("(SKIPPED) ");

			PrintTransmission(input == compilerPipeStream, headerBuff, readSecond ? 12 : 8);

			if (!Enumerable.SequenceEqual(headerBuff.Take(4), magic))
			{
				throw new IOException("Expected magic bytes, reveived incorrectly!");
			}

			if (writeToOutput)
			{
				output.Write(headerBuff, 0, readSecond ? 12 : 8);
			}

			if (!output.IsConnected)
			{
				middlemanOutputLog.WriteLine($"{(output == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

				if (output == unityPipeStream)
					SendShutdownMessage();

				compProc.WaitForExit(500);

				if (input.IsConnected)
					input.Close();

				Environment.Exit(1);
			}

			return readSecond ? new Header(BitConverter.ToInt32(headerBuff, 4), BitConverter.ToInt32(headerBuff, 8)) : new Header(BitConverter.ToInt32(headerBuff, 4));
		}

		private static Header header = new Header();

		private static int ReadString(PipeStream input, PipeStream output, bool writeToOutput = true)
		{
			if (input == output)
			{
				throw new ArgumentException("input == output");
			}

			header = ReadHeader(input, output, true, writeToOutput);
			if (header.first == 0)
				return 0;

			ExpandBuff(header.first);

			int readBytes = 0;
			do
			{
				readBytes += input.Read(buff, readBytes, header.first - readBytes);

				if (!input.IsConnected)
				{
					middlemanOutputLog.WriteLine($"{(input == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

					if (input == unityPipeStream)
						SendShutdownMessage();

					compProc.WaitForExit(500);

					if (output.IsConnected)
						output.Close();

					Environment.Exit(1);
				}
			} while (readBytes < header.first);

			if (!writeToOutput)
				middlemanOutputLog.Write("(SKIPPED) ");

			PrintTransmission(input == compilerPipeStream, buff, readBytes);

			if (writeToOutput)
			{
				output.Write(buff, 0, readBytes);
			}

			if (!output.IsConnected)
			{
				middlemanOutputLog.WriteLine($"{(output == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

				if (output == unityPipeStream)
					SendShutdownMessage();

				compProc.WaitForExit(500);

				if (input.IsConnected)
					input.Close();

				Environment.Exit(1);
			}

			return readBytes;
		}
		
		private static void WriteHeader(PipeStream output, int first)
		{
			byte[] header = new byte[8];
			Array.Copy(magic, 0, header, 0, 4);
			Array.Copy(BitConverter.GetBytes(first), 0, header, 4, 4);

			output.Write(header, 0, 8);

			if (!output.IsConnected)
			{
				middlemanOutputLog.WriteLine($"{(output == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

				if (output == unityPipeStream)
					SendShutdownMessage();

				compProc.WaitForExit(500);

				if (output == unityPipeStream)
				{
					if (compilerPipeStream.IsConnected)
						compilerPipeStream.Close();
				}
				else
				{
					if (unityPipeStream.IsConnected)
						unityPipeStream.Close();
				}

				Environment.Exit(1);
			}

			PrintTransmission(output == unityPipeStream, header, 8);
		}

		private static void WriteHeader(PipeStream output, int first, int second)
		{
			byte[] header = new byte[12];
			Array.Copy(magic, 0, header, 0, 4);
			Array.Copy(BitConverter.GetBytes(first), 0, header, 4, 4);
			Array.Copy(BitConverter.GetBytes(second), 0, header, 8, 4);

			output.Write(header, 0, 12);

			if (!output.IsConnected)
			{
				middlemanOutputLog.WriteLine($"{(output == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

				if (output == unityPipeStream)
					SendShutdownMessage();

				compProc.WaitForExit(500);

				if (output == unityPipeStream)
				{
					if (compilerPipeStream.IsConnected)
						compilerPipeStream.Close();
				}
				else
				{
					if (unityPipeStream.IsConnected)
						unityPipeStream.Close();
				}

				Environment.Exit(1);
			}

			PrintTransmission(output == unityPipeStream, header, 12);
		}

		private static void Write(PipeStream output, byte[] buff, int offset, int length)
		{
			WriteHeader(output, length, 0);

			output.Write(buff, offset, length);

			if (!output.IsConnected)
			{
				middlemanOutputLog.WriteLine($"{(output == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

				if (output == unityPipeStream)
					SendShutdownMessage();

				compProc.WaitForExit(500);

				if (output == unityPipeStream)
				{
					if (compilerPipeStream.IsConnected)
						compilerPipeStream.Close();
				}
				else
				{
					if (unityPipeStream.IsConnected)
						unityPipeStream.Close();
				}

				Environment.Exit(1);
			}

			PrintTransmission(output == unityPipeStream, buff, length);
		}
		
		private static byte[] writeBuff = new byte[32768];

		private static void Write(PipeStream output, Stream stream, int length)
		{
			WriteHeader(output, length, 0);

			int read;
			while (length > 0 &&
				   (read = stream.Read(writeBuff, 0, Math.Min(writeBuff.Length, length))) > 0)
			{
				output.Write(writeBuff, 0, read);
				length -= read;
			}

			if (!output.IsConnected)
			{
				middlemanOutputLog.WriteLine($"{(output == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

				if (output == unityPipeStream)
					SendShutdownMessage();

				compProc.WaitForExit(500);

				if (output == unityPipeStream)
				{
					if (compilerPipeStream.IsConnected)
						compilerPipeStream.Close();
				}
				else
				{
					if (unityPipeStream.IsConnected)
						unityPipeStream.Close();
				}

				Environment.Exit(1);
			}

			PrintTransmission(output == unityPipeStream, buff, length);
		}

		private static void WriteString(PipeStream output, string str)
		{
			byte[] stringBuff = Encoding.UTF8.GetBytes(str);
			Write(output, stringBuff, 0, stringBuff.Length);
		}

		private static Regex passNameRegex = new Regex(@"^<Unnamed Pass (\d+)>$");

		private static void InterceptTask()
		{
			int readBytes = 0;
			int cnt = 0;

			try
			{
				header = ReadHeader(compilerPipeStream, unityPipeStream, true);

				while (true)
				{
					readBytes = ReadString(unityPipeStream, compilerPipeStream);
					string command = Encoding.UTF8.GetString(buff, 0, readBytes);
					middlemanOutputLog.WriteLine($"Received command: {command}");

					if (command == "c:initializeCompiler")
					{
						// First message
						{
							header = ReadHeader(unityPipeStream, compilerPipeStream, false);
							int subTransmissionCnt = header.first;

							for (int i = 0; i < subTransmissionCnt; i++)
							{
								readBytes = ReadString(unityPipeStream, compilerPipeStream);
								middlemanOutputLog.WriteLine($"Received parameter: {Encoding.UTF8.GetString(buff, 0, readBytes)}");
							}
						}

						// Second message
						{
							header = ReadHeader(unityPipeStream, compilerPipeStream, false);
							int subTransmissionCnt = header.first;

							for (int i = 0; i < subTransmissionCnt * 2; i++)
							{
								readBytes = ReadString(unityPipeStream, compilerPipeStream);
								middlemanOutputLog.WriteLine($"Received parameter: {Encoding.UTF8.GetString(buff, 0, readBytes)}");
							}
						}

						// Terminate
						header = ReadHeader(unityPipeStream, compilerPipeStream, true);

						// Feedback
						header = ReadHeader(compilerPipeStream, unityPipeStream, false);

						for (int messages = 0; messages < 26; messages++)
						{
							header = ReadHeader(compilerPipeStream, unityPipeStream, true);
							header = ReadHeader(compilerPipeStream, unityPipeStream, false);
						}
					}
					else if (command == "c:preprocess")
					{
						// First message (shader file contents)
						readBytes = ReadString(unityPipeStream, compilerPipeStream);
						string shaderCode = Encoding.UTF8.GetString(buff, 0, readBytes);

						middlemanOutputLog.WriteLine("Shader code:");
						middlemanOutputLog.WriteLine(shaderCode);
						middlemanOutputLog.WriteLine();

						// Second message (shader file directory)
						middlemanOutputLog.WriteLine("preprocess: Shader file directory");
						readBytes = ReadString(unityPipeStream, compilerPipeStream);

						// Third message (shader file name)
						middlemanOutputLog.WriteLine("preprocess: Shader file name");
						readBytes = ReadString(unityPipeStream, compilerPipeStream);

						header = ReadHeader(unityPipeStream, compilerPipeStream, false); // surface only
						header = ReadHeader(unityPipeStream, compilerPipeStream, false); // build platform
						header = ReadHeader(unityPipeStream, compilerPipeStream, false); // valid APIs

						// Fourth message (p keywords)
						{
							middlemanOutputLog.WriteLine("preprocess: p keywords");
							header = ReadHeader(unityPipeStream, compilerPipeStream, false);
							cnt = header.first;

							for (int keywords = 0; keywords < cnt; keywords++)
							{
								readBytes = ReadString(unityPipeStream, compilerPipeStream);
							}
						}

						// Fifth message (d keywords)
						{
							middlemanOutputLog.WriteLine("preprocess: d keywords");
							header = ReadHeader(unityPipeStream, compilerPipeStream, false);
							cnt = header.first;

							for (int keywords = 0; keywords < cnt; keywords++)
							{
								readBytes = ReadString(unityPipeStream, compilerPipeStream);
							}
						}

						// Feedback
						{
							while (true)
							{
								readBytes = ReadString(compilerPipeStream, unityPipeStream);
								string line = Encoding.UTF8.GetString(buff, 0, readBytes);
								middlemanOutputLog.WriteLine(line);

								if (line.StartsWith("shader:"))
									break;

								if (line.StartsWith("keywordsEnd:"))
								{
									readBytes = ReadString(compilerPipeStream, unityPipeStream, true);
									readBytes = ReadString(compilerPipeStream, unityPipeStream, true);
									readBytes = ReadString(compilerPipeStream, unityPipeStream, true);
									ReadHeader(compilerPipeStream, unityPipeStream, true);

									middlemanOutputLog.WriteLine("preprocess: Other keywords");
									header = ReadHeader(compilerPipeStream, unityPipeStream, false);
									cnt = header.first;
							
									for (int i = 0; i < cnt; i++)
									{
										readBytes = ReadString(compilerPipeStream, unityPipeStream);
										middlemanOutputLog.WriteLine(Encoding.UTF8.GetString(buff, 0, readBytes));
										ReadHeader(compilerPipeStream, unityPipeStream, true);
									}

									continue;
								}
							}

							// processed shader code
							middlemanOutputLog.WriteLine("preprocess: Processed shader code");
							readBytes = ReadString(compilerPipeStream, unityPipeStream);
						}
					}
					else if (command == "c:compileSnippet")
					{
						// Processed shader code
						middlemanOutputLog.WriteLine("compileSnippet: Shader code");
						readBytes = ReadString(unityPipeStream, compilerPipeStream);

						// Shader file directory
						middlemanOutputLog.WriteLine("compileSnippet: Shader directory");
						readBytes = ReadString(unityPipeStream, compilerPipeStream);
						string shaderDir = Encoding.UTF8.GetString(buff, 0, readBytes);

						// Shader file name
						middlemanOutputLog.WriteLine("compileSnippet: Shader file");
						readBytes = ReadString(unityPipeStream, compilerPipeStream);
						string shaderFileName = Encoding.UTF8.GetString(buff, 0, readBytes);

						// Get guid from the file
						string metaPath = Path.Combine(shaderDir, shaderFileName + ".meta");
						string guid = null;

						if (File.Exists(metaPath))
						{
							string metaContens;
							using (StreamReader reader = new StreamReader(File.Open(metaPath, FileMode.Open, FileAccess.Read, FileShare.Read)))
								metaContens = reader.ReadToEnd();

							int guidIndex = metaContens.IndexOf("guid: ");
							if (guidIndex != -1)
							{
								guid = metaContens.Substring(guidIndex + "guid: ".Length, 32);
								middlemanOutputLog.WriteLine($"compileSnippet: Derived shader guid = '{guid}'");
							}
						}
						else
						{
							middlemanOutputLog.WriteLine($"compileSnippet: Could not locate meta file at '{metaPath}'");
						}

						// Pass name
						middlemanOutputLog.WriteLine("compileSnippet: Shader pass");
						readBytes = ReadString(unityPipeStream, compilerPipeStream);
						string passName = Encoding.UTF8.GetString(buff, 0, readBytes);

						middlemanOutputLog.WriteLine("compileSnippet: 4 unknown");
						ReadHeader(unityPipeStream, compilerPipeStream, false);
						ReadHeader(unityPipeStream, compilerPipeStream, false);
						ReadHeader(unityPipeStream, compilerPipeStream, false);
						ReadHeader(unityPipeStream, compilerPipeStream, false);

						// ??? Keywords
						middlemanOutputLog.WriteLine("compileSnippet: Unknown keyword array");
						header = ReadHeader(unityPipeStream, compilerPipeStream, false);
						cnt = header.first;

						for (int i = 0; i < cnt; i++)
						{
							readBytes = ReadString(unityPipeStream, compilerPipeStream);
						}

						// Enabled shader keywords
						middlemanOutputLog.WriteLine("compileSnippet: Enabled keywords");
						header = ReadHeader(unityPipeStream, compilerPipeStream, false);
						cnt = header.first;

						List<string> enabledKeywords = new List<string>();
						for (int i = 0; i < cnt; i++)
						{
							readBytes = ReadString(unityPipeStream, compilerPipeStream);
							enabledKeywords.Add(Encoding.UTF8.GetString(buff, 0, readBytes));
						}

						middlemanOutputLog.WriteLine($"compileSnippet: Enabled keywords = {string.Join(" ", enabledKeywords)}");

						// Disabled shader keywords
						middlemanOutputLog.WriteLine("compileSnippet: Disabled keywords");
						header = ReadHeader(unityPipeStream, compilerPipeStream, false);
						cnt = header.first;

						List<string> disabledKeywords = new List<string>();
						for (int i = 0; i < cnt; i++)
						{
							readBytes = ReadString(unityPipeStream, compilerPipeStream);
							disabledKeywords.Add(Encoding.UTF8.GetString(buff, 0, readBytes));
						}

						middlemanOutputLog.WriteLine($"compileSnippet: Disabled keywords = {string.Join(" ", disabledKeywords)}");

						middlemanOutputLog.WriteLine("compileSnippet: reading compiler output");
						ReadHeader(unityPipeStream, compilerPipeStream, false);
						
						// flags=0
						ReadHeader(unityPipeStream, compilerPipeStream, true);
						// lang=0
						ReadHeader(unityPipeStream, compilerPipeStream, false);
						// type=Vertex|Fragment
						header = ReadHeader(unityPipeStream, compilerPipeStream, false);
						bool vertexShader = header.first == 0;
						middlemanOutputLog.WriteLine($"Shader type: {(vertexShader ? "Vertex" : "Fragment")}");
						// platform=BuildStandaloneWin64Player (19)
						ReadHeader(unityPipeStream, compilerPipeStream, false);
						// reqs=4075 (?)
						ReadHeader(unityPipeStream, compilerPipeStream, true);
						// mask=6
						ReadHeader(unityPipeStream, compilerPipeStream, false);
						// start=194
						ReadHeader(unityPipeStream, compilerPipeStream, false);

						bool shaderReplaced = false;

						// Injection
						Match passNameMatch = passNameRegex.Match(passName);
						if (!passNameMatch.Success)
							middlemanOutputLog.WriteLine($"Invalid pass name: {passName}");

						if (passNameMatch.Success && guid != null && shaders.TryGetValue(guid, out ShaderEntry entry))
						{
							int passNum = int.Parse(passNameMatch.Groups[1].Value);
							ShaderPass passEntry = entry.shaderPasses.Where(p => p.passNum == passNum).FirstOrDefault();

							middlemanOutputLog.WriteLine($"Shader found in the table. Attempting to find suitable variant");

							if (passEntry == null)
							{
								middlemanOutputLog.WriteLine($"Shader pass not found");
							}
							else
							{
								long enabledBitmask = 0;

								foreach (string enabledKeyword in enabledKeywords)
								{
									int keywordIdx = entry.shaderKeywords.IndexOf(enabledKeyword);
									if (keywordIdx == -1)
										continue;

									enabledBitmask |= (long)1 << keywordIdx;
								}

								VariantEntry matchedVariant = null;

								if (vertexShader)
								{
									enabledBitmask = enabledBitmask & passEntry.vertexKeywordMask;
									matchedVariant = passEntry.vertexVariants.Where(v => (v.keywords & enabledBitmask) == enabledBitmask).FirstOrDefault();
									if (matchedVariant == null)
									{
										matchedVariant = passEntry.vertexVariants.Last();
										middlemanOutputLog.WriteLine($"Fallback variant");
									}
								}
								else
								{
									enabledBitmask = enabledBitmask & passEntry.fragmentKeywordMask;
									matchedVariant = passEntry.fragmentVariants.Where(v => (v.keywords & enabledBitmask) == enabledBitmask).FirstOrDefault();
									if (matchedVariant == null)
									{
										matchedVariant = passEntry.fragmentVariants.Last();
										middlemanOutputLog.WriteLine($"Fallback variant");
									}
								}

								ZipArchiveEntry blob = blobs.GetEntry($"{guid}/{matchedVariant.offset}");
								if (blob == null)
								{
									middlemanOutputLog.WriteLine("Could not locate blob entry");
								}
								else
								{
									// WRITE BINDINGS
									foreach (var binding in matchedVariant.inputBindings.OrderBy(b => b.Source))
									{
										WriteString(unityPipeStream, $"input: {binding.Source} {(int)binding.Target}");
										middlemanOutputLog.WriteLine($"Binding = input: {binding.Source} {(int)binding.Target}");
									}

									ShaderParameters commonParams = vertexShader ? passEntry.vertexCommonParameters : passEntry.fragmentCommonParameters;

									// Variant parameters

									IEnumerable<ConstantBuffer> buffs = (matchedVariant.parameters.BaseConstantBuffer == null || string.IsNullOrEmpty(matchedVariant.parameters.BaseConstantBuffer.Name))
										? matchedVariant.parameters.ConstantBuffers
										: new ConstantBuffer[] { matchedVariant.parameters.BaseConstantBuffer }.Concat(matchedVariant.parameters.ConstantBuffers);

									foreach (var cb in buffs)
									{
										var partialCb = commonParams.ConstantBuffers.Where(c => c.Partial && c.Name == cb.Name).FirstOrDefault();

										List<ConstantBufferParameter> parameters = new List<ConstantBufferParameter>(cb.CBParams);
										if (partialCb != null)
											parameters.AddRange(partialCb.CBParams);

										WriteString(unityPipeStream, $"cb: {cb.Name} {cb.UsedSize} {parameters.Count}");
										middlemanOutputLog.WriteLine($"Binding = cb: {cb.Name} {cb.UsedSize} {parameters.Count}");

										foreach (var param in parameters.OrderBy(p => p.Index))
										{
											WriteString(unityPipeStream, $"const: {param.ParamName} {param.Index} 0 {(param.IsMatrix ? 1 : 0)} {param.Rows} {param.Columns} {param.ArraySize}");
											middlemanOutputLog.WriteLine($"Binding = const: {param.ParamName} {param.Index} 0 {(param.IsMatrix ? 1 : 0)} {param.Rows} {param.Columns} {param.ArraySize}");
										}

										middlemanOutputLog.WriteLine($"Struct param count: {cb.StructParams.Count} : {(partialCb == null ? -1 : partialCb.StructParams.Count)}");
									}

									List<ConstantBufferBinding> cbBindings = new List<ConstantBufferBinding>(matchedVariant.parameters.ConstBindings);
									cbBindings.AddRange(commonParams.ConstBindings.Where(binding => !cbBindings.Any(b => b.Name == binding.Name)).ToArray());

									foreach (var cbBinding in cbBindings.OrderBy(b => b.Index))
									{
										WriteString(unityPipeStream, $"cbbind: {cbBinding.Name} {cbBinding.Index}");
										middlemanOutputLog.WriteLine($"Binding = cbbind: {cbBinding.Name} {cbBinding.Index}");
									}

									List<TextureParameter> textures = new List<TextureParameter>(matchedVariant.parameters.TextureParameters);
									textures.AddRange(commonParams.TextureParameters.Where(texture => !textures.Any(t => t.Name == texture.Name)));

									foreach (var tex in textures.OrderBy(t => t.Index))
									{
										WriteString(unityPipeStream, $"texbind: {tex.Name} {tex.Index} {tex.SamplerIndex} {(tex.MultiSampled ? 1 : 0)} {tex.Dim}");
										middlemanOutputLog.WriteLine($"Binding = texbind: {tex.Name} {tex.Index} {tex.SamplerIndex} {(tex.MultiSampled ? 1 : 0)} {tex.Dim}");
									}

									List<ConstantBufferBinding> buffers = new List<ConstantBufferBinding>(matchedVariant.parameters.Buffers);
									buffers.AddRange(commonParams.Buffers.Where(buff => !buffers.Any(b => b.Name == buff.Name)));

									foreach (var buff in buffers.OrderBy(buff => buff.Index))
									{
										WriteString(unityPipeStream, $"bufferbind: {buff.Name} {buff.Index} {buff.ArraySize}");
										middlemanOutputLog.WriteLine($"Binding = bufferbind: {buff.Name} {buff.Index} {buff.ArraySize}");
									}

									List<SamplerParameter> samplers = new List<SamplerParameter>(matchedVariant.parameters.Samplers);
									samplers.AddRange(commonParams.Samplers.Where(samp => !samplers.Any(s => s.Sampler == samp.Sampler)));

									foreach (var sampler in samplers.OrderBy(s => s.BindPoint))
									{
										WriteString(unityPipeStream, $"sampler: {sampler.Sampler} {sampler.BindPoint}");
										middlemanOutputLog.WriteLine($"Binding = sampler: {sampler.Sampler} {sampler.BindPoint}");
									}

									WriteString(unityPipeStream, $"stats: {matchedVariant.statsAlu} {matchedVariant.statsTex} {matchedVariant.statsFlow} {matchedVariant.statsTempRegister}");
									middlemanOutputLog.WriteLine($"Binding = stats: {matchedVariant.statsAlu} {matchedVariant.statsTex} {matchedVariant.statsFlow} {matchedVariant.statsTempRegister}");
									// END BINDINGS


									// IGNORE COMPILER INPUT BEGIN
									while (true)
									{
										readBytes = ReadString(compilerPipeStream, unityPipeStream, false);
										string binding = Encoding.UTF8.GetString(buff, 0, readBytes);
										middlemanOutputLog.WriteLine($"(SKIP) compileSnippet: Binding = '{binding}'");

										if (Encoding.UTF8.GetString(buff, 0, readBytes).StartsWith("shader: "))
										{
											break;
										}
									}

									header = ReadHeader(compilerPipeStream, unityPipeStream, false, false);

									middlemanOutputLog.WriteLine("(SKIP) compileSnippet: Shader bytecode");
									readBytes = ReadString(compilerPipeStream, unityPipeStream, false);
									// IGNORE COMPILER INPUT END

									// Inject shader
									WriteString(unityPipeStream, "shader: 1");
									WriteHeader(unityPipeStream, matchedVariant.type);

									using (Stream blobStream = blob.Open())
									{
										Write(unityPipeStream, blobStream, matchedVariant.length);
									}

									//byte[] bytecode = new byte[matchedVariant.length];
									//blobs.Seek(matchedVariant.offset, SeekOrigin.Begin);
									//blobs.Read(bytecode, 0, matchedVariant.length);

									//Write(unityPipeStream, bytecode, 0, matchedVariant.length);

									shaderReplaced = true;
								}
							}
						}

						// Response in case no injection was done
						if (!shaderReplaced)
						{
							/*
							// input: 0 0
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// input: 1 1
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// input: 2 -1
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// input: 3 3
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// input: 4 5
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// input: 6 -1
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// input: 8 -1
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// cb: $Globals 368 48
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: _MainTex_ST 64 0 0 1 4 0
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: _Color 96 0 0 1 4 0
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: _VertexColors 240 0 0 1 1 0
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: _TextureWarping 308 0 0 1 1 0
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// cb: UnityPerCamera 144 9
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: _WorldSpaceCameraPos 64 0 0 1 3 0
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// cb: UnityLighting 768 20
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: unity_LightColor 112 0 0 1 4 8
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: unity_LightPosition 240 0 0 1 4 8
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: const: unity_LightAtten 368 0 0 1 4 8
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: unity_SpotDirection 496 0 0 1 4 8
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// cb: UnityPerDraw 176 5
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: unity_ObjectToWorld 0 0 1 4 4 0
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: unity_WorldToObject 64 0 1 4 4 0
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// cb: UnityPerFrame 368 11
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: glstate_lightmodel_ambient 0 0 0 1 4 0
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: unity_MatrixV 144 0 1 4 4 0
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: unity_MatrixInvV 208 0 1 4 4 0
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// const: unity_MatrixVP 272 0 1 4 4 0
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// cbbind: $Globals 0
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// cbbind: UnityPerCamera 1
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// cbbind: UnityLighting 2
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// cbbind: UnityPerDraw 3
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// cbbind: UnityPerFrame 4
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// stats: 80 0 1 9
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// err: 1 4 490
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// Assets/Shaders/MasterShader/ULTRAKILL-Standard.shader
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// Output value 'vert' is not completely initialized
							readBytes = ReadString(compilerPipeStream, unityPipeStream);

							// shader: 1
							readBytes = ReadString(compilerPipeStream, unityPipeStream);
							*/

							while (true)
							{
								readBytes = ReadString(compilerPipeStream, unityPipeStream);
								string binding = Encoding.UTF8.GetString(buff, 0, readBytes);
								middlemanOutputLog.WriteLine($"compileSnippet:  Binding = '{binding}'");

								if (Encoding.UTF8.GetString(buff, 0, readBytes).StartsWith("shader: "))
								{
									break;
								}
							}

							header = ReadHeader(compilerPipeStream, unityPipeStream, false);

							middlemanOutputLog.WriteLine("compileSnippet: Shader bytecode");
							readBytes = ReadString(compilerPipeStream, unityPipeStream);
						}
					}
					else if (command == "c:preprocessCompute")
					{
						// Shader source code
						readBytes = ReadString(unityPipeStream, compilerPipeStream);

						// File name
						readBytes = ReadString(unityPipeStream, compilerPipeStream);

						ReadHeader(unityPipeStream, compilerPipeStream, false);
						ReadHeader(unityPipeStream, compilerPipeStream, false);

						header = ReadHeader(unityPipeStream, compilerPipeStream, false);
						cnt = header.first;
						for (int i = 0; i < cnt; i++)
						{
							ReadString(unityPipeStream, compilerPipeStream);
						}

						header = ReadHeader(unityPipeStream, compilerPipeStream, false);
						cnt = header.first;
						for (int i = 0; i < cnt; i++)
						{
							ReadString(unityPipeStream, compilerPipeStream);
						}

						while (true)
						{
							readBytes = ReadString(compilerPipeStream, unityPipeStream);
							string line = Encoding.UTF8.GetString(buff, 0, readBytes);
							middlemanOutputLog.WriteLine(line);

							if (line.StartsWith("endKernels:"))
							{
								// Read processed code
								ReadString(compilerPipeStream, unityPipeStream);

								ReadHeader(compilerPipeStream, unityPipeStream, false);
								ReadHeader(compilerPipeStream, unityPipeStream, false);
								ReadHeader(compilerPipeStream, unityPipeStream, false);

								break;
							}

							if (line.StartsWith("requirements:"))
							{
								ReadHeader(compilerPipeStream, unityPipeStream, true);
								
								header = ReadHeader(compilerPipeStream, unityPipeStream, false);
								cnt = header.first;
								for (int i = 0; i < cnt; i++)
								{
									ReadString(compilerPipeStream, unityPipeStream);
									ReadHeader(compilerPipeStream, unityPipeStream, true);
								}
							}
						}
					}
					else if (command == "c:compileComputeKernel")
					{
						// Source code
						ReadString(unityPipeStream, compilerPipeStream);

						// File name
						ReadString(unityPipeStream, compilerPipeStream);

						// Main method name
						ReadString(unityPipeStream, compilerPipeStream);

						ReadHeader(unityPipeStream, compilerPipeStream, false);
						ReadHeader(unityPipeStream, compilerPipeStream, false);
						ReadHeader(unityPipeStream, compilerPipeStream, false);

						header = ReadHeader(unityPipeStream, compilerPipeStream, false);
						cnt = header.first;
						for (int i = 0; i < cnt; i++)
						{
							ReadString(unityPipeStream, compilerPipeStream);
							ReadString(unityPipeStream, compilerPipeStream);
						}

						header = ReadHeader(unityPipeStream, compilerPipeStream, false);
						cnt = header.first;
						for (int i = 0; i < cnt; i++)
						{
							ReadString(unityPipeStream, compilerPipeStream);
						}

						header = ReadHeader(unityPipeStream, compilerPipeStream, false);
						cnt = header.first;
						for (int i = 0; i < cnt; i++)
						{
							ReadString(unityPipeStream, compilerPipeStream);
						}

						ReadHeader(unityPipeStream, compilerPipeStream, false);
						ReadHeader(unityPipeStream, compilerPipeStream, true);
						ReadHeader(unityPipeStream, compilerPipeStream, true);
						ReadHeader(unityPipeStream, compilerPipeStream, false);
						ReadHeader(unityPipeStream, compilerPipeStream, false);

						while (true)
						{
							readBytes = ReadString(compilerPipeStream, unityPipeStream);
							string line = Encoding.UTF8.GetString(buff, 0, readBytes);
							middlemanOutputLog.WriteLine(line);

							if (line.StartsWith("computeData:"))
								break;
						}

						ReadString(compilerPipeStream, unityPipeStream);
					}
					else if (command == "c:disassembleShader")
					{
						// Shader name
						ReadString(unityPipeStream, compilerPipeStream);

						ReadHeader(unityPipeStream, compilerPipeStream, false);
						ReadHeader(unityPipeStream, compilerPipeStream, false);
						ReadHeader(unityPipeStream, compilerPipeStream, false);

						// Shader binary
						ReadString(unityPipeStream, compilerPipeStream);

						while (true)
						{
							readBytes = ReadString(compilerPipeStream, unityPipeStream);
							string line = Encoding.UTF8.GetString(buff, 0, readBytes);
							middlemanOutputLog.WriteLine(line);

							if (line.StartsWith("disasm:"))
								break;
						}

						ReadString(compilerPipeStream, unityPipeStream);
					}
					else if (command == "c:shutdown")
					{
						middlemanOutputLog.WriteLine($"Shutdown.");
						compProc.WaitForExit(5000);
						compilerPipeStream.Close();
						unityPipeStream.Close();
						middlemanOutputLog.Flush();
						Environment.Exit(0);
					}
					else
					{
						middlemanOutputLog.WriteLine($"Unknown command: {command}");
						throw new Exception($"Unknown command: {command}");
					}
				}
			}
			catch (Exception e)
			{
				middlemanOutputLog.WriteLine();
				middlemanOutputLog.WriteLine($"{e.GetType().Name} : {e.Message}");
				middlemanOutputLog.WriteLine(e.StackTrace);
				middlemanOutputLog.Flush();

				if (!compProc.HasExited)
					compProc.Kill();

				Environment.Exit(1);
			}
		}
	}
}
