using System.IO;
using FsMetastore.Model.Items;

namespace FsMetastore.Persistence.Zipper.Model
{
    public class DriveZipperItem : IZipperItem
    {
        public DriveZipperItem(DriveInfo driveInfo, DriveMeta driveMeta)
        {
            DriveInfo = driveInfo;
            DriveMeta = driveMeta;
        }

        public DriveInfo DriveInfo { get; }
        public DriveMeta DriveMeta { get; }
    }
}