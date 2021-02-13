FsScanStream format.
================

* Each batch can contain up to 6 files:
    *  Source (Mandatory - Json):
        * Machine Name
        * Date
        * Encoding (Utf8/Utf16)
        * Name Storage Strategy
            * Flexible (Each name is preceeded by a byte that indicates the storage mode.)
            * Fixed (Every name is stored in the same way.)
        * Drives: 
            * GoldId: 
                * A Guid that defines the drive.
            * RootId: 
                * An n byte integer that represents the root folder of the drive.
            * MountPoint
                * A string indicating the mount point of the drive within the system.
    *  Folders (Mandatory): The folder names and metadata associated with the the folders.
        * Id: 
            * An n byte integer that represents the folder within this batch.
        * Name 
            * Potentially a "string ref", see notes below.
        * Mask: (A byte bitmask indicating which of the remaining values to expect)
        * SurrogateId: 
            * If the source file system knows it.
        * ParentId
        * Modified Date
        * Permission Mask
        * Owner Id
        * Group Id
    *  Files (Mandatory): The file names and basic metadata associated with each file
        * Id: (An n byte integer that represents the folder within this batch.)
        * Name 
            * Potentially a "string ref", see notes below.
        * Mask: (A byte bitmask indicating which of the remaining values to expect)
        * SurrogateId: (If the source file system knows it.)
        * Modified Date
        * File Length
        * Permission Mask
        * Owner Id
        * Group Id
    *  Owners (Optional)
        * Id: (An n byte integer that represents the owner within this batch.)
        * SurrogateId: (If the source file system knows it.)
        * Name 
    *  Strings (Optional): 
        * Id: (An n byte integer that represents the string within this batch.)
        * Length: A 2 byte length (max length: 65,535)
        * Value: A string encoded with the system encoding
    *  Metadata (Optional) - 
        * Type: (either file or folder)
        * Id: The id of the object referred to in this batch.
        * KeyValueArray: An array of key value pairs that represent the extra metadata
            * Keys and values are rendered as Names


The system has a facility for storing names in a separate file.  This only makes sense when there are a high proportion of 
files with the same name, and this feature isn't supported by FsMetaDb.  The NameStorageStrategy options are:
* __LocalString__.  This will be the most common option - and results in the filename being stored with the other metadata. 
* __StringRef__. The names of files and folders is stored in a separate file with a reference from the metadata stream.    
