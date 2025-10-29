// <copyright file="Exceptions.cs" company="Allied Bits Ltd.">
//
// Copyright 2025 Allied Bits Ltd.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>

using System.Globalization;

namespace Tlumach.Base
{
    public class TlumachException : Exception
    {
        public TlumachException()
            : base()
        {
        }

        public TlumachException(string message)
            : base(message)
        {
        }

        public TlumachException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class GenericParserException : TlumachException
    {
        public GenericParserException()
        {
        }

        public GenericParserException(string message) : base(message)
        {
        }

        public GenericParserException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class ParserFileException : GenericParserException
    {
        public string FileName { get; }

        public ParserFileException(string fileName)
            : base()
        {
            FileName = fileName;
        }

        public ParserFileException(string fileName, string message)
            : base(message)
        {
            FileName = fileName;
        }

        public ParserFileException(string fileName, string message, Exception innerException)
            : base(message, innerException)
        {
            FileName = fileName;
        }
    }

    public class ParserLoadException : ParserFileException
    {
        public ParserLoadException(string fileName)
            : base(fileName)
        {
        }

        public ParserLoadException(string fileName, string message)
            : base(fileName, message)
        {
        }

        public ParserLoadException(string fileName, string message, Exception innerException)
            : base(fileName, message, innerException)
        {
        }
    }

    public class ParserConfigException : ParserFileException
    {
        public ParserConfigException(string fileName)
            : base(fileName)
        {
        }

        public ParserConfigException(string fileName, string message)
            : base(fileName, message)
        {
        }

        public ParserConfigException(string fileName, string message, Exception innerException)
            : base(fileName, message, innerException)
        {
        }
    }

    public class TextParseException : GenericParserException
    {
        /// <summary>
        /// The starting position of the text block that could not be parsed.
        /// </summary>
        public int StartPosition { get; }

        /// <summary>
        /// The ending position of the text block that could not be parsed.
        /// </summary>
        public int EndPosition { get; }

        /// <summary>
        /// The line number in which the error occurred.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// The column number in which the error occurred.
        /// </summary>
        public int ColumnNumber { get; }

        public TextParseException(string message, int startPosition, int endPosition, int lineNumber, int columnNumber)
            : base(message)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public TextParseException(string message, int startPosition, int endPosition, int lineNumber, int columnNumber, Exception innerException)
            : base(message, innerException)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }
    }

    public class TextFileParseException : TextParseException
    {
        /// <summary>
        /// The name of the file, parsing of which has failed.
        /// </summary>
        public string FileName { get; }

        public TextFileParseException(string fileName, int startPosition, int endPosition, int lineNumber, int columnNumber)
            : base(string.Empty, startPosition, endPosition, lineNumber, columnNumber)
        {
            FileName = fileName;
        }

        public TextFileParseException(string fileName, string message, int startPosition, int endPosition, int lineNumber, int columnNumber)
            : base(message, startPosition, endPosition, lineNumber, columnNumber)
        {
            FileName = fileName;
        }

        public TextFileParseException(string fileName, string message, int startPosition, int endPosition, int lineNumber, int columnNumber, Exception innerException)
            : base(message, startPosition, endPosition, lineNumber, columnNumber, innerException)
        {
            FileName = fileName;
        }
    }
}