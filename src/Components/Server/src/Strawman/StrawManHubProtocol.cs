// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.ExceptionServices;
using ConsoleApp3;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// Implements the SignalR Hub Protocol using MessagePack.
    /// </summary>
    public class StrawManHubProtocol : IHubProtocol
    {
        private const int ErrorResult = 1;
        private const int VoidResult = 2;
        private const int NonVoidResult = 3;

        internal static readonly string ProtocolName = "messagepack";
        private static readonly int ProtocolVersion = 1;
        private static readonly int ProtocolMinorVersion = 0;

        /// <inheritdoc />
        public string Name => ProtocolName;

        /// <inheritdoc />
        public int Version => ProtocolVersion;

        /// <inheritdoc />
        public int MinorVersion => ProtocolMinorVersion;

        /// <inheritdoc />
        public TransferFormat TransferFormat => TransferFormat.Binary;

        /// <inheritdoc />
        public virtual bool IsVersionSupported(int version)
        {
            return version == Version;
        }

        /// <inheritdoc />
        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            if (!BinaryMessageParser.TryParseMessage(ref input, out var payload))
            {
                message = null;
                return false;
            }

            var reader = new MessagePackReader(ref payload);

            if (!reader.TryReadArrayLength(out var itemCount))
            {
                ThrowFormatException("itemCount");
            }

            if (!reader.TryReadInt32(out var messageType))
            {
                ThrowFormatException("messageType");
            }

            switch (messageType)
            {
                case HubProtocolConstants.InvocationMessageType:
                    message = CreateInvocationMessage(ref reader, binder, itemCount);
                    return true;
                case HubProtocolConstants.StreamInvocationMessageType:
                    message = CreateStreamInvocationMessage(ref reader, binder, itemCount);
                    return true;
                case HubProtocolConstants.StreamItemMessageType:
                    message = CreateStreamItemMessage(ref reader, binder);
                    return true;
                case HubProtocolConstants.CompletionMessageType:
                    message = CreateCompletionMessage(ref reader, binder);
                    return true;
                case HubProtocolConstants.CancelInvocationMessageType:
                    message = CreateCancelInvocationMessage(ref reader);
                    return true;
                case HubProtocolConstants.PingMessageType:
                    message = PingMessage.Instance;
                    return true;
                case HubProtocolConstants.CloseMessageType:
                    message = CreateCloseMessage(ref reader);
                    return true;
                default:
                    // Future protocol changes can add message types, old clients can ignore them
                    message = null;
                    return false;
            }
        }

        private static HubMessage CreateInvocationMessage(ref MessagePackReader reader, IInvocationBinder binder, uint itemCount)
        {
            var headers = ReadHeaders(ref reader);
            var invocationId = ReadInvocationId(ref reader);

            // For MsgPack, we represent an empty invocation ID as an empty string,
            // so we need to normalize that to "null", which is what indicates a non-blocking invocation.
            if (string.IsNullOrEmpty(invocationId))
            {
                invocationId = null;
            }

            var target = ReadString(ref reader, "target");

            object[] arguments;
            try
            {
                var parameterTypes = binder.GetParameterTypes(target);
                arguments = BindArguments(ref reader, parameterTypes);
            }
            catch (Exception ex) when (!(ex is FormatException))
            {
                return new InvocationBindingFailureMessage(invocationId, target, ExceptionDispatchInfo.Capture(ex));
            }

            string[] streams = null;
            // Previous clients will send 5 items, so we check if they sent a stream array or not
            if (itemCount > 5)
            {
                streams = ReadStreamIds(ref reader);
            }

            return ApplyHeaders(headers, new InvocationMessage(invocationId, target, arguments, streams));
        }

        private static HubMessage CreateStreamInvocationMessage(ref MessagePackReader reader, IInvocationBinder binder, uint itemCount)
        {
            var headers = ReadHeaders(ref reader);
            var invocationId = ReadInvocationId(ref reader);
            var target = ReadString(ref reader, "target");

            object[] arguments;
            try
            {
                var parameterTypes = binder.GetParameterTypes(target);
                arguments = BindArguments(ref reader, parameterTypes);
            }
            catch (Exception ex) when (!(ex is FormatException))
            {
                return new InvocationBindingFailureMessage(invocationId, target, ExceptionDispatchInfo.Capture(ex));
            }

            string[] streams = null;
            // Previous clients will send 5 items, so we check if they sent a stream array or not
            if (itemCount > 5)
            {
                streams = ReadStreamIds(ref reader);
            }

            return ApplyHeaders(headers, new StreamInvocationMessage(invocationId, target, arguments, streams));
        }

        private static StreamItemMessage CreateStreamItemMessage(ref MessagePackReader reader, IInvocationBinder binder)
        {
            var headers = ReadHeaders(ref reader);
            var invocationId = ReadInvocationId(ref reader);

            var itemType = binder.GetStreamItemType(invocationId);
            var value = DeserializeObject(ref reader, itemType, "item");
            return ApplyHeaders(headers, new StreamItemMessage(invocationId, value));
        }

        private static CompletionMessage CreateCompletionMessage(ref MessagePackReader reader, IInvocationBinder binder)
        {
            var headers = ReadHeaders(ref reader);
            var invocationId = ReadInvocationId(ref reader);

            if (!reader.TryReadInt32(out var resultKind))
            {
                ThrowFormatException("resultKind");
            }

            string error = null;
            object result = null;
            var hasResult = false;

            switch (resultKind)
            {
                case ErrorResult:
                    error = ReadString(ref reader, "error");
                    break;
                case NonVoidResult:
                    var itemType = binder.GetReturnType(invocationId);
                    result = DeserializeObject(ref reader, itemType, "argument");
                    hasResult = true;
                    break;
                case VoidResult:
                    hasResult = false;
                    break;
                default:
                    throw new InvalidDataException("Invalid invocation result kind.");
            }

            return ApplyHeaders(headers, new CompletionMessage(invocationId, error, result, hasResult));
        }

        private static CancelInvocationMessage CreateCancelInvocationMessage(ref MessagePackReader reader)
        {
            var headers = ReadHeaders(ref reader);
            var invocationId = ReadInvocationId(ref reader);
            return ApplyHeaders(headers, new CancelInvocationMessage(invocationId));
        }

        private static CloseMessage CreateCloseMessage(ref MessagePackReader reader)
        {
            var error = ReadString(ref reader, "error");
            return new CloseMessage(error);
        }

        private static Dictionary<string, string> ReadHeaders(ref MessagePackReader reader)
        {
            if (!reader.TryReadMapLength(out var headerCount))
            {
                ThrowFormatException("headers");
            }

            if (headerCount == 0)
            {
                return null;
            }

            var headers = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < headerCount; i++)
            {
                if (!reader.TryReadString(out var key))
                {
                    ThrowFormatException($"headers[{i}].Key");
                }

                if (!reader.TryReadString(out var value))
                {
                    ThrowFormatException($"headers[{i}].Value");
                }

                headers[key] = value;
            }

            return headers;
        }

        private static string[] ReadStreamIds(ref MessagePackReader reader)
        {
            if (!reader.TryReadArrayLength(out var streamIdCount))
            {
                ThrowFormatException("streamIds");
            }

            if (streamIdCount == 0)
            {
                return null;
            }

            var streams = new List<string>();
            for (var i = 0; i < streamIdCount; i++)
            {
                streams.Add(ReadString(ref reader, $"streamId[{i}]"));
            }

            return streams.ToArray();
        }

        private static object[] BindArguments(ref MessagePackReader reader, IReadOnlyList<Type> parameterTypes)
        {
            if (!reader.TryReadArrayLength(out var argumentCount))
            {
                ThrowFormatException("arguments");
            }

            if (parameterTypes.Count != argumentCount)
            {
                throw new InvalidDataException(
                    $"Invocation provides {argumentCount} argument(s) but target expects {parameterTypes.Count}.");
            }

            try
            {
                var arguments = new object[argumentCount];
                for (var i = 0; i < argumentCount; i++)
                {
                    arguments[i] = DeserializeObject(ref reader, parameterTypes[i], "argument");
                }

                return arguments;
            }
            catch (Exception ex) when (!(ex is FormatException))
            {
                throw new InvalidDataException("Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.", ex);
            }
        }

        /// <inheritdoc />
        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        {
            var writer = MemoryBufferWriter.Get();

            try
            {
                // Write message to a buffer so we can get its length
                WriteMessageCore(message, writer);

                // Write length then message to output
                BinaryMessageFormatter.WriteLengthPrefix(writer.Length, output);
                writer.CopyTo(output);
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }

        ///// <inheritdoc />
        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
        {
            var pipe = new Pipe();
            var writer = pipe.Writer;

            // Write message to a buffer so we can get its length
            WriteMessageCore(message, writer);

            var memory = writer.GetMemory();

            var dataLength = memory.Length;
            var prefixLength = BinaryMessageFormatter.LengthPrefixLength(dataLength);

            var array = new byte[dataLength + prefixLength];
            var span = array.AsSpan();

            // Write length then message to output
            var written = BinaryMessageFormatter.WriteLengthPrefix(dataLength, span);
            Debug.Assert(written == prefixLength);

            memory.Span.CopyTo(span.Slice(prefixLength));

            return array;
        }

        private void WriteMessageCore(HubMessage message, IBufferWriter<byte> writer)
        {
            var packer = new MessagePackWriter(writer);

            switch (message)
            {
                case InvocationMessage invocationMessage:
                    WriteInvocationMessage(invocationMessage, ref packer);
                    break;
                case StreamInvocationMessage streamInvocationMessage:
                    WriteStreamInvocationMessage(streamInvocationMessage, ref packer);
                    break;
                case StreamItemMessage streamItemMessage:
                    WriteStreamingItemMessage(streamItemMessage, ref packer);
                    break;
                case CompletionMessage completionMessage:
                    WriteCompletionMessage(completionMessage, ref packer);
                    break;
                case CancelInvocationMessage cancelInvocationMessage:
                    WriteCancelInvocationMessage(cancelInvocationMessage, ref packer);
                    break;
                case PingMessage pingMessage:
                    WritePingMessage(pingMessage, ref packer);
                    break;
                case CloseMessage closeMessage:
                    WriteCloseMessage(closeMessage, ref packer);
                    break;
                default:
                    throw new InvalidDataException($"Unexpected message type: {message.GetType().Name}");
            }

            packer.Flush();
        }

        private void WriteInvocationMessage(InvocationMessage message, ref MessagePackWriter packer)
        {
            packer.WriteArrayLength(6);

            packer.WriteInt32(HubProtocolConstants.InvocationMessageType);
            PackHeaders(ref packer, message.Headers);
            if (string.IsNullOrEmpty(message.InvocationId))
            {
                packer.WriteNil();
            }
            else
            {
                packer.WriteString(message.InvocationId);
            }
            packer.WriteString(message.Target);
            packer.WriteArrayLength(message.Arguments.Length);
            foreach (var arg in message.Arguments)
            {
                SerializeArgument(arg, ref packer);
            }

            WriteStreamIds(message.StreamIds, ref packer);
        }

        private void WriteStreamInvocationMessage(StreamInvocationMessage message, ref MessagePackWriter packer)
        {
            packer.WriteArrayLength(6);

            packer.WriteInt32(HubProtocolConstants.StreamInvocationMessageType);
            PackHeaders(ref packer, message.Headers);
            packer.WriteString(message.InvocationId);
            packer.WriteString(message.Target);

            packer.WriteArrayLength(message.Arguments.Length);
            foreach (var arg in message.Arguments)
            {
                SerializeArgument(arg, ref packer);
            }

            WriteStreamIds(message.StreamIds, ref packer);
        }

        private void WriteStreamingItemMessage(StreamItemMessage message, ref MessagePackWriter packer)
        {
            packer.WriteArrayLength(4);
            packer.WriteInt32(HubProtocolConstants.StreamItemMessageType);
            PackHeaders(ref packer, message.Headers);
            packer.WriteString(message.InvocationId);
            SerializeArgument(message.Item, ref packer);
        }

        private void SerializeArgument(object argument, ref MessagePackWriter packer)
        {
            switch (argument)
            {
                case null:
                    packer.WriteNil();
                    break;

                case string stringValue:
                    packer.WriteString(stringValue);
                    break;

                case int intValue:
                    packer.WriteInt32(intValue);
                    break;

                case long longValue:
                    packer.WriteInt64(longValue);
                    break;

                case byte[] byteArray:
                    packer.WriteBytes(byteArray);
                    break;

                default:
                    throw new FormatException($"Unsupported argument type {argument.GetType()}");
            }
        }

        private static object DeserializeObject(ref MessagePackReader reader, Type type, string field)
        {
            if (type == typeof(string))
            {
                if (reader.TryReadNil())
                {
                    return null;
                }
                else if (reader.TryReadString(out var value))
                {
                    return value;
                }

                ThrowFormatException(field);
            }
            else if (type == typeof(int))
            {
                if (reader.TryReadInt32(out var value))
                {
                    return value;
                }

                ThrowFormatException(field);
            }
            else if (type == typeof(long))
            {
                if (reader.TryReadInt64(out var value))
                {
                    return value;
                }

                ThrowFormatException(field);
            }
            else if (type == typeof(byte[]))
            {
                if (!reader.TryReadArrayLength(out var length))
                {
                    ThrowFormatException(field + ".length");
                }

                var bytes = new List<byte>();
                for (var i = 0; i < length; i++)
                {
                    if (!reader.TryReadUInt8(out var value))
                    {
                        ThrowFormatException(field + $"[i]");
                    }

                    bytes.Add(value);
                }

                return bytes.ToArray();
            }


            throw new FormatException($"Type {type} is not supported");
        }


        private void WriteStreamIds(string[] streamIds, ref MessagePackWriter packer)
        {
            if (streamIds != null)
            {
                packer.WriteArrayLength(streamIds.Length);
                foreach (var streamId in streamIds)
                {
                    packer.WriteString(streamId);
                }
            }
            else
            {
                packer.WriteArrayLength(0);
            }
        }

        private void WriteCompletionMessage(CompletionMessage message, ref MessagePackWriter packer)
        {
            var resultKind =
                message.Error != null ? ErrorResult :
                message.HasResult ? NonVoidResult :
                VoidResult;

            packer.WriteArrayLength(4 + (resultKind != VoidResult ? 1 : 0));
            packer.WriteInt32(HubProtocolConstants.CompletionMessageType);
            PackHeaders(ref packer, message.Headers);
            packer.WriteString(message.InvocationId);
            packer.WriteInt32(resultKind);
            switch (resultKind)
            {
                case ErrorResult:
                    packer.WriteString(message.Error);
                    break;
                case NonVoidResult:
                    SerializeArgument(message.Result, ref packer);
                    break;
            }
        }

        private void WriteCancelInvocationMessage(CancelInvocationMessage message, ref MessagePackWriter packer)
        {
            packer.WriteArrayLength(3);
            packer.WriteInt32(HubProtocolConstants.CancelInvocationMessageType);
            PackHeaders(ref packer, message.Headers);
            packer.WriteString(message.InvocationId);
        }

        private void WriteCloseMessage(CloseMessage message, ref MessagePackWriter packer)
        {
            packer.WriteArrayLength(2);
            packer.WriteInt32(HubProtocolConstants.CloseMessageType);
            if (string.IsNullOrEmpty(message.Error))
            {
                packer.WriteNil();
            }
            else
            {
                packer.WriteString(message.Error);
            }
        }

        private void WritePingMessage(PingMessage _, ref MessagePackWriter packer)
        {
            packer.WriteArrayLength(1);
            packer.WriteInt32(HubProtocolConstants.PingMessageType);
        }

        private void PackHeaders(ref MessagePackWriter writer, IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                writer.WriteMapLength(0);
                return;
            }

            writer.WriteMapLength(headers.Count);
            foreach (var header in headers)
            {
                writer.WriteString(header.Key);
                writer.WriteString(header.Value);
            }
        }

        private static string ReadInvocationId(ref MessagePackReader reader) => ReadString(ref reader, "invocationId");

        private static string ReadString(ref MessagePackReader reader, string field)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (!reader.TryReadString(out var value))
            {
                ThrowFormatException(field);
            }

            return value;
        }

        private static T ApplyHeaders<T>(IDictionary<string, string> source, T destination) where T : HubInvocationMessage
        {
            if (source != null && source.Count > 0)
            {
                destination.Headers = source;
            }

            return destination;
        }

        private static void ThrowFormatException(string field)
        {
            throw new FormatException($"Reading field '{field}' failed.");
        }
    }
}
