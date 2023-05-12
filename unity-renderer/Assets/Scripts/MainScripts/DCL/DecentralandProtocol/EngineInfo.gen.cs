// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: decentraland/sdk/components/engine_info.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace DCL.ECSComponents {

  /// <summary>Holder for reflection information generated from decentraland/sdk/components/engine_info.proto</summary>
  public static partial class EngineInfoReflection {

    #region Descriptor
    /// <summary>File descriptor for decentraland/sdk/components/engine_info.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static EngineInfoReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Ci1kZWNlbnRyYWxhbmQvc2RrL2NvbXBvbmVudHMvZW5naW5lX2luZm8ucHJv",
            "dG8SG2RlY2VudHJhbGFuZC5zZGsuY29tcG9uZW50cyJQCgxQQkVuZ2luZUlu",
            "Zm8SFAoMZnJhbWVfbnVtYmVyGAEgASgNEhUKDXRvdGFsX3J1bnRpbWUYAiAB",
            "KAISEwoLdGlja19udW1iZXIYAyABKA1CFKoCEURDTC5FQ1NDb21wb25lbnRz",
            "YgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::DCL.ECSComponents.PBEngineInfo), global::DCL.ECSComponents.PBEngineInfo.Parser, new[]{ "FrameNumber", "TotalRuntime", "TickNumber" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  /// <summary>
  /// EngineInfo provides information about the graphics engine running the scene.
  /// The values of this component are written at the "physics" stage of the ADR-148. Meaning
  /// the tick_number and frame_number of the same frame could be used as correlation numbers
  /// for timestamps in other components.
  /// </summary>
  public sealed partial class PBEngineInfo : pb::IMessage<PBEngineInfo>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<PBEngineInfo> _parser = new pb::MessageParser<PBEngineInfo>(() => new PBEngineInfo());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<PBEngineInfo> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::DCL.ECSComponents.EngineInfoReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public PBEngineInfo() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public PBEngineInfo(PBEngineInfo other) : this() {
      frameNumber_ = other.frameNumber_;
      totalRuntime_ = other.totalRuntime_;
      tickNumber_ = other.tickNumber_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public PBEngineInfo Clone() {
      return new PBEngineInfo(this);
    }

    /// <summary>Field number for the "frame_number" field.</summary>
    public const int FrameNumberFieldNumber = 1;
    private uint frameNumber_;
    /// <summary>
    /// frame counter of the engine
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint FrameNumber {
      get { return frameNumber_; }
      set {
        frameNumber_ = value;
      }
    }

    /// <summary>Field number for the "total_runtime" field.</summary>
    public const int TotalRuntimeFieldNumber = 2;
    private float totalRuntime_;
    /// <summary>
    /// total runtime of this scene in seconds
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public float TotalRuntime {
      get { return totalRuntime_; }
      set {
        totalRuntime_ = value;
      }
    }

    /// <summary>Field number for the "tick_number" field.</summary>
    public const int TickNumberFieldNumber = 3;
    private uint tickNumber_;
    /// <summary>
    /// tick counter of the scene as per ADR-148
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint TickNumber {
      get { return tickNumber_; }
      set {
        tickNumber_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as PBEngineInfo);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(PBEngineInfo other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (FrameNumber != other.FrameNumber) return false;
      if (!pbc::ProtobufEqualityComparers.BitwiseSingleEqualityComparer.Equals(TotalRuntime, other.TotalRuntime)) return false;
      if (TickNumber != other.TickNumber) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (FrameNumber != 0) hash ^= FrameNumber.GetHashCode();
      if (TotalRuntime != 0F) hash ^= pbc::ProtobufEqualityComparers.BitwiseSingleEqualityComparer.GetHashCode(TotalRuntime);
      if (TickNumber != 0) hash ^= TickNumber.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (FrameNumber != 0) {
        output.WriteRawTag(8);
        output.WriteUInt32(FrameNumber);
      }
      if (TotalRuntime != 0F) {
        output.WriteRawTag(21);
        output.WriteFloat(TotalRuntime);
      }
      if (TickNumber != 0) {
        output.WriteRawTag(24);
        output.WriteUInt32(TickNumber);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (FrameNumber != 0) {
        output.WriteRawTag(8);
        output.WriteUInt32(FrameNumber);
      }
      if (TotalRuntime != 0F) {
        output.WriteRawTag(21);
        output.WriteFloat(TotalRuntime);
      }
      if (TickNumber != 0) {
        output.WriteRawTag(24);
        output.WriteUInt32(TickNumber);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (FrameNumber != 0) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(FrameNumber);
      }
      if (TotalRuntime != 0F) {
        size += 1 + 4;
      }
      if (TickNumber != 0) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(TickNumber);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(PBEngineInfo other) {
      if (other == null) {
        return;
      }
      if (other.FrameNumber != 0) {
        FrameNumber = other.FrameNumber;
      }
      if (other.TotalRuntime != 0F) {
        TotalRuntime = other.TotalRuntime;
      }
      if (other.TickNumber != 0) {
        TickNumber = other.TickNumber;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            FrameNumber = input.ReadUInt32();
            break;
          }
          case 21: {
            TotalRuntime = input.ReadFloat();
            break;
          }
          case 24: {
            TickNumber = input.ReadUInt32();
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 8: {
            FrameNumber = input.ReadUInt32();
            break;
          }
          case 21: {
            TotalRuntime = input.ReadFloat();
            break;
          }
          case 24: {
            TickNumber = input.ReadUInt32();
            break;
          }
        }
      }
    }
    #endif

  }

  #endregion

}

#endregion Designer generated code
