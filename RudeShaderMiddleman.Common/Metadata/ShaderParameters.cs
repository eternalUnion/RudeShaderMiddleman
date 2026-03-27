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

		public ShaderParameters(BinaryReader reader)
		{
			int baseConstantBufferExists = reader.ReadInt32();
			if (baseConstantBufferExists != 0)
			{
				BaseConstantBuffer = new ConstantBuffer(reader);
			}
			else
			{
				BaseConstantBuffer = null;
			}

			ConstantBuffers = new List<ConstantBuffer>();
			int cbCount = reader.ReadInt32();
			for (int i = 0; i < cbCount; i++)
			{
				ConstantBuffers.Add(new ConstantBuffer(reader));
			}

			TextureParameters = new List<TextureParameter>();
			int texCount = reader.ReadInt32();
			for (int i = 0; i < texCount; i++)
			{
				TextureParameters.Add(new TextureParameter(reader));
			}

			ConstBindings = new List<ConstantBufferBinding>();
			int cbBindsCount = reader.ReadInt32();
			for (int i = 0; i < cbBindsCount; i++)
			{
				ConstBindings.Add(new ConstantBufferBinding(reader));
			}

			Buffers = new List<ConstantBufferBinding>();
			int buffCount = reader.ReadInt32();
			for (int i = 0; i < buffCount; i++)
			{
				Buffers.Add(new ConstantBufferBinding(reader));
			}

			UAVs = new List<UAVParameter>();
			int uavCount = reader.ReadInt32();
			for (int i = 0; i < uavCount; i++)
			{
				UAVs.Add(new UAVParameter(reader));
			}

			Samplers = new List<SamplerParameter>();
			int samplerCount = reader.ReadInt32();
			for (int i = 0; i < samplerCount; i++)
			{
				Samplers.Add(new SamplerParameter(reader));
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			if (BaseConstantBuffer == null)
			{
				writer.Write(0);
			}
			else
			{
				writer.Write(1);
				BaseConstantBuffer.Serialize(writer);
			}

			writer.Write((int)ConstantBuffers.Count);
			foreach (var constantBuffer in ConstantBuffers)
			{
				constantBuffer.Serialize(writer);
			}

			writer.Write((int)TextureParameters.Count);
			foreach (var textureParameter in TextureParameters)
			{
				textureParameter.Serialize(writer);
			}

			writer.Write((int)ConstBindings.Count);
			foreach (var constBinding in ConstBindings)
			{
				constBinding.Serialize(writer);
			}

			writer.Write((int)Buffers.Count);
			foreach (var buffer in Buffers)
			{
				buffer.Serialize(writer);
			}

			writer.Write((int)UAVs.Count);
			foreach (var uav in UAVs)
			{
				uav.Serialize(writer);
			}

			writer.Write((int)Samplers.Count);
			foreach (var sampler in Samplers)
			{
				sampler.Serialize(writer);
			}
		}
	}
}
