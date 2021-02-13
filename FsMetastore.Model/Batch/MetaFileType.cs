namespace FsMetastore.Model.Batch
{
    /// <summary>
    /// These are the types of files that might be stored in a batch.  
    /// </summary>
    /// <remarks>
    /// The most commonly used types of file are Source and ImportDb
    /// </remarks>
    public enum MetaFileType
    {
        Unset,
        Source,
        Folders,
        Files,
        /// <summary>
        /// Since there are typically a small number of owners and the structure of information has many reasons to vary, we just use
        /// json to format this file.  
        /// </summary>
        /// <remarks>
        /// The objective of this system is to be able to globally identify a user or groups, so user or group information that is
        /// purely local to a particular machine is often not stored.  Also, user bob on one machine may be the same person as
        /// RobertWagner on another machine - but then again we need to be careful to avoid connecting "Robert Wagner" the actor with
        /// "Robert Wagner" the doctor.
        ///
        /// Centrally the batch owners (and groups) are represented relative to the drive rather than the machine.  We do not use the
        /// machine because drives can too easily move between computers.  There is the possibility that this will lead to conflict
        /// (ie user1 on drive1 is treated as 'Bob' on machine1, but 'Robert' on machine2), but generally these things are resolvable
        /// on the central server where the stitching is performed.
        ///
        /// Users defined are identified within the batch with a 32bit int like everything else, but when processed centrally they
        /// are identified as:
        ///   [Drive Guid]/[UserName]
        /// 
        /// There is some variation between Linux and Windows systems.  The main difference is that the concept of ownership works
        /// quite differently on windows.  To simplify data collection on windows we capture the owner for files based on the
        /// whether or not the file is in the users directory or not.  Generally we try to store the following 3 things as a miniumum:
        ///   - User Name
        ///   - Full Name
        ///   - home directory
        /// </remarks>
        BatchOwners,
        /// <summary>
        /// Group information is generally fairly hard to track on windows and so for the moment we do not bother.
        /// </summary>
        BatchGroups,
        /// <summary>
        /// A sqlite file containing the names of each file/folder, referred to by a 32bit id from.
        /// </summary>
        /// <remarks>
        /// Sqlite can be used to detect and remove duplicates in file names.
        /// </remarks>
        SqliteNames,
        /// <summary>
        /// A file with equivalent information to SqliteNames, but written in variable length binary encoded form sorted by id.
        /// The strings are stored as a 7bit encoded length followed by a string encoded with the text encoding of the batch.
        /// </summary>
        IndexedNames,
        /// <summary>
        /// An ordered fixed length file structure containing the absolute positions of a string within SqliteNames
        /// </summary>
        IndexedNamesIndex,
        /// <summary>
        /// This file would be used where there was more than Int32.Max of name data.
        /// The first IndexedNamesIndex is a fixed length format based on a 32bit reference into the IndexedNames.
        /// We havent worked out exactly how to do this and it doesn't seem like a priority right now.  However
        /// in the very rare case whereever this happens we would probably just use a 64bit reference in a second file
        /// </summary>
        IndexedNamesOverflow,
        /// <summary>
        /// This is the index for IndexedNamesOverflow
        /// </summary>
        IndexedNamesOverflowIndex,
        /// <summary>
        /// This is the Sha256 of a file, stored as an ordered fixed length structure.
        ///     [FileId][Date][Sha256]
        /// </summary>
        FileSha256,
        /// <summary>
        /// This is a Sqlite database that stores File and Folder metadata.
        /// </summary>
        FileMetaDb
    };
}