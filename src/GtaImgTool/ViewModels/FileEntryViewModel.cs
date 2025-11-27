/*
    GtaImgTool - A GUI tool for viewing and editing GTA IMG archive files.
    Copyright (c) 2025 Vaibhav Pandey <contact@vaibhavpandey.com>

    Licensed under the MIT License. See LICENSE file in the project root for full license information.
*/

using GtaImg;

namespace GtaImgTool.ViewModels
{
    public class FileEntryViewModel : ViewModelBase
    {
        private readonly IMGEntry _entry;

        public FileEntryViewModel(IMGEntry entry)
        {
            _entry = entry;
        }

        public string Name => _entry.Name;
        public uint Offset => _entry.Offset;
        public uint SizeBlocks => _entry.Size;
        public long SizeBytes => _entry.SizeInBytes;

        public string SizeFormatted
        {
            get
            {
                var bytes = SizeBytes;
                if (bytes >= 1024 * 1024)
                    return $"{bytes / (1024.0 * 1024.0):F2} MB";
                if (bytes >= 1024)
                    return $"{bytes / 1024.0:F2} KB";
                return $"{bytes} B";
            }
        }

        public string FileType
        {
            get
            {
                var ext = System.IO.Path.GetExtension(Name).ToUpperInvariant();
                return ext switch
                {
                    ".DFF" => "3D Model",
                    ".TXD" => "Texture Dictionary",
                    ".COL" => "Collision",
                    ".IFP" => "Animation",
                    ".IPL" => "Item Placement",
                    ".IDE" => "Item Definition",
                    ".DAT" => "Data File",
                    ".CFG" => "Config File",
                    _ => string.IsNullOrEmpty(ext) ? "Unknown" : ext.TrimStart('.')
                };
            }
        }

        public IMGEntry Entry => _entry;
    }
}

