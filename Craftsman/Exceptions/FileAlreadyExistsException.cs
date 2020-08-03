﻿namespace Craftsman.Exceptions
{
    using System;

    [Serializable]
    class FileAlreadyExistsException : Exception
    {
        public FileAlreadyExistsException() : base($"This file already exists.")
        {

        }

        public FileAlreadyExistsException(string file) : base($"The file `{file}` already exists.")
        {

        }
    }
}
