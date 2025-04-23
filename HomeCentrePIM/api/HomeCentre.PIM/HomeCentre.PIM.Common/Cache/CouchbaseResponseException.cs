using System;
using System.Runtime.Serialization;

namespace HomeCentre.PIM.Common.Cache
{
    [Serializable]
    internal class CouchbaseResponseException : Exception
    {
        public CouchbaseResponseException()
        {
        }

        public CouchbaseResponseException(string message)
            : base(message)
        {
        }

        public CouchbaseResponseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CouchbaseResponseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}