namespace FsMetastore.Persistence.Factories
{
    public class FsMetaDbOptions
    {
        public FsMetaDbType DbType { get; set; } 
        public string ConnectionString { get; set; }
    }
}