using FsMetastore.Persistence.IO.FileBatches;
using FsMetastore.Persistence.IO.FsScanStream;

namespace FsMetastore.Persistence.IndexedData
{
    public class StringRefOptimizer
    {
        private readonly IStringRefWriter _stringRefWriter;
        private readonly IScanDbBatchIOFactory _scanDbBatchIOFactory;

        public StringRefOptimizer(
            IStringRefWriter stringRefWriter, 
            IScanDbBatchIOFactory scanDbBatchIOFactory)
        {
            _stringRefWriter = stringRefWriter;
            _scanDbBatchIOFactory = scanDbBatchIOFactory;
        }

        public void Optimize(IMetaReader metaReader, IMetaPersister metaWriter)
        {
            _stringRefWriter.InitDb();
            var folder = metaReader.ReadNextFolder();
            var file = metaReader.ReadNextFile();
            foreach (var stringMap in _stringRefWriter.GetStringMap())
            {
                var found = 0;
                while (folder != null && folder.Name.Id == stringMap.currentId)
                {
                    folder.Name.Id = (int) stringMap.targetId;
                    metaWriter.StoreFolder(folder);
                    folder = metaReader.IsFolderReaderAtEnd ? null : metaReader.ReadNextFolder();
                    found++;
                }

                while (file != null && file.Name.Id == stringMap.currentId)
                {
                    file.Name.Id = (int) stringMap.targetId;
                    metaWriter.StoreFile(file);
                    file = metaReader.IsFileReaderAtEnd ? null : metaReader.ReadNextFile();
                    found++;
                }

                if (found <= 0)
                {

                }
            }

            var indexedDataWriter = _scanDbBatchIOFactory.CreateIndexedDataWriter();
            indexedDataWriter.Create();
            foreach (var stringMap in _stringRefWriter.GetUniqueStrings())
            {
                indexedDataWriter.WriteStringItem((uint)stringMap.stringId, stringMap.val);
            }
            indexedDataWriter.Close();
            _stringRefWriter.DeleteDb();
            //_stringRefWriter.Flush();
            metaWriter.Close();
            metaReader.Close();
            _scanDbBatchIOFactory.CleanPreOptimFiles();
        }
    }
}
