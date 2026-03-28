
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace RudeShaderMiddleman.Common.Metadata
{
    public class ConstantBufferParameter
    {
        public string ParamName;
        private int ParamNameIndex;
        public ShaderParamType ParamType;
        public int Rows;
        public int Columns;
        public bool IsMatrix;
        public int ArraySize;
        public int Index;

        public ConstantBufferParameter() { }

        public void Serialize(BinaryWriter writer, List<string> nameMap)
        {
			ParamNameIndex = nameMap.IndexOf(ParamName);
			if (ParamNameIndex == -1)
			{
				ParamNameIndex = nameMap.Count;
				nameMap.Add(ParamName);
			}

			writer.Write(ParamNameIndex);
            writer.Write((int)ParamType);
            writer.Write(Rows);
            writer.Write(Columns);
            writer.Write(IsMatrix);
            writer.Write(ArraySize);
            writer.Write(Index);
        }

        public ConstantBufferParameter(BinaryReader reader, List<string> nameMap)
        {
            ParamNameIndex = reader.ReadInt32();
            ParamName = nameMap[ParamNameIndex];
            ParamType = (ShaderParamType)reader.ReadInt32();
            Rows = reader.ReadInt32();
            Columns = reader.ReadInt32();
            IsMatrix = reader.ReadBoolean();
            ArraySize = reader.ReadInt32();
            Index = reader.ReadInt32();
        }
    }
}
