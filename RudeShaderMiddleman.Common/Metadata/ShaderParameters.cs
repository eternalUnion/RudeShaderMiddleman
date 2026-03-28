using System.Collections.Generic;
using System.IO;

namespace RudeShaderMiddleman.Common.Metadata
{
	public class ShaderParameters
	{
		public ConstantBuffer BaseConstantBuffer;
		public List<ConstantBuffer> ConstantBuffers;

		public List<TextureParameter> TextureParameters;
		public List<ConstantBufferBinding> ConstBindings;
		public List<ConstantBufferBinding> Buffers;
		public List<UAVParameter> UAVs;
		public List<SamplerParameter> Samplers;

		public ShaderParameters(BinaryReader reader, List<string> nameMap)
		{
			int baseConstantBufferExists = reader.ReadInt32();
			if (baseConstantBufferExists != 0)
			{
				BaseConstantBuffer = new ConstantBuffer(reader, nameMap);
			}
			else
			{
				BaseConstantBuffer = null;
			}

			ConstantBuffers = new List<ConstantBuffer>();
			int cbCount = reader.ReadInt32();
			for (int i = 0; i < cbCount; i++)
			{
				ConstantBuffers.Add(new ConstantBuffer(reader, nameMap));
			}

			TextureParameters = new List<TextureParameter>();
			int texCount = reader.ReadInt32();
			for (int i = 0; i < texCount; i++)
			{
				TextureParameters.Add(new TextureParameter(reader, nameMap));
			}

			ConstBindings = new List<ConstantBufferBinding>();
			int cbBindsCount = reader.ReadInt32();
			for (int i = 0; i < cbBindsCount; i++)
			{
				ConstBindings.Add(new ConstantBufferBinding(reader, nameMap));
			}

			Buffers = new List<ConstantBufferBinding>();
			int buffCount = reader.ReadInt32();
			for (int i = 0; i < buffCount; i++)
			{
				Buffers.Add(new ConstantBufferBinding(reader, nameMap));
			}

			UAVs = new List<UAVParameter>();
			int uavCount = reader.ReadInt32();
			for (int i = 0; i < uavCount; i++)
			{
				UAVs.Add(new UAVParameter(reader, nameMap));
			}

			Samplers = new List<SamplerParameter>();
			int samplerCount = reader.ReadInt32();
			for (int i = 0; i < samplerCount; i++)
			{
				Samplers.Add(new SamplerParameter(reader));
			}
		}

		public void Serialize(BinaryWriter writer, List<string> nameMap)
		{
			if (BaseConstantBuffer == null)
			{
				writer.Write(0);
			}
			else
			{
				writer.Write(1);
				BaseConstantBuffer.Serialize(writer, nameMap);
			}

			writer.Write((int)ConstantBuffers.Count);
			foreach (var constantBuffer in ConstantBuffers)
			{
				constantBuffer.Serialize(writer, nameMap);
			}

			writer.Write((int)TextureParameters.Count);
			foreach (var textureParameter in TextureParameters)
			{
				textureParameter.Serialize(writer, nameMap);
			}

			writer.Write((int)ConstBindings.Count);
			foreach (var constBinding in ConstBindings)
			{
				constBinding.Serialize(writer, nameMap);
			}

			writer.Write((int)Buffers.Count);
			foreach (var buffer in Buffers)
			{
				buffer.Serialize(writer, nameMap);
			}

			writer.Write((int)UAVs.Count);
			foreach (var uav in UAVs)
			{
				uav.Serialize(writer, nameMap);
			}

			writer.Write((int)Samplers.Count);
			foreach (var sampler in Samplers)
			{
				sampler.Serialize(writer);
			}
		}
	}
}
