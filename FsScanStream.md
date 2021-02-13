## Storage formats:

### FsScanStream:
The FsScanStream format is a protobuf stream that contains a file hierarchy.  Items in the stream 
provide the same information as would be found when recursively querying a file system.  By 
exchanging FsScanStreams, multiple systems can keep accurate records of a file system. The stream 
can contain:
 - All folders/files in a file system.
   - Or any subset of folders, provided that there all items are connected to the root folder.
 - Folders/files that have changed since the previous scan (and all the parent folders of that file).

As an additional constraint to simplify change detection:
 * Items are stored alphabetically, in a depth first hierarchy.
 * Files and folders are currently stored in separate files, but in retrospect this may have been a 
   poor decision.

FsScanStreams is only suited for reading in a forward only manner.  However, some limited facilities 
are included to index FsScanStream files to perform simple lookups.
 - Id => Meta
 - PathString => Id (via CityHash)

A FsScanStream folder contains FsScanStream files (containing file metadata) plus extra information in
Json that is required to interpret the FsScanStream.  In total the folder contains:
 - The FsScanStream files (protobuf)
   - (as described above)
 - A BatchSource file (json)
   - Origin information (machine, mount point)
   - Date of last scan.
   - Generation 

### FsMetaDb:
FsMetaDb is based on Sqlite, it contains the same data fields as FsScanStream and can be 
processed in the same way.  Its main purpose is to keep track of changes that result from 
repeated file systems scans. FsScanStream can be extracted from FsMetaDb based on the id
of the scan (Generation).

FsMetaDb features:
* Supports repeated file system scans and keeps track of which scan a file last changed in (with 2 more columns than FsScanStream)
  * CreatedGeneration
  * ModifiedGeneration
* The scanner allows FsMetaDb to be updated while exporting DiffDB format files
* If a folder is deleted, we only track the delete event on the folder - not the descendent children.
* Does not use foreign keys.

### Comparative FsMetaDb vs FsScanStream
* FsMetaDb has a significant minimum size (a few entries still requires 28kb+)
* Large FsMetaDb require almost twice as much storage as FsScanStream.
  * Eg.  With 8000 files FsMetaDb required 860kb vs 453kb of FsScanStream.
* FsScanStream can be read in memory as a stream, whereas FsMetaDb needs to be written to disk first.
* FsMetaDb requires an extra "CreatedGeneration" column so allow change detection.
  * We mostly replaced this with an in memory list, but fall back to CreatedGeneration when limits are exceeded.
* FsMetaDb requires an extra 64 bit int "Ord" column so values can easily be read out in order.
* Indexing of FsMetaDb is more flexible

### Ord - Hierarchical ordering.
The diff system relies on a lexicographically ordered series of path information from the file
system and the database.  See [Ord - Hierarchical ordering](Ord.md) for a discussion on the 
options and current solutions.

## Performance Stats

### First Load
Performance Info on a XPS 9550 Dell laptop running windows 10 with NTFS and 2,570,133 files:
  - Bare file system: 100s first run, 90s subsequent (+- 5 seconds)
  - StringRef (optimized + memory index): 50sec read, 20sec write overhead, 95.8MB/15MB (uncompressed/7z)
  - LocalString: 20sec read, 5sec write overhead, 122MB/18.6MB (uncompressed/7z)
  - PerItem: 20sec read, 8sec write overhead, 125MB/18.9MB (uncompressed/7z)
  - Optimized LocalString read: 10.2sec (209 files per ms)

### Metrics
Note: The timings here are very sensitive to file performance, so have an error margin of 50%.  

| Task             | Db Type        | Time     | Rate           | Size      | 
| --------------   | -------------- |    -----:|          -----:|     -----:| 
| Read FS 2.4M     | N/A            |  1.74min | 23.13 files/ms | 
| Load 2.4M Files  | FsScanStream     | 96.22sec | 25.05 files/ms | 132.57MB  |
|                  | FsMeta DB    |  2.08min | 19.29 files/ms | 263.23MB
| Diff 2.4M Files  | FsScanStream     |  2.07min | 19.45 files/ms | 1261B
|  (192 change)    | FsMeta DB    |  2.58min | 15.54 files/ms | 44.57KB
| Load 864 Files   | FsScanStream     | 115.06ms | 7.51 files/ms  | 60.01KB
| Diff 864 Files   | FsScanStream     | 106.88ms | 8.08 files/ms  | 937B
| Diff 864 Files   | FsMeta DB    | 107.99ms | 8.00 files/ms  | 28.56KB

See also [compact_filemetadata](FileMetaBatching.Model/compact_filemetadata.md)



