Essentually the purpose of this library is to allow the file metadata to be recorded from one system and sent to another. 
The objective is to be as space efficient as possible, whilst 
Initially the meta data includes the bare minimum from the file system:
 - name (full path)
 - length
 - last modified date
 - owner/group
 However, we suppliment other information and what that
 - surrogate id
 - data classifications (why is it important, quality ratings etc)
 - Sha256
 - application metadata (song title etc)

 Given the existance of things like the tar format, why is another format required?

  - Data Volume
     - We just want the metadata about the files, not the file data itself.
     - Just my home directory has over 1 million files in it and that corresponds to about 60M (12MB compressed) of 
       metadata data, but with TAR and other systems it is several times that amount even if we skip the file storage part.
     - Since we want to snapshot changes in the file system over time, the storage volume of this format is particularly 
       important. We can also use this format to just store the changes in metadata since the last snapshot.
  - Extensibility
     We want to be able to add metadata on top of what the file system already has such as data classification and 
     application metadata.
  
What are these string refs for?

The string refs aren't really saving much from a storage or transfer point of view, and sometimes they are more trouble than 
they are worth.  For that reason, the system is can work without stringrefs if there isn't a good reason in the use case. 
In particular if all the data is going into an RDBMS anyway, it almost certainly doesn't make sense. There are two scenarios where 
stringrefs can be a small advantage - although the general advice is not to use them unless you really need it.

Ignored Files

One example of that is the files in the git objects have a 40 byte name that is sometimes not used at all by 
downstream systems.  It is interesting to know how much data is in the folder and when it was last updated, but 
the names itself isn't useful. It is just simpler to capture all the information that we can, and filter it out 
later.  But since the file format is sequential, we can often improve our read performance a lot if we store a 
4 byte identifier instead of that 40byte name (offset by the read penalty for the filenames we do need).

Duplicate File Names

Some systems will have a large percentage of duplicate file names.  This commonly happens where many applications have the 
same name configuration files or the same dll's in their folder structure.  There are two reasons that we care about this:
1. Duplicate names are an important factor in file classification.  So, the work done to identify the name sharing between files
is sometimes useful for some downstream processing.
2. In some cases we save about 15% transfer size by using this structure.  However, this comes at the cost of more complexity
and reduced performance on on both read and write sides.

Other Notes

Read Performance

While it isnt one of the core objectives of the file format, there are some advantages to storing it this way.
     - Reading file metadata directly from the file system can be slow, reading the metadata from this format 
       is typically 4 times faster on windows (possibly not a great benchmark).  But depending on whether the
       os has the fs information cached (and other factors) this can be an even greater performance advantage
       in reading from this format instead.
