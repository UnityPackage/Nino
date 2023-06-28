/* this is generated by nino */
using System.Runtime.CompilerServices;

namespace Nino.Benchmark.Models
{
    public partial class NestedData
    {
        public static NestedData.SerializationHelper NinoSerializationHelper = new NestedData.SerializationHelper();
        public unsafe class SerializationHelper: Nino.Serialization.NinoWrapperBase<NestedData>
        {
            #region NINO_CODEGEN
            public SerializationHelper()
            {

            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void Serialize(NestedData value, ref Nino.Serialization.Writer writer)
            {
                if(value == null)
                {
                    writer.Write(false);
                    return;
                }
                writer.Write(true);
                writer.Write(value.Name);
                writer.Write(value.Ps);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override NestedData Deserialize(Nino.Serialization.Reader reader)
            {
                if(!reader.ReadBool())
                    return null;
                NestedData value = new NestedData();
                value.Name = reader.ReadString();
                value.Ps = reader.ReadArray<Nino.Benchmark.Models.Data>();
                return value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetSize(NestedData value)
            {
                if(value == null)
                {
                    return 1;
                }
                int ret = 1;
                ret += Nino.Serialization.Serializer.GetSize(value.Name);
                ret += Nino.Serialization.Serializer.GetSize(value.Ps);
                return ret;
            }
            #endregion
        }
    }
}