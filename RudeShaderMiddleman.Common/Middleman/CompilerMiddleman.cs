using RudeShaderMiddleman.Common.BlobTable;
using RudeShaderMiddleman.Common.ShaderTable;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RudeShaderMiddleman.Common.Middleman
{
	public partial class CompilerMiddleman
	{
		private readonly Stream unityPipeStream;
		private readonly Stream compilerPipeStream;
		private readonly StreamWriter middlemanOutputLog;

		private readonly Func<bool> unityPipeStreamConnected;
		private readonly Func<bool> compilerPipeStreamConnected;

		private readonly ShaderTableFile shaderTable;
		private readonly BlobTableFile blobs;

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
			Stream unityPipeStream,
			Func<bool> unityPipeStreamConnected,
			Stream compilerPipeStream,
			Func<bool> compilerPipeStreamConnected,
			StreamWriter middlemanOutputLog,
			ShaderTableFile shaderTableFile,
			BlobTableFile blobs
		)
		{
			this.unityPipeStream = unityPipeStream;
			this.unityPipeStreamConnected = unityPipeStreamConnected;
			this.compilerPipeStream = compilerPipeStream;
			this.compilerPipeStreamConnected = compilerPipeStreamConnected;
			this.middlemanOutputLog = middlemanOutputLog;
			this.shaderTable = shaderTableFile;
			this.blobs = blobs;

			if (middlemanOutputLog == null)
				throw new ArgumentNullException("Log stream is null");

			if (unityPipeStream == null)
				throw new ArgumentNullException("Unity pipe stream is null");

			if (unityPipeStreamConnected == null)
				throw new ArgumentNullException("Unity pipe stream connected function is null");

			if (compilerPipeStream == null)
				throw new ArgumentNullException("Pipe compiler stream is null");

			if (compilerPipeStreamConnected == null)
				throw new ArgumentNullException("Compiler pipe stream connected function is null");

			if (!unityPipeStreamConnected())
				throw new ArgumentException("Unity pipe stream is not connected");

			if (!compilerPipeStreamConnected())
				throw new ArgumentException("Compiler pipe stream is not connected");

			if (shaderTableFile == null)
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

				if (unityPipeStreamConnected())
					unityPipeStream.Close();
				if (compilerPipeStreamConnected())
					compilerPipeStream.Close();

				Environment.Exit(1);
			}
		}
	}
}
