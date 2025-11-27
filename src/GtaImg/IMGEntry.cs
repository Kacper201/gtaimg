/*
    GtaImg - A library for reading and manipulating GTA IMG archive files.
    Copyright (c) 2025 Vaibhav Pandey <contact@vaibhavpandey.com>

    Licensed under the MIT License. See LICENSE file in the project root for full license information.
*/

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GtaImg
{
    /// <summary>
    /// An entry inside an IMGArchive.
    /// </summary>
    /// <remarks>
    /// The entry stores the offset and size in IMG blocks (2048 bytes each),
    /// and the name of the file (up to 23 characters + null terminator).
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMGEntry
    {
        /// <summary>
        /// Maximum length of entry name (excluding null terminator).
        /// </summary>
        public const int MaxNameLength = 23;

        /// <summary>
        /// Size of entry name field in bytes (including null terminator space).
        /// </summary>
        public const int NameFieldSize = 24;

        /// <summary>
        /// The offset of the entry, in IMG blocks (2048 bytes each).
        /// Measured from the beginning of the IMG file in both VER1 and VER2.
        /// </summary>
        public uint Offset;

        /// <summary>
        /// The size of the entry, in IMG blocks (2048 bytes each).
        /// </summary>
        public uint Size;

        /// <summary>
        /// Raw name bytes (24 bytes, null-terminated).
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NameFieldSize)]
        public byte[] NameBytes;

        /// <summary>
        /// Gets or sets the name of the entry.
        /// </summary>
        public string Name
        {
            get
            {
                if (NameBytes == null)
                    return string.Empty;

                int length = Array.IndexOf(NameBytes, (byte)0);
                if (length < 0)
                    length = NameBytes.Length;

                return Encoding.ASCII.GetString(NameBytes, 0, length);
            }
            set
            {
                if (NameBytes == null)
                    NameBytes = new byte[NameFieldSize];

                Array.Clear(NameBytes, 0, NameFieldSize);

                if (!string.IsNullOrEmpty(value))
                {
                    int copyLen = Math.Min(value.Length, MaxNameLength);
                    Encoding.ASCII.GetBytes(value, 0, copyLen, NameBytes, 0);
                }
            }
        }

        /// <summary>
        /// Gets the size of this entry in bytes.
        /// </summary>
        public long SizeInBytes
        {
            get { return IMGArchive.BlocksToBytes(Size); }
        }

        /// <summary>
        /// Gets the offset of this entry in bytes.
        /// </summary>
        public long OffsetInBytes
        {
            get { return IMGArchive.BlocksToBytes(Offset); }
        }

        /// <summary>
        /// Creates a new IMGEntry with the specified name and size.
        /// </summary>
        /// <param name="name">The entry name.</param>
        /// <param name="sizeInBlocks">The size in IMG blocks.</param>
        public IMGEntry(string name, uint sizeInBlocks)
            : this()
        {
            Offset = 0;
            Size = sizeInBlocks;
            NameBytes = new byte[NameFieldSize];
            Name = name;
        }

        public override string ToString()
        {
            return string.Format("{0} (Offset: {1}, Size: {2} blocks)", Name, Offset, Size);
        }
    }
}
