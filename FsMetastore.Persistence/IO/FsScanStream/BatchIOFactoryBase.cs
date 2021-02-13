using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FsMetastore.Model.Batch;
using FsMetastore.Persistence.IO.Test;

namespace FsMetastore.Persistence.IO.FsScanStream
{
    abstract class BatchIOFactoryBase : IBatchIOFactory
    {
        protected readonly ITestOutputer _testOutput;
        protected readonly BatchIOConfig _batchIOConfig;
        protected  readonly string _batchPathRoot;
        public BatchIOFactoryBase(BatchIOConfig batchIOConfig, ITestOutputer testOutput)
        {
            _batchIOConfig = batchIOConfig;
            _testOutput = testOutput;

            _batchPathRoot = batchIOConfig.BatchPathRoot;

            if (!Directory.Exists(_batchPathRoot))
            {
                throw new ApplicationException($"Target path not found: {_batchPathRoot}");
            }
        }
        
        protected string CreateFilePath(MetaFileType fileType, string suffix)
        {
            return Path.Combine(_batchPathRoot, BatchFileNames.CreateFileName(fileType, suffix));
        }

        public async Task<T> ReadJsonAsync<T>(MetaFileType fileType)
        {
            var filePath = CreateFilePath(fileType, BatchFileNames.JsonSuffix);
            if (!File.Exists(filePath))
            {
                return default(T);
            }

            using (var fs = OpenFileStream(fileType, BatchFileNames.JsonSuffix, FileMode.Open))
            {
                return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(fs);
            }
        }
        
        public async Task WriteJsonAsync<T>(MetaFileType fileType, T objectToWrite)
        {
            using (var fs = OpenFileStream(fileType, BatchFileNames.JsonSuffix, FileMode.Create))
            {
                await System.Text.Json.JsonSerializer.SerializeAsync(fs, objectToWrite);
            }
        }

        protected FileStream OpenFileStream(MetaFileType fileType, string suffix, FileMode fileMode)
        {
            var filePath = CreateFilePath(fileType, suffix);
            _testOutput?.WriteLine($"Opening {fileMode}:{filePath}");
            var fileStream = OpenFileWithWait(filePath, fileMode, _batchIOConfig.MaxWait);
            return fileStream;
        }

        protected static FileStream OpenFileWithWait(string filePath, FileMode fileMode, TimeSpan maxWait)
        {
            
            Stopwatch sw = null;
            while(true) {
                FileStream fs = null;
                try {
                    fs = File.Open(filePath, fileMode);
                    return fs;
                }
                catch (IOException) {
                    fs?.Dispose ();

                    if(maxWait > TimeSpan.Zero)
                    {
                        if(sw == null)
                        {
                            sw = Stopwatch.StartNew();
                        }
                        if(sw.Elapsed > maxWait)
                        {
                            throw;
                        }
                    }

                    TimeSpan minTimeToWait = TimeSpan.FromMilliseconds(50);
                    TimeSpan maxTimeToWait = TimeSpan.FromSeconds(1);
                    var timeToWait = TimeSpan.FromSeconds(maxWait.TotalSeconds/10);
                    if(minTimeToWait > timeToWait)
                    {
                        timeToWait = minTimeToWait;
                    }
                    else if(timeToWait > maxTimeToWait)
                    {
                        timeToWait = maxTimeToWait;
                    }

                    Thread.Sleep (timeToWait);
                }
            }
        }

        //this doesn't work very well.
        private static FileStream OpenFileWithWatcher(string filePath, FileMode fileMode, TimeSpan maxWait)
        {
            
            if (maxWait <= TimeSpan.Zero)
            {
                //skip all the file monitoring if the caller doesn't want to wait anyway.
                return File.Open(filePath, fileMode);
            }

            var sw = Stopwatch.StartNew();
            //setup a file watcher before trying to read the file to avoid the race conditions doing it the other way around
            var resetEvent = new ManualResetEventSlim(false);
            var fileSystemWatcher =
                new FileSystemWatcher(Path.GetDirectoryName(filePath))
                {
                    EnableRaisingEvents = true
                };

            fileSystemWatcher.Changed +=
                (o, e) =>
                {
                    if (Path.GetFullPath(e.FullPath) == Path.GetFullPath(filePath))
                    {
                        resetEvent.Set();
                    }
                };

            do
            {
                FileStream fs = null;
                try
                {
                    fs = File.Open(filePath, fileMode);
                    return fs;
                }
                catch (IOException)
                {
                    fs?.Dispose();

                    var timeToWait = maxWait - (sw.Elapsed);

                    if (timeToWait <= TimeSpan.Zero)
                    {
                        throw;
                    }

                    if (resetEvent.Wait(timeToWait))
                    {
                        resetEvent.Reset();
                    }
                }
            } while (true);
        }
    }
}