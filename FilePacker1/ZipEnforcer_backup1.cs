using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace ZipBuilder
{
    public class ZipEnforcer
    {
        static private UInt32 Get32(byte[] data, int i)
        {
            return (UInt32)data[i] | ((UInt32)data[i+1]) << 8 | ((UInt32)data[i+2]) << 16 | ((UInt32)data[i + 3]) << 24;
        }

        static private UInt64 Get64(byte[] data, int i)
        {
            return (UInt64)data[i] | 
                ((UInt64)data[i + 1]) << 8 | 
                ((UInt64)data[i + 2]) << 16 | 
                ((UInt64)data[i+3]) << 24 | 
                ((UInt64)data[i+4]) << 32 | 
                ((UInt64)data[i+5]) << 40 | 
                ((UInt64)data[i+6]) << 48 | 
                ((UInt64)data[i + 7]) << 56;
        }

        static private UInt16 Get16(byte[] data , int i)
        {
            return (UInt16)((UInt32)data[i] | ((UInt32)data[i + 1]) << 8);
        }

        static private void Put16(UInt16 v, byte[] data, int i) {
            data[i] = (byte)((UInt32)v & 0xffu);
            data[i + 1] = (byte)(((UInt32)v >> 8) & 0xffu);
        }

        static private void Put32(UInt32 v, byte[] data, int i)
        {
            data[i] = (byte)(v & 0xffu);
            data[i+1] = (byte)((v >> 8) & 0xffu);
            data[i + 2] = (byte)((v >> 16) & 0xffu);
            data[i + 3] = (byte)((v >> 24) & 0xffu);
        }

        static private void Put64(UInt64 v, byte[] data, int i)
        {
            data[i] = (byte)(v & 0xfful);
            data[i+1] = (byte)((v >> 8) & 0xfful);
            data[i + 2] = (byte)((v >> 16) & 0xfful);
            data[i + 3] = (byte)((v >> 24) & 0xfful);
            data[i + 4] = (byte)((v >> 32) & 0xfful);
            data[i + 5] = (byte)((v >> 40) & 0xfful);
            data[i + 6] = (byte)((v >> 48) & 0xfful);
            data[i + 7] = (byte)((v >> 56) & 0xfful);
        }

        private static readonly byte[] LOCAL_HEADER_SIGNATURE = { 0x50, 0x4b, 0x03, 0x04 };
        private static readonly byte[] CENTRAL_DIR_HEADER_SIGNATURE = { 0x50, 0x4b, 0x01, 0x02 };
        private static readonly byte[] ZIP64_END_CENTRAL_DIR_RECORD_SIGNATURE = { 0x50, 0x4b, 0x06, 0x06 };
        private static readonly byte[] ZIP64_END_CENTRAL_DIR_LOCATOR_SIGNATURE = { 0x50, 0x4b, 0x06, 0x07 };
        private static readonly byte[] END_OF_CENTRAL_DIR_SIGNATURE = { 0x50, 0x4b, 0x05, 0x06 };
        private static readonly byte[] ZIP64_EXTENDED_INFO_EXTRA_FIELD_ID = { 0x00, 0x01 };

        private static readonly byte[] ALL_ONES_FOUR_BYTES = { 0xff, 0xff, 0xff, 0xff };
        private static readonly byte[] ALL_ZEROS_FOUR_BYTES = { 0x00, 0x00, 0x00, 0x00 };

        private static readonly byte[] OFFSET_START_CENTRAL_DIR_IN_END_CENTRAL_DIR_EXCEED_32 = ALL_ONES_FOUR_BYTES;
        private static readonly byte[] GENERAL_PURPOSE_BIT_FLAG_ENCRYPTION_BIT_MASK = { 0x00, 0x01 };
        private static readonly byte[] GENERAL_PURPOSE_BIT_FLAG_DATA_DESCRIPTOR_PRESENT_BIT_MASK = { 0x00, 0x08 };
        private static readonly byte[] GENERAL_PURPOSE_BIT_FLAG_RESERVED_ENHANCED_DEFLATING_BIT_MASK = { 0x00, 0x10 };

        private static readonly int SIGNATURE_SIZE = 4;
        
        private static readonly int DIGITAL_SIGNATURE_AFTER_CENTRAL_DIR_SECTION_SIZE = 0;
        
        private static readonly int ZIP64_END_OF_CENTRAL_DIRECTORY_RECORD_NO_EXTENSIBLE_DATA_SECTION_SIZE = 56;
        
        private static readonly int ZIP64_END_OF_CENTRAL_DIRECTORY_LOCATOR_SECTION_SIZE = 20;
        
        private static readonly int END_OF_CENTRAL_DIRECTORY_RECORD_NO_COMMENT_SECTION_SIZE = 22;
        
        private static readonly int END_OF_CENTRAL_DIRECTORY_RECORD_ZIP_FILE_COMMENT_MAX_SIZE = 65535;
        
        private static readonly int END_OF_CENTRAL_DIRECTORY_RECORD_SECTION_MAX_SIZE = 
            END_OF_CENTRAL_DIRECTORY_RECORD_NO_COMMENT_SECTION_SIZE +
            END_OF_CENTRAL_DIRECTORY_RECORD_ZIP_FILE_COMMENT_MAX_SIZE;
        
        private static readonly int DATA_DESCRIPTOR_SECTION_SIZE = 20;
        
        private static readonly int ARCHIVE_DECRYPTION_HEADER_SECTION_SIZE = 0;
        
        private static readonly int ARCHIVE_EXTRA_DATA_RECORD_SECTION_SIZE = 0;
        
        private static readonly int TWO_END_SECTION_SIZE_SUM = 
            ZIP64_END_OF_CENTRAL_DIRECTORY_LOCATOR_SECTION_SIZE +
            END_OF_CENTRAL_DIRECTORY_RECORD_NO_COMMENT_SECTION_SIZE;

        private static readonly int THREE_END_SECTION_SIZE_SUM =
            ZIP64_END_OF_CENTRAL_DIRECTORY_RECORD_NO_EXTENSIBLE_DATA_SECTION_SIZE +
            ZIP64_END_OF_CENTRAL_DIRECTORY_LOCATOR_SECTION_SIZE +
            END_OF_CENTRAL_DIRECTORY_RECORD_NO_COMMENT_SECTION_SIZE;

        internal class ExtraField
        {
            public UInt16 id;
            public UInt16 size;
        }

        internal class Zip64ExtendedInformationExtraField : ExtraField
        {
            public UInt64 uncompressedSize;
            public UInt64 compressedSize;
            public UInt64 offsetOfLocalHeader;
        }

        internal class DataDescriptor
        {
            public UInt32 crc32;
            public UInt64 compressedSize;
            public UInt64 uncompressedSize;
        }

        internal class CentralDirectoryHeader
        {
            public UInt64 thisSectionSize;
            public UInt16 versionMadeBy;
            public UInt16 versionNeededToExtract;
            public UInt16 generalPurposeBitFlag;
            public UInt16 compressionMethod;
            public UInt16 lastModTime;
            public UInt16 lastModDate;
            public byte[] crc32 = new byte[4];
            public UInt32 compressedSize;
            public UInt32 uncompressedSize;
            public UInt16 filenameLength;
            public UInt16 extraFieldLength;
            public UInt16 fileCommentLength;
            public UInt16 diskNumberStart;
            public UInt16 internalFileAttributes;
            public UInt32 exteranlFileAttributes;
            public UInt32 offsetOfLocalHeader;
            public char[] filename;
            public List<ExtraField> extraFields;
        }

        internal class LocalHeader
        {
            public UInt16 versionNeededToExtract;
            public UInt16 generalPurposeBitFlag;
            public UInt16 compressionMethod;
            public UInt16 lastModTime;
            public UInt16 lastModDate;
            public byte[] crc32 = new byte[4];
            public UInt32 compressedSize;
            public UInt32 uncompressedSize;
            public UInt16 filenameLength;
            public UInt16 extraFieldLength;
            public char[] filename;
            public ExtraField[] extraFields;
        }

        internal class LocalFile
        {
            public LocalHeader localHeader;
            public DataDescriptor dataDescriptor;
        }

        internal class EndOfCentralDir
        {
            public UInt32 thisSectionSize;
            public UInt16 numberOfThisDisk;
            public UInt16 numberOfDiskWithStart;
            public UInt16 totalNumberOfEntriesInCentralDirOnThisDisk;
            public UInt16 totalNumberOfEntriesInCentralDir;
            public UInt32 centralDirSize;
            public UInt32 offsetOfStartOfCentralDir;
            public UInt16 zipFileCommentLength;
            //public byte[] zipFileComment;
        }

        internal class Zip64EndOfCentralDirLocator
        {
            public UInt32 numberOfDiskWithStart;
            public UInt64 offsetOfZip64EndOfCentralDirRecord;
            public UInt32 totalNumberOfDisk;
        }

        internal class Zip64EndOfCentralDirRecord
        {
            public UInt64 thisSectionSize;

            public UInt64 LengthAfterSecondField;
            public UInt16 versionMadeBy;
            public UInt16 versionNeeded;
            public UInt32 numberOfThisDisk;
            public UInt32 numberOfDiskWithStart;
            public UInt64 totalNumberOfEntriesInCentralDirOnThisDisk;
            public UInt64 totalNumberOfEntriesInCentralDir;
            public UInt64 centralDirSize;
            public UInt64 offsetOfStartOfCentralDir;
        }

        static private int findEndOfCentralDirSignature(byte[] buffer)
        {
            int sz = buffer.Length;
            if (sz < END_OF_CENTRAL_DIRECTORY_RECORD_NO_COMMENT_SECTION_SIZE)
            {
                throw new ArgumentException("buffer size less than minimum end of central dir section size");
            }
            int index = sz - END_OF_CENTRAL_DIRECTORY_RECORD_NO_COMMENT_SECTION_SIZE;
            //byte[] sig = new byte[SIGNATURE_SIZE];
            //Array.Copy(buffer, index, sig, 0, 4);
            bool isMatch = true;
            for (int i = 0; i < SIGNATURE_SIZE; ++i)
            {
                if (buffer[index + i] != END_OF_CENTRAL_DIR_SIGNATURE[i])
                {
                    isMatch = false;
                    break;
                }
            }
            if (isMatch) {
                return index;
            }

            for (; index >= 0; --index) {
                isMatch = true;
                for (int i = 0; i <= SIGNATURE_SIZE; ++i) {
                    if (buffer[index + i] != END_OF_CENTRAL_DIR_SIGNATURE[i])
                    {
                        isMatch = false;
                        break;
                    }
                }
                if (isMatch)
                {
                    return index;
                }
            }

            return index;
        }

        static private void fillEndOfCentralDir(byte[] buffer, int index, EndOfCentralDir endOfCentralDir)
        {
            index += SIGNATURE_SIZE;
            endOfCentralDir.numberOfThisDisk = Get16(buffer, index);
            index += 2;
            endOfCentralDir.numberOfDiskWithStart = Get16(buffer, index);
            index += 2;
            endOfCentralDir.totalNumberOfEntriesInCentralDirOnThisDisk = Get16(buffer, index);
            index += 2;
            endOfCentralDir.totalNumberOfEntriesInCentralDir = Get16(buffer, index);
            index += 2;
            endOfCentralDir.centralDirSize = Get32(buffer, index);
            index += 4;
            endOfCentralDir.offsetOfStartOfCentralDir = Get32(buffer, index);
            index += 4;
            endOfCentralDir.zipFileCommentLength = Get16(buffer, index);
            endOfCentralDir.thisSectionSize = (UInt32)END_OF_CENTRAL_DIRECTORY_RECORD_NO_COMMENT_SECTION_SIZE + (UInt32)endOfCentralDir.zipFileCommentLength;
        }

        static private int parseEndOfCentralDir(byte[] buffer, EndOfCentralDir endOfCentralDir)
        {
            int index = findEndOfCentralDirSignature(buffer);
            if (index >= 0)
            {
                fillEndOfCentralDir(buffer, index, endOfCentralDir);
                return 0;
            }
            else
            {
                return -1;
            }
        }

        


        static private Zip64EndOfCentralDirLocator parseZip64EndOfCentralDirLocator(byte[] buffer, int index)
        {
            Zip64EndOfCentralDirLocator locator = new Zip64EndOfCentralDirLocator();
            bool isMatch = true;
            for (int i = 0; i < SIGNATURE_SIZE; ++i)
            {
                if (buffer[index+i] != ZIP64_END_CENTRAL_DIR_LOCATOR_SIGNATURE[i])
                {
                    isMatch = false;
                    break;
                }
            }
            if (!isMatch)
            {
                throw new Exception("Zip64 End of Central Directory Locator Signature Mismatch");
            }
            index += SIGNATURE_SIZE;
            locator.numberOfDiskWithStart = Get32(buffer, index);
            index += 4;
            locator.offsetOfZip64EndOfCentralDirRecord = Get64(buffer, index);
            index += 8;
            locator.totalNumberOfDisk = Get32(buffer, index);
            return locator;
        }

        static private Zip64EndOfCentralDirRecord parseZip64EndOfCentralDirRecord(byte[] buffer, int index)
        {
            Zip64EndOfCentralDirRecord record = new Zip64EndOfCentralDirRecord();
            bool isMatch = true;
            for (int i = 0; i < SIGNATURE_SIZE; ++i)
            {
                if (buffer[index + i] != ZIP64_END_CENTRAL_DIR_RECORD_SIGNATURE[i])
                {
                    isMatch = false;
                    break;
                }
            }
            if (!isMatch)
            {
                throw new Exception("Zip64 End of Central Directory Record Signature Mismatch");
            }
            index += SIGNATURE_SIZE;
            record.LengthAfterSecondField = Get64(buffer, index);
            index += 8;
            record.versionMadeBy = Get16(buffer, index);
            index += 2;
            record.versionNeeded = Get16(buffer, index);
            index += 2;
            record.numberOfThisDisk = Get32(buffer, index);
            index += 4;
            record.numberOfDiskWithStart = Get32(buffer, index);
            index += 4;
            record.totalNumberOfEntriesInCentralDirOnThisDisk = Get64(buffer, index);
            index += 8;
            record.totalNumberOfEntriesInCentralDir = Get64(buffer, index);
            index += 8;
            record.centralDirSize = Get64(buffer, index);
            index += 8;
            record.offsetOfStartOfCentralDir = Get64(buffer, index);

            record.thisSectionSize = record.LengthAfterSecondField + 12;

            return record;
        }


        
        static private CentralDirectoryHeader ParseOneCentralDirectoryHeader(byte[] buffer, int index)
        {
            CentralDirectoryHeader header = new CentralDirectoryHeader();
            bool isMatch = true;
            for (int i = 0; i < SIGNATURE_SIZE; ++i)
            {
                if (buffer[index + i] != CENTRAL_DIR_HEADER_SIGNATURE[i])
                {
                    isMatch = false;
                    break;
                }
            }
            if (!isMatch)
            {
                throw new Exception("Central Directory Header Signature Mismatch");
            }

            index += SIGNATURE_SIZE;
            header.versionMadeBy = Get16(buffer, index);
            index += 2;
            header.versionNeededToExtract = Get16(buffer, index);
            index += 2;
            header.generalPurposeBitFlag = Get16(buffer, index);
            index += 2;
            header.compressionMethod = Get16(buffer, index);
            index += 2;
            header.lastModTime = Get16(buffer, index);
            index += 2;
            header.lastModDate = Get16(buffer, index);
            index += 2;
            header.crc32 = Get32(buffer, index);
            index += 4;
            header.compressedSize = Get32(buffer, index);
            index += 4;
            header.uncompressedSize = Get32(buffer, index);
            index += 4;
            header.filenameLength = Get16(buffer, index);
            index += 2;
            header.extraFieldLength = Get16(buffer, index);
            index += 2;
            header.fileCommentLength = Get16(buffer, index);
            index += 2;
            header.diskNumberStart = Get16(buffer, index);
            index += 2;
            header.internalFileAttributes = Get16(buffer, index);
            index += 2;
            header.exteranlFileAttributes = Get32(buffer, index);
            index += 4;
            header.offsetOfLocalHeader = Get32(buffer, index);
            index += 4;

            if (header.extraFieldLength > 0)
            {
                index += header.filenameLength;
                List<ExtraField> extraFields = new List<ExtraField>();
                UInt16 processedExtraFieldBytes = 0;
                while (processedExtraFieldBytes < header.extraFieldLength)
                {
                    UInt16 id = Get16(buffer, index);
                    index += 2;
                    processedExtraFieldBytes += 2;
                    switch (id)
                    {
                        case (UInt16)0x0001:
                            Zip64ExtendedInformationExtraField zip64ExtInfo = new Zip64ExtendedInformationExtraField();
                            UInt16 sz = Get16(buffer, index);
                            index += 2;
                            processedExtraFieldBytes += 2;
                            if (header.uncompressedSize == UInt32.MaxValue && sz >= 8)
                            {
                                zip64ExtInfo.uncompressedSize = Get64(buffer, index);
                                index += 8;
                                sz -= 8;
                                processedExtraFieldBytes += 8;
                            }
                            if (header.compressedSize == UInt32.MaxValue && sz >= 8)
                            {
                                zip64ExtInfo.compressedSize = Get64(buffer, index);
                                index += 8;
                                sz -= 8;
                                processedExtraFieldBytes += 8;
                            }
                            if (header.offsetOfLocalHeader == UInt32.MaxValue && sz >= 8)
                            {
                                zip64ExtInfo.offsetOfLocalHeader = Get64(buffer, index);
                                index += 8;
                                sz -= 8;
                                processedExtraFieldBytes += 8;
                            }
                            extraFields.Add(zip64ExtInfo);
                            break;
                        default:
                            break;
                    }
                }
                header.extraFields = extraFields;
            }
            header.thisSectionSize = (UInt64)46 + header.extraFieldLength + header.filenameLength + header.fileCommentLength;
            return header;
        }

        static private CentralDirectoryHeader[] centralDirHeaders parseCentralDirectory(FileStream fs, UInt64 offsetOfCentralDirStart,  byte[] buffer, UInt64 offsetOfBufferStart)
        {
            const int INCREMENTAL_BLOCK_SIZE = 1024;
            if (offsetOfBufferStart <= offsetOfCentralDirStart)
            {

            }
        }


        static public void setGeneralPurposeBit(string zipFilePath)
        {
            FileInfo fileInfo = new FileInfo(zipFilePath);
            long totalSize = fileInfo.Length;

            using (FileStream fs = new FileStream(zipFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                EndOfCentralDir endOfCentralDir = new EndOfCentralDir();
                byte[] buffer = new byte[1024];
                
                fs.Seek(-1024, SeekOrigin.End);
                int ret = parseEndOfCentralDir(buffer, endOfCentralDir);
                if (ret < 0)
                {
                    buffer = new byte[END_OF_CENTRAL_DIRECTORY_RECORD_SECTION_MAX_SIZE]; // 65535+22 bytes
                    fs.Seek(0 - END_OF_CENTRAL_DIRECTORY_RECORD_SECTION_MAX_SIZE, SeekOrigin.End);
                    ret = parseEndOfCentralDir(buffer, endOfCentralDir);
                    if (ret < 0)
                    {
                        throw new Exception("Fail to find the signature of End of Central Directory Record Section");
                    }
                }
                if (endOfCentralDir.offsetOfStartOfCentralDir == UInt32.MaxValue)
                {
                    // the offset of the start of central directory exceeds 4-bytes unsigned integer
                    // zip64 end of central directory record and locator are rpesent.
                    
                    if (buffer.Length > endOfCentralDir.thisSectionSize && buffer.Length - endOfCentralDir.thisSectionSize >= ZIP64_END_OF_CENTRAL_DIRECTORY_LOCATOR_SECTION_SIZE)
                    {
                        // zip64 end of central dir locator section is in the buffer
                        Zip64EndOfCentralDirLocator locator = parseZip64EndOfCentralDirLocator(buffer, 
                            buffer.Length - (int)endOfCentralDir.thisSectionSize - ZIP64_END_OF_CENTRAL_DIRECTORY_LOCATOR_SECTION_SIZE);

                        // calculate if the start of "Zip64 end of central directory record is in current buffer
                        if ((UInt64)totalSize - locator.offsetOfZip64EndOfCentralDirRecord <= (UInt64)buffer.Length)
                        {
                            Zip64EndOfCentralDirRecord record = parseZip64EndOfCentralDirRecord(buffer, (buffer.Length - (int)((UInt64)totalSize - locator.offsetOfZip64EndOfCentralDirRecord)));
                        }
                        else
                        {
                            // seek again to get Zip64 end of central dir record
                        }
                    }
                    else
                    {
                        // Zip64 End of central dir locator is not completely in the buffer. I just need to read in a little more becasue the locator is fixed 20 bytes
                    }
                }
                else
                {
                    // I can directly seek to the start of central directory
                }
            }
        }
    }
}
