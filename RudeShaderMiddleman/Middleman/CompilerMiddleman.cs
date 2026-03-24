using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Text;

namespace RudeShaderMiddleman.Middleman
{
	internal partial class CompilerMiddleman
	{
		private readonly NamedPipeClientStream unityPipeStream;
		private readonly NamedPipeServerStream compilerPipeStream;
		private readonly StreamWriter middlemanOutputLog;

		private readonly Dictionary<string, ShaderEntry> shaders;
		private readonly ZipArchive blobs;

		enum LogLevel
		{
			DEBUG,
			INFO,
		}

		private string logPrefix = "";
		private void Log(string text, LogLevel logLevel = LogLevel.INFO)
		{
			switch(logLevel)
			{
				case LogLevel.DEBUG:
#if DEBUG
					middlemanOutputLog.Write(logPrefix);
					middlemanOutputLog.WriteLine(text);
#endif
					break;

				case LogLevel.INFO:
					middlemanOutputLog.Write(logPrefix);
					middlemanOutputLog.WriteLine(text);
					break;
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

		public CompilerMiddleman(
			NamedPipeClientStream unityPipeStream,
			NamedPipeServerStream compilerPipeStream,
			StreamWriter middlemanOutputLog,
			Dictionary<string, ShaderEntry> shaders,
			ZipArchive blobs
		)
		{
			this.unityPipeStream = unityPipeStream;
			this.compilerPipeStream = compilerPipeStream;
			this.middlemanOutputLog = middlemanOutputLog;
			this.shaders = shaders;
			this.blobs = blobs;

			if (middlemanOutputLog == null)
				throw new ArgumentNullException("Log stream is null");

			if (unityPipeStream == null)
				throw new ArgumentNullException("Unity pipe stream is null");

			if (compilerPipeStream == null)
				throw new ArgumentNullException("Pipe compiler stream is null");

			if (!unityPipeStream.IsConnected)
				throw new ArgumentException("Unity pipe stream is not connected");

			if (!compilerPipeStream.IsConnected)
				throw new ArgumentException("Compiler pipe stream is not connected");

			if (shaders == null)
				throw new ArgumentException("Shader map is null");

			if (blobs == null)
				throw new ArgumentException("Blob archive is null");
		}

		public void Start()
		{
			try
			{
				Header header = ReadHeader(compilerPipeStream, unityPipeStream, true);

				while (true)
				{
					int readBytes = ReadString(unityPipeStream, compilerPipeStream);
					string command = Encoding.UTF8.GetString(buff, 0, readBytes);
					middlemanOutputLog.WriteLine($"Received command: {command}");

					switch (command)
					{
						case "c:initializeCompiler":
							logPrefix = "    initializeCompiler: ";
							InitializeCompiler();
							break;

						case "c:preprocess":
							logPrefix = "    preprocess: ";
							Preprocess();
							break;

						case "c:compileSnippet":
							logPrefix = "    compileSnippet: ";
							CompileSnippet();
							break;

						case "c:preprocessCompute":
							logPrefix = "    preprocessCompute: ";
							PreprocessCompute();
							break;

						case "c:compileComputeKernel":
							logPrefix = "    compileComputeKernel: ";
							CompilerComputeKernelCommand();
							break;

						case "c:disassembleShader":
							logPrefix = "    disassembleShader: ";
							DisassembleShader();
							break;

						case "c:shutdown":
							logPrefix = "    shutdown: ";
							Shutdown();
							break;

						default:
							middlemanOutputLog.WriteLine($"Unknown command: {command}");
							throw new Exception($"Unknown command: {command}");
					}

					logPrefix = "";
				}
			}
			catch (Exception e)
			{
				middlemanOutputLog.WriteLine();
				middlemanOutputLog.WriteLine($"{e.GetType().Name} : {e.Message}");
				middlemanOutputLog.WriteLine(e.StackTrace);
				middlemanOutputLog.Flush();

				if (unityPipeStream.IsConnected)
					unityPipeStream.Close();
				if (compilerPipeStream.IsConnected)
					compilerPipeStream.Close();

				Environment.Exit(1);
			}
		}
	}
}
