
using System.IO;

namespace RudeShadermiddleman.Common.Metadata
{
    public class ConstantBufferParameter
    {
        public string ParamName;
        public ShaderParamType ParamType;
        public int Rows;
        public int Columns;
        public bool IsMatrix;
        public int ArraySize;
        public int Index;

        public ConstantBufferParameter() { }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ParamName);
            writer.Write((int)ParamType);
            writer.Write(Rows);
            writer.Write(Columns);
            writer.Write(IsMatrix);
            writer.Write(ArraySize);
            writer.Write(Index);
        }

        public ConstantBufferParameter(BinaryReader reader)
        {
            ParamName = reader.ReadString();
            ParamType = (ShaderParamType)reader.ReadInt32();
            Rows = reader.ReadInt32();
            Columns = reader.ReadInt32();
            IsMatrix = reader.ReadBoolean();
            ArraySize = reader.ReadInt32();
            Index = reader.ReadInt32();
        }
    }
}
