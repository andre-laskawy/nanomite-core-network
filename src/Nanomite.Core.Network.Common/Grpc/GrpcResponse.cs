// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: GrpcResponse.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Nanomite.Core.Network.Common {

  /// <summary>Holder for reflection information generated from GrpcResponse.proto</summary>
  public static partial class GrpcResponseReflection {

    #region Descriptor
    /// <summary>File descriptor for GrpcResponse.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static GrpcResponseReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChJHcnBjUmVzcG9uc2UucHJvdG8SHE5hbm9taXRlLkNvcmUuTmV0d29yay5D",
            "b21tb24aCWFueS5wcm90bxoQUmVzdWx0Q29kZS5wcm90byJ9CgxHcnBjUmVz",
            "cG9uc2USOAoGUmVzdWx0GAEgASgOMiguTmFub21pdGUuQ29yZS5OZXR3b3Jr",
            "LkNvbW1vbi5SZXN1bHRDb2RlEg8KB01lc3NhZ2UYAiABKAkSIgoERGF0YRgD",
            "IAMoCzIULmdvb2dsZS5wcm90b2J1Zi5BbnlCKwoPaW8uZ3JwYy5tZXNzYWdl",
            "QhBNZXNzYWdlRGF0YVByb3RvUAGiAgNITFdiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Google.Protobuf.WellKnownTypes.AnyReflection.Descriptor, global::Nanomite.Core.Network.Common.ResultCodeReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Nanomite.Core.Network.Common.GrpcResponse), global::Nanomite.Core.Network.Common.GrpcResponse.Parser, new[]{ "Result", "Message", "Data" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class GrpcResponse : pb::IMessage<GrpcResponse> {
    private static readonly pb::MessageParser<GrpcResponse> _parser = new pb::MessageParser<GrpcResponse>(() => new GrpcResponse());
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<GrpcResponse> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Nanomite.Core.Network.Common.GrpcResponseReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GrpcResponse() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GrpcResponse(GrpcResponse other) : this() {
      result_ = other.result_;
      message_ = other.message_;
      data_ = other.data_.Clone();
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GrpcResponse Clone() {
      return new GrpcResponse(this);
    }

    /// <summary>Field number for the "Result" field.</summary>
    public const int ResultFieldNumber = 1;
    private global::Nanomite.Core.Network.Common.ResultCode result_ = 0;
    /// <summary>
    //// &lt;summary>
    //// An enumeration indicating whether the command or fetch request was executed successfully.
    //// (E.g. OK, ERROR)
    //// &lt;/summary>  
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Nanomite.Core.Network.Common.ResultCode Result {
      get { return result_; }
      set {
        result_ = value;
      }
    }

    /// <summary>Field number for the "Message" field.</summary>
    public const int MessageFieldNumber = 2;
    private string message_ = "";
    /// <summary>
    //// &lt;summary>
    //// (Optional) In an error case this property can contain the specific error message.
    //// &lt;/summary>  
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Message {
      get { return message_; }
      set {
        message_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "Data" field.</summary>
    public const int DataFieldNumber = 3;
    private static readonly pb::FieldCodec<global::Google.Protobuf.WellKnownTypes.Any> _repeated_data_codec
        = pb::FieldCodec.ForMessage(26, global::Google.Protobuf.WellKnownTypes.Any.Parser);
    private readonly pbc::RepeatedField<global::Google.Protobuf.WellKnownTypes.Any> data_ = new pbc::RepeatedField<global::Google.Protobuf.WellKnownTypes.Any>();
    /// <summary>
    //// &lt;summary>
    //// (Optional) A List of any proto, which can contain any kind of other proto messages.
    //// This list is used to send the response data to a specific command or fetch request.
    //// &lt;/summary>  
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<global::Google.Protobuf.WellKnownTypes.Any> Data {
      get { return data_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as GrpcResponse);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(GrpcResponse other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Result != other.Result) return false;
      if (Message != other.Message) return false;
      if(!data_.Equals(other.data_)) return false;
      return true;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Result != 0) hash ^= Result.GetHashCode();
      if (Message.Length != 0) hash ^= Message.GetHashCode();
      hash ^= data_.GetHashCode();
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Result != 0) {
        output.WriteRawTag(8);
        output.WriteEnum((int) Result);
      }
      if (Message.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(Message);
      }
      data_.WriteTo(output, _repeated_data_codec);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Result != 0) {
        size += 1 + pb::CodedOutputStream.ComputeEnumSize((int) Result);
      }
      if (Message.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Message);
      }
      size += data_.CalculateSize(_repeated_data_codec);
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(GrpcResponse other) {
      if (other == null) {
        return;
      }
      if (other.Result != 0) {
        Result = other.Result;
      }
      if (other.Message.Length != 0) {
        Message = other.Message;
      }
      data_.Add(other.data_);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            input.SkipLastField();
            break;
          case 8: {
            result_ = (global::Nanomite.Core.Network.Common.ResultCode) input.ReadEnum();
            break;
          }
          case 18: {
            Message = input.ReadString();
            break;
          }
          case 26: {
            data_.AddEntriesFrom(input, _repeated_data_codec);
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
