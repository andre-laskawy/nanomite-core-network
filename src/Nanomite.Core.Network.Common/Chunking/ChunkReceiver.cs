///-----------------------------------------------------------------
///   File:         ChunkReceiver.cs
///   Author:   	Andre Laskawy           
///   Date:         06.10.2018 13:55:09
///-----------------------------------------------------------------

namespace Nanomite.Core.Network.Common.Chunking
{
    using Google.Protobuf;
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using Nanomite.Core.Network.Common.Models;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="ChunkReceiver" />
    /// </summary>
    public class ChunkReceiver : IChunkReceiver<Command>
    {
        /// <inheritdoc />
        public Action<Command, string, string, Metadata> FileReceived { get; set; }

        /// <inheritdoc />
        internal BlockingCollection<File> fileList = new BlockingCollection<File>();

        /// <inheritdoc />
        private ConcurrentDictionary<string, BlockingCollection<FileChunk>> fileChunkDictionary = new ConcurrentDictionary<string, BlockingCollection<FileChunk>>();

        /// <inheritdoc />
        public async void ChunkReceived(Command cmd, string streamId, string token, Metadata header)
        {
            try
            {
                if (cmd.Data.FirstOrDefault().TypeUrl == Any.Pack(new FileChunk()).TypeUrl)
                {
                    var chunk = cmd.Data.FirstOrDefault().CastToModel<FileChunk>();
                    if (fileChunkDictionary.ContainsKey(chunk.FileId))
                    {
                        while (!fileChunkDictionary[chunk.FileId].TryAdd(chunk))
                        {
                            await Task.Delay(1);
                        }
                        File file = fileList.FirstOrDefault(p => p.Id == chunk.FileId);
                        Console.WriteLine(file.TotalChunks + "/" + fileChunkDictionary[file.Id].Count);
                        if (file.TotalChunks == fileChunkDictionary[file.Id].Count)
                        {
                            Command completeFile = new Command()
                            {
                                Type = CommandType.File,
                                Topic = StaticCommandKeys.FileTransfer
                            };

                            //stick together file
                            byte[] stickedFile = new byte[file.Size];
                            int chunksize = -1;
                            foreach (FileChunk c in fileChunkDictionary[file.Id].OrderBy(p => p.Index))
                            {
                                byte[] content = c.Content.ToArray();
                                if (chunksize == -1)
                                {
                                    chunksize = content.Length;
                                }
                                content.CopyTo(stickedFile, chunksize * c.Index);
                                content = null; // free up space in memory
                            }
                            file.Content = ByteString.CopyFrom(stickedFile);

                            //clear memory
                            stickedFile = null;

                            BlockingCollection<FileChunk> dump;
                            fileChunkDictionary.TryRemove(file.Id, out dump);
                            dump.Dispose();

                            //forward/send file as a complete package wrapped by a command
                            completeFile.Data.Add(Any.Pack(file));
                            FileReceived?.Invoke(completeFile, streamId, token, header);
                            completeFile = null; // free up space in memory
                            file = null; // free up space in memory
                        }
                    }
                    else
                    {
                        new Task(async () =>
                       {
                           await new Func<bool>(() => fileChunkDictionary.ContainsKey(chunk.FileId)).AwaitResult(60);
                           ChunkReceived(cmd, streamId, token, header);
                       }).Start();
                    }
                }
                else
                {
                    // File information received
                    File file = cmd.Data.FirstOrDefault().CastToModel<File>();
                    while (!fileList.TryAdd(file))
                    {
                        await Task.Delay(1);
                    }

                    if (!fileChunkDictionary.ContainsKey(file.Id))
                    {
                        while (!fileChunkDictionary.TryAdd(file.Id, new BlockingCollection<FileChunk>()))
                        {
                            await Task.Delay(1);
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
