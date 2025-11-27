/*
    GtaImg - A library for reading and manipulating GTA IMG archive files.
    Copyright (c) 2025 Vaibhav Pandey <contact@vaibhavpandey.com>

    Licensed under the MIT License. See LICENSE file in the project root for full license information.
*/

using System;

namespace GtaImg
{
    /// <summary>
    /// Exception thrown by IMGArchive when an error occurs.
    /// </summary>
    public class IMGException : Exception
    {
        public string SourceFile { get; private set; }
        public int SourceLine { get; private set; }

        public IMGException(string message)
            : base(message)
        {
            SourceFile = null;
            SourceLine = -1;
        }

        public IMGException(string message, string sourceFile, int sourceLine)
            : base(message)
        {
            SourceFile = sourceFile;
            SourceLine = sourceLine;
        }

        public IMGException(string message, Exception innerException)
            : base(message, innerException)
        {
            SourceFile = null;
            SourceLine = -1;
        }

        public IMGException(string message, string sourceFile, int sourceLine, Exception innerException)
            : base(message, innerException)
        {
            SourceFile = sourceFile;
            SourceLine = sourceLine;
        }
    }
}
