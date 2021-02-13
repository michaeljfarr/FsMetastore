# Ord - Hierarchical ordering.
This is a discussion on different approaches to apply ordering to the FsMetaDb hierarchy.

## Options
The diff system relies on a lexicographically ordered series of path information from the file
system and the database.  There are a few ways to achieve this:
1. Recursively query the database (possibly via CTE) to discover the order each time.  This approach is
   quite easy but it is just slow to query.
2. Store the path string for each folder.  This approach was excluded from this implementation because
   of the storage cost of the index.  But it would be easy to achieve and easier to maintain.  There are
   alternatives to this that reduce the index size by a factor of 4.
3. Store an integer for each folder that indicates the order.  This approach was selected because it had
   the least storage cost.  For the moment, we have a fairly simple but solution that requires *some* 
   updates to ord values after each re-scan operation completes.

Many systems can store quite deep hierarchies with more than 30 levels of hierarchy and over 1000 characters, eg  
* C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\Extensions\Microsoft\Python\Miniconda\Miniconda3-x64\Lib\site-packages\adodbapi\test\adodbapitest.py
* Older nodejs packages 

#### Theory

Consider the hierarchy in the following table.  The objective is to provide an Ord index that means that is
ordered based on alphabetically ordering of Path - which isn't directly available.  Ideally the values in
this index should be stable when a few folders change.

| ParentId   | Id       | Name  | Path        | 
| -----------| -------- | -----:|     -------:| 
| null       | 1        | root  | /root       |
| 1          | 2        | b     | /root/b     |
| 1          | 5        | c     | /root/c     |
| 1          | 6        | a     | /root/a     |
| 2          | 3        | c     | /root/b/c   |
| 2          | 4        | b     | /root/b/b   |
| 3          | 7        | a     | /root/b/c/a |

One approach would be to recursively loop through the system, incrementing an integer for each item
discovered.  This is a simple approach, but means that you will often be calculating all the indexes
- it would look like this:

| ParentId   | Id       | Name  | Ord  | Path        | 
| -----------| -------- | -----:|-----:|     --------| 
| null       | 1        | root  | 1    | /root       |
| 1          | 6        | root  | 2    | /root/a     |
| 1          | 2        | root  | 3    | /root/b     |
| 2          | 4        | root  | 4    | /root/b/b   |
| 2          | 3        | root  | 5    | /root/b/c   |
| 3          | 7        | root  | 6    | /root/b/c/a |
| 1          | 5        | root  | 7    | /root/c     |

A simple alternative to this is to recursively loop through the system, but this time keep
each item separated by a consistent amount.  This allows items to be added within the hierarchy
without needing to update all of the indexing records in the database. 

In the following example 1000 points are given to each point in the hierarchy.  In theory, we
should be able to add 999 items into any point in the hierarchy without needing to update any of the
other indexes.  


| ParentId   | Id       | Name  | Ord   | Path        | 
| -----------| -------- | -----:|------:|     --------| 
| null       | 1        | root  | 1000  | /root       |
| 1          | 6        | root  | 2000  | /root/a     |
| 1          | 2        | root  | 3000  | /root/b     |
| 2          | 4        | root  | 4000  | /root/b/b   |
| 2          | 3        | root  | 5000  | /root/b/c   |
| 3          | 7        | root  | 6000  | /root/b/c/a |
| 1          | 5        | root  | 7000  | /root/c     |

On the first indexing, files are immediately assigned their Id × OrdPoints.  On subsequent indexes
files are assigned PrevId + MaxOrdOffset / Math.Min(Math.Pow(10, currentGeneration - 1), MaxOrdOffset), but only if that is greater than 
current Ord for that item.  To make this useful on large systems, we would use a 64 bit int and OrdPoints as  
10,000,000 points.This provides the following constraints:
 - Each ord is always at least one more that the previous ord.
 - Ords only need to change if OrdPoints/(Min(GenerationNum, OrdPoints)) files have been added
 - There is a limit of 10^12 of files (which is somewhat beyond what we'd want to store in a sqlite database).  

 
 

