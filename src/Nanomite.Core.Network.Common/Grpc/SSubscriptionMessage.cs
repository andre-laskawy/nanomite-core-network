// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: S_SubscriptionMessage.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Nanomite.Core.Network.Common {

  /// <summary>Holder for reflection information generated from S_SubscriptionMessage.proto</summary>
  public static partial class SSubscriptionMessageReflection {

    #region Descriptor
    /// <summary>File descriptor for S_SubscriptionMessage.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static SSubscriptionMessageReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChtTX1N1YnNjcmlwdGlvbk1lc3NhZ2UucHJvdG8SHE5hbm9taXRlLkNvcmUu",
            "TmV0d29yay5Db21tb24iJgoVU19TdWJzY3JpcHRpb25NZXNzYWdlEg0KBVRv",
            "cGljGAEgASgJQisKD2lvLmdycGMubWVzc2FnZUIQTWVzc2FnZURhdGFQcm90",
            "b1ABogIDSExXYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Nanomite.Core.Network.Common.S_SubscriptionMessage), global::Nanomite.Core.Network.Common.S_SubscriptionMessage.Parser, new[]{ "Topic" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class S_SubscriptionMessage : pb::IMessage<S_SubscriptionMessage> {
    private static readonly pb::MessageParser<S_SubscriptionMessage> _parser = new pb::MessageParser<S_SubscriptionMessage>(() => new S_SubscriptionMessage());
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<S_SubscriptionMessage> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Nanomite.Core.Network.Common.SSubscriptionMessageReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public S_SubscriptionMessage() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public S_SubscriptionMessage(S_SubscriptionMessage other) : this() {
      topic_ = other.topic_;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public S_SubscriptionMessage Clone() {
      return new S_SubscriptionMessage(this);
    }

    /// <summary>Field number for the "Topic" field.</summary>
    public const int TopicFieldNumber = 1;
    private string topic_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Topic {
      get { return topic_; }
      set {
        topic_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as S_SubscriptionMessage);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(S_SubscriptionMessage other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Topic != other.Topic) return false;
      return true;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Topic.Length != 0) hash ^= Topic.GetHashCode();
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Topic.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Topic);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Topic.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Topic);
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(S_SubscriptionMessage other) {
      if (other == null) {
        return;
      }
      if (other.Topic.Length != 0) {
        Topic = other.Topic;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            input.SkipLastField();
            break;
          case 10: {
            Topic = input.ReadString();
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
