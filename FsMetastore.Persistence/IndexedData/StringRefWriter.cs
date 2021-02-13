#define NODICTIONARY
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using FsMetastore.Model.Batch;
using FsMetastore.Persistence.Sqlite;

namespace FsMetastore.Persistence.IndexedData
{
    class StringRefWriter : SqliteBase, IStringRefWriter
    {
        private readonly string _targetFolder;
        private readonly int _stringsPerBatch;
        private int _maxId;
        private const string DataTableName = "strings";
#if NODICTIONARY
        private List<(string, int)> _lastBatch;
        private List<(string, int)> _nextBatch = new List<(string, int)>();
#else
        private Dictionary<string, int> _lastBatch;
        private Dictionary<string, int> _nextBatch = new Dictionary<string, int>();
#endif

        public StringRefWriter(BatchIOConfig batchIOConfig, int stringsPerBatch = 50000)
        {
            //if each string is 20 bytes, and we do 50000 per batch - that would be 1MB per batch?
            _targetFolder = batchIOConfig.BatchPathRoot;
            _stringsPerBatch = stringsPerBatch;
            _filePath = Path.Combine(_targetFolder, "stringrefs.db");
        }

        //total saving: select sum(saving) from ( select length(val)*(count(*) - 1) as saving from strings group by val) a
        
        public void InitDb()
        {
            OpenConnection();
            EnsureTableExists();

            _maxId = MaxId();
            //lets avoid maintaining indexes while we are just doing bulk inserts, we can recreate it at the end.
            DropAllIndexes(DataTableName);
        }

        private void EnsureTableExists()
        {
            base.EnsureTableExists(DataTableName, "(id INTEGER, val TEXT)");
        }
        
        void IStringRefWriter.DeleteDb()
        {
            base.DeleteDb();
        }

        protected override bool ShouldDeleteOnClose()
        {
            return MaxId() == 0;
        }


        public int AddString(string newString)
        {
#if NODICTIONARY
#else
            if (_lastBatch != null && _lastBatch.TryGetValue(newString, out var curId))
            {
                return curId;
            }
            if (_nextBatch.TryGetValue(newString, out var curId2))
            {
                return curId2;
            }
#endif
            var val = ++_maxId;
#if NODICTIONARY
            _nextBatch.Add((newString, val));
#else
            _nextBatch.Add(newString, val);
#endif
            if(_nextBatch.Count >= _stringsPerBatch)
            {
                SwitchBatchAndWrite();
            }

            return val;
        }

        public override void Flush()
        {
            SwitchBatchAndWrite();
        }


        public void SwitchBatchAndWrite()
        {
            _lastBatch = _nextBatch;
            //should do this asynchronously ... 
            
                
#if NODICTIONARY
            WriteBatch(_lastBatch.Select(a=>new KeyValuePair<string, int>(a.Item1, a.Item2)));
            _nextBatch = new List<(string, int)>();
#else
            WriteBatch(_lastBatch);
            _nextBatch = new Dictionary<string, int>();
#endif
        }

        ////string id map: select s.id, dupes.to_id, s.val, dupes.val from strings s inner join (select min(id) to_id, val from strings group by val) dupes on s.val=dupes.val order by s.id
        public IEnumerable<(long currentId, long targetId, string val)> GetStringMap()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"select s.id, dupes.to_id, s.val from strings s inner join (select min(id) to_id, val from strings group by val) dupes on s.val=dupes.val order by s.id";
                var result = command.ExecuteReader(CommandBehavior.SequentialAccess);
                while (result.Read())
                {
                    yield return ((long)result[0], (long)result[1], result[2] as string);
                }
            }
        }

        public IEnumerable<(long stringId, string val)> GetUniqueStrings()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"select min(id), val from strings group by val order by min(id) asc";
                var result = command.ExecuteReader(CommandBehavior.SequentialAccess);
                while (result.Read())
                {
                    yield return ((long)result[0], result[1] as string);
                }
            }
        }

        private int MaxId()
        {
            return MaxId(DataTableName);
        }

        private void WriteBatch(IEnumerable<KeyValuePair<string, int>> lastBatch)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                var command = _connection.CreateCommand();
                //(id INTEGER, val TEXT)
                command.CommandText = $"INSERT INTO {DataTableName} (id, val) VALUES ($id, $val)";

                var idParam = command.CreateParameter();
                idParam.ParameterName = "$id";
                command.Parameters.Add(idParam);

                var valParam = command.CreateParameter();
                valParam.ParameterName = "$val";
                command.Parameters.Add(valParam);

                foreach(var item in lastBatch)
                {
                    idParam.Value = item.Value;
                    valParam.Value = item.Key;
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }
    }
}
