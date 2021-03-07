using System.Data.HashFunction;
using System.Data.HashFunction.CityHash;
using FsMetastore.Model.Batch;
using FsMetastore.Model.Protobuf;
using FsMetastore.Persistence.Factories;
using FsMetastore.Persistence.IndexedData;
using FsMetastore.Persistence.IO.Commands;
using FsMetastore.Persistence.IO.FileBatches;
using FsMetastore.Persistence.IO.FsMetaDb;
using FsMetastore.Persistence.IO.FsScanStream;
using FsMetastore.Persistence.IO.Test;
using FsMetastore.Persistence.Meta;
using FsMetastore.Persistence.PathHash;
using FsMetastore.Persistence.Zipper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FsMetastore.Persistence.IOC
{
    public static class ServiceProviderExtensions
    {
        public static IServiceCollection AddSqliteFsMetastore(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.Configure<FsMetaDbOptions>(configuration.GetSection("FsMetaDb"));
            serviceCollection.AddSingleton<IFsMetaDbContextFactory, SqliteFsMetaDbContextFactory>();
            serviceCollection.AddSingleton<IFsMetaDbContextFactoryBuilder, SqliteFsMetaDbContextFactoryBuilder>();
            return serviceCollection;
        }
        
        public static IServiceCollection AddFsMetastore(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IBatchCommandService, BatchCommandService>();
            serviceCollection.AddSingleton<IBatchScopeFactory, BatchScopeFactory>();
            //serviceCollection.AddScoped<BatchScopeProvider>();
            
                
            serviceCollection.TryAddScoped<IScanDbBatchIOFactory, FsScanStreamBatchIOFactory>();
            serviceCollection.TryAddScoped<BatchIOConfig>();
            serviceCollection.TryAddScoped<BatchStorageRules>();
            
            serviceCollection.TryAddSingleton<ITestOutputer, NullTestOutputer>();
            
            //serviceCollection.AddScoped<BatchState>();
            //serviceCollection.AddScoped<IBatchStorageRulesProvider>(a=>a.GetRequiredService<BatchState>());
            
            serviceCollection.TryAddScoped<IMetaBatchReader, MetaBatchReader>();
            //serviceCollection.TryAddScoped<IFileMetaSpider, FileMetaSpider>();
            serviceCollection.TryAddScoped<IMetaFactory, MetaFactory>();
            
            DateTimeOffsetSurrogate.Register();
            serviceCollection.TryAddScoped<IMetaSerializer, ProtobufMetaSerializer>();
            //serviceCollection.TryAddScoped<IMetaSerializer, CustomMetaSerializer>();
            
            
            serviceCollection.TryAddScoped<BatchSourceProvider>();
            serviceCollection.TryAddScoped<IBatchSourceProvider>(a=>a.GetRequiredService<BatchSourceProvider>());
            serviceCollection.TryAddScoped<IPathStringComparerProvider>(a=>a.GetRequiredService<BatchSourceProvider>());

            //These services read/write either to ScanDb (custom binary) or ImportDb (sqlite) based storage
            //ScanDb can only store a single scan generation, it write-once format and can't be extended.
            //ImportDb is a multi generation format, and allows records to be added/soft deleted.
            serviceCollection.TryAddScoped<FsMetaDbPersister>();
            serviceCollection.TryAddScoped<FsMetaDbMetaReader>();
            serviceCollection.TryAddScoped<IFsMetaDbContext, FsMetaDbContext>();
            serviceCollection.TryAddScoped<MetaStreamPersister>();
            serviceCollection.TryAddScoped<MetaStreamReader>();
            serviceCollection.TryAddScoped<NoopTimerMetaPersister>();
            serviceCollection.TryAddScoped<MetaStreamPlusDbReader>();
            
            serviceCollection.TryAddScoped<FileSystemZipEnumerableFactory>();
            

            

            //currently the container configuration doesn't support injection of some types, and instead we 
            //use manual factories.
            //IMetaReader can't be injected because the instance class is determined based on a rule outside the container
            //IMetaEnumerator can't be injected because it depends on IMetaReader;

            serviceCollection.TryAddScoped<StringRefOptimizer>();

            serviceCollection.TryAddScoped<IStringRefWriter, StringRefWriter>();

            serviceCollection.TryAddScoped<IIndexedDataFileReader, IndexedDataFileReader>();
            serviceCollection.TryAddScoped<IIndexedDataFileWriter, IndexedDataFileWriter>();
            serviceCollection.TryAddScoped<IPathIndexCreator, PathIndexCreator>();

            var cityHash = CityHashFactory.Instance.Create(new CityHashConfig(){
                HashSizeInBits = 64
            });
            serviceCollection.TryAddSingleton<IHashFunction>(cityHash);
            serviceCollection.TryAddSingleton<IPathHashCalculator, PathHashCalculator>();
            
            serviceCollection.TryAddScoped<IPathIndexWriter, SqlitePathIndexWriter>();
            
            return serviceCollection;
        }
    }
}
