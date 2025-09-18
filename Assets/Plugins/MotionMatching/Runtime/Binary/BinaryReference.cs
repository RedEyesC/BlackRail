using System;

namespace MotionMatching
{
    public struct BinaryReference
    {
        internal string FilePath;

        internal BlobAssetReference<Binary> LoadBinary()
        {
            string binaryFilePath = FilePath;

            if (BlobFile.Exists(binaryFilePath))
            {
                int fileVersion = BlobFile.ReadBlobAssetVersion(binaryFilePath);
                if (fileVersion != Binary.s_CodeVersion)
                {
                    throw new ArgumentException(
                        $"Binary '{binaryFilePath}' is outdated, its version is {fileVersion} whereas Kinematica version is {Binary.s_CodeVersion}. Rebuild the asset to update the binary."
                    );
                }

                return BlobFile.ReadBlobAsset<Binary>(binaryFilePath);
            }
            else
            {
                throw new ArgumentException($"Invalid binary reference to an non-existing Kinematica Asset");
            }
        }
    }
}
