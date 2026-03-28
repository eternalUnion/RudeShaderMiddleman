using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace RudeShaderMiddleman.Common.Middleman
{
	public partial class CompilerMiddleman
	{
		// Protocol header
		private static readonly byte[] magic = new byte[] { 0xE4, 0xD1, 0x0B, 0x0C };

		// Read/write buffer
		private byte[] buff = new byte[32768];

		private void ExpandBuff(int minSize)
		{
			int newCap = buff.Length;
			while (newCap < minSize)
				newCap *= 2;

			if (buff.Length == newCap) return;

			byte[] newBuff = new byte[newCap];
			Array.Copy(buff, 0, newBuff, 0, buff.Length);
			buff = newBuff;
		}

		static char[] hexChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
		private static string ToHex(byte b)
		{
			return $"{hexChars[b / 16]}{hexChars[b % 16]}";
		}

		private void PrintTransmission(bool compilerToUnity, byte[] buff, int len, bool withPrefix = true, bool complete = true)
		{
#if DEBUG
			StringBuilder s = new StringBuilder();
			for (int i = 0; i < len; i++)
			{
				if (i % 16 != 0) s.Append(' ');
				else if (i != 0) s.Append('\n');

				s.Append(ToHex(buff[i]));
			}

			if (complete) s.Append("\n\n");
			else s.Append("\n");

			if (withPrefix)
			{
				middlemanOutputLog.Write(compilerToUnity ? "Compiler ==> Unity " : "Unity ==> Compiler ");
				if (complete) middlemanOutputLog.WriteLine($"({len})\n");
				else middlemanOutputLog.WriteLine($"({len}) [PARTIAL]\n");
			}

			middlemanOutputLog.Write(s.ToString());
#endif
		}

		#region Read
		private byte[] headerBuff = new byte[12];
		private Header ReadHeader(Stream input, Stream output, bool readSecond, bool writeToOutput = true)
		{
			if (input == output)
			{
				throw new ArgumentException("input == output");
			}

			var inputConnected = (input == unityPipeStream) ? unityPipeStreamConnected : compilerPipeStreamConnected;
			var outputConnected = (output == unityPipeStream) ? unityPipeStreamConnected : compilerPipeStreamConnected;

			int readBytes = 0;
			do
			{
				readBytes += input.Read(headerBuff, readBytes, (readSecond ? 12 : 8) - readBytes);

				if (!inputConnected())
				{
					middlemanOutputLog.WriteLine($"{(input == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

					if (output == unityPipeStream)
						SendShutdownMessage();

					compilerPipeStream.Dispose();
					unityPipeStream.Dispose();

					Environment.Exit(1);
				}
			} while (readBytes < (readSecond ? 12 : 8));

#if DEBUG
			if (!writeToOutput)
				middlemanOutputLog.Write("(SKIPPED) ");
#endif

			PrintTransmission(input == compilerPipeStream, headerBuff, readSecond ? 12 : 8);

			if (!Enumerable.SequenceEqual(headerBuff.Take(4), magic))
			{
				throw new IOException("Expected magic bytes, reveived incorrectly!");
			}

			if (writeToOutput)
			{
				output.Write(headerBuff, 0, readSecond ? 12 : 8);
			}

			if (!outputConnected())
			{
				middlemanOutputLog.WriteLine($"{(output == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

				if (output == unityPipeStream)
					SendShutdownMessage();

				compilerPipeStream.Dispose();
				unityPipeStream.Dispose();

				Environment.Exit(1);
			}

			return readSecond ? new Header(BitConverter.ToInt32(headerBuff, 4), BitConverter.ToInt32(headerBuff, 8)) : new Header(BitConverter.ToInt32(headerBuff, 4));
		}

		private int ReadString(Stream input, Stream output, bool writeToOutput = true)
		{
			if (input == output)
			{
				throw new ArgumentException("input == output");
			}

			var inputConnected = (input == unityPipeStream) ? unityPipeStreamConnected : compilerPipeStreamConnected;
			var outputConnected = (output == unityPipeStream) ? unityPipeStreamConnected : compilerPipeStreamConnected;

			Header header = ReadHeader(input, output, true, writeToOutput);
			if (header.first == 0)
				return 0;

			ExpandBuff(header.first);

			int readBytes = 0;
			do
			{
				readBytes += input.Read(buff, readBytes, header.first - readBytes);

				if (!inputConnected())
				{
					middlemanOutputLog.WriteLine($"{(input == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

					if (output == unityPipeStream)
						SendShutdownMessage();

					compilerPipeStream.Dispose();
					unityPipeStream.Dispose();

					Environment.Exit(1);
				}
			} while (readBytes < header.first);

#if DEBUG
			if (!writeToOutput)
				middlemanOutputLog.Write("(SKIPPED) ");
#endif

			PrintTransmission(input == compilerPipeStream, buff, readBytes);

			if (writeToOutput)
			{
				output.Write(buff, 0, readBytes);
			}

			if (!outputConnected())
			{
				middlemanOutputLog.WriteLine($"{(output == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

				if (output == unityPipeStream)
					SendShutdownMessage();

				compilerPipeStream.Dispose();
				unityPipeStream.Dispose();

				Environment.Exit(1);
			}

			return readBytes;
		}
#endregion



		#region Write
		private void WriteHeader(Stream output, int first)
		{
			byte[] header = new byte[8];
			Array.Copy(magic, 0, header, 0, 4);
			Array.Copy(BitConverter.GetBytes(first), 0, header, 4, 4);

			var outputConnected = (output == unityPipeStream) ? unityPipeStreamConnected : compilerPipeStreamConnected;

			output.Write(header, 0, 8);

			if (!outputConnected())
			{
				middlemanOutputLog.WriteLine($"{(output == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

				if (output == unityPipeStream)
					SendShutdownMessage();

				compilerPipeStream.Dispose();
				unityPipeStream.Dispose();

				Environment.Exit(1);
			}

			PrintTransmission(output == unityPipeStream, header, 8);
		}

		private void WriteHeader(Stream output, int first, int second)
		{
			byte[] header = new byte[12];
			Array.Copy(magic, 0, header, 0, 4);
			Array.Copy(BitConverter.GetBytes(first), 0, header, 4, 4);
			Array.Copy(BitConverter.GetBytes(second), 0, header, 8, 4);

			var outputConnected = (output == unityPipeStream) ? unityPipeStreamConnected : compilerPipeStreamConnected;

			output.Write(header, 0, 12);

			if (!outputConnected())
			{
				middlemanOutputLog.WriteLine($"{(output == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

				if (output == unityPipeStream)
					SendShutdownMessage();

				compilerPipeStream.Dispose();
				unityPipeStream.Dispose();

				Environment.Exit(1);
			}

			PrintTransmission(output == unityPipeStream, header, 12);
		}
		
		private void Write(Stream output, byte[] buff, int offset, int length)
		{
			WriteHeader(output, length, 0);

			var outputConnected = (output == unityPipeStream) ? unityPipeStreamConnected : compilerPipeStreamConnected;

			output.Write(buff, offset, length);

			if (!outputConnected())
			{
				middlemanOutputLog.WriteLine($"{(output == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

				if (output == unityPipeStream)
					SendShutdownMessage();

				compilerPipeStream.Dispose();
				unityPipeStream.Dispose();

				Environment.Exit(1);
			}

			PrintTransmission(output == unityPipeStream, buff, length);
		}

		private byte[] writeBuff = new byte[32768];

		private void Write(Stream output, Stream stream, int length)
		{
			WriteHeader(output, length, 0);

			var outputConnected = (output == unityPipeStream) ? unityPipeStreamConnected : compilerPipeStreamConnected;

			int read;
			bool prefix = true;
			while (length > 0 &&
				   (read = stream.Read(writeBuff, 0, Math.Min(writeBuff.Length, length))) > 0)
			{
				output.Write(writeBuff, 0, read);
				length -= read;

				PrintTransmission(output == unityPipeStream, writeBuff, read, prefix, length == 0);
				prefix = false;
			}

			if (!outputConnected())
			{
				middlemanOutputLog.WriteLine($"{(output == unityPipeStream ? "Unity pipe" : "Compiler server")} closed.");

				if (output == unityPipeStream)
					SendShutdownMessage();

				compilerPipeStream.Close();
				unityPipeStream.Close();

				Environment.Exit(1);
			}
		}

		private void WriteString(Stream output, string str)
		{
			byte[] stringBuff = Encoding.UTF8.GetBytes(str);
			Write(output, stringBuff, 0, stringBuff.Length);
		}
		#endregion

		private void SendShutdownMessage()
		{
			if (!compilerPipeStreamConnected())
				return;

			WriteString(compilerPipeStream, "c:shutdown");
			compilerPipeStream.Flush();
		}
	}
}
