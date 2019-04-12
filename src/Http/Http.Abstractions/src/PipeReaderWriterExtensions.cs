using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace System.IO.Pipelines
{
    public static class PipeReaderWriterExtensions
    {
        public static PipeReader AsPipeReader(this Stream stream)
        {
            return PipeReader.Create(stream);
        }

        public static PipeWriter AsPipeWriter(this Stream stream)
        {
            return PipeWriter.Create(stream);
        }
    }
}
