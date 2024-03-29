// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace meta.types
{

using global::System;
using global::FlatBuffers;

public struct TransformT : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static TransformT GetRootAsTransformT(ByteBuffer _bb) { return GetRootAsTransformT(_bb, new TransformT()); }
  public static TransformT GetRootAsTransformT(ByteBuffer _bb, TransformT obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p.bb_pos = _i; __p.bb = _bb; }
  public TransformT __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public meta.types.BufferHeader? Header { get { int o = __p.__offset(4); return o != 0 ? (meta.types.BufferHeader?)(new meta.types.BufferHeader()).__assign(__p.__indirect(o + __p.bb_pos), __p.bb) : null; } }
  public meta.types.Position? Position { get { int o = __p.__offset(6); return o != 0 ? (meta.types.Position?)(new meta.types.Position()).__assign(o + __p.bb_pos, __p.bb) : null; } }
  public meta.types.Quaternion? Orientation { get { int o = __p.__offset(8); return o != 0 ? (meta.types.Quaternion?)(new meta.types.Quaternion()).__assign(o + __p.bb_pos, __p.bb) : null; } }

  public static void StartTransformT(FlatBufferBuilder builder) { builder.StartObject(3); }
  public static void AddHeader(FlatBufferBuilder builder, Offset<meta.types.BufferHeader> headerOffset) { builder.AddOffset(0, headerOffset.Value, 0); }
  public static void AddPosition(FlatBufferBuilder builder, Offset<meta.types.Position> positionOffset) { builder.AddStruct(1, positionOffset.Value, 0); }
  public static void AddOrientation(FlatBufferBuilder builder, Offset<meta.types.Quaternion> orientationOffset) { builder.AddStruct(2, orientationOffset.Value, 0); }
  public static Offset<TransformT> EndTransformT(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<TransformT>(o);
  }
  public static void FinishTransformTBuffer(FlatBufferBuilder builder, Offset<TransformT> offset) { builder.Finish(offset.Value); }
};


}
