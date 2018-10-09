///-----------------------------------------------------------------
///   File:         ChunkSender.cs
///   Author:   	Andre Laskawy           
///   Date:         06.10.2018 13:57:35
///-----------------------------------------------------------------

namespace Nanomite.Core.Network.Common.Chunking
{
    using Google.Protobuf;
    using Google.Protobuf.WellKnownTypes;
    using Nanomite.Core.Network.Common;
    using Nanomite.Core.Network.Common.Models;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="ChunkSender" />
    /// </summary>
    public class ChunkSender : IChunkSender<Command, FetchRequest, GrpcResponse>
    {
        /// <summary>
        /// The chunk size. A multiple of bytes.
        /// Default 1 MByte.
        /// </summary>
        public static int ChunkSize = 1024 * 1024;

        /// <summary>
        /// Sends the file.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="command">The command<see cref="Command"/></param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public async Task SendFile(IClient<Command, FetchRequest, GrpcResponse> client, Command command, int timeout = 60)
        {
            try
            {
                File file = command.Data.FirstOrDefault().CastToModel<File>();
                file.TotalChunks = Convert.ToInt32(Math.Floor((double)file.Size / ChunkSize) + 1);

                byte[] fileContent = file.Content.ToByteArray();

                ByteString empty = ByteString.CopyFrom(new byte[0]);
                file.Content = empty;
                IStream<Command> stream = client.GetStream();
                stream.Connected = true;

                // Generate and transfer file information
                File fileInformation = new File()
                {
                    Id = file.Id,
                    CreatedDT = file.CreatedDT,
                    ModifiedDT = file.ModifiedDT,
                    Name = file.Name,
                    Size = file.Size,
                    TotalChunks = file.TotalChunks,
                    Version = file.Version
                };

                Command fileCmd = new Command()
                {
                    Type = CommandType.File,
                    Topic = StaticCommandKeys.FileTransfer
                };
                fileCmd.Data.Add(Any.Pack(fileInformation));
                stream.AddToQueue(fileCmd);

                // Generate and transfer file chunks
                for (int i = 0; i < file.TotalChunks; i++)
                {
                    Command cmd = new Command()
                    {
                        Type = CommandType.File,
                        Topic = StaticCommandKeys.FileTransfer
                    };

                    FileChunk chunk = new FileChunk()
                    {
                        Content = ByteString.CopyFrom(fileContent.Skip(i * ChunkSize).Take(ChunkSize).ToArray()),
                        FileId = file.Id,
                        Index = i
                    };

                    cmd.Data.Add(Any.Pack(chunk));
                    stream.AddToQueue(cmd);
                    chunk = null; // free up space in memory
                    cmd = null; // free up space in memory
                };

                await Task.Delay(0);
            }
            catch
            {
                throw;
            }
        }
    }
}
