using System;

namespace CodeKicker.BBCode.Core
{
    [Serializable]
    public class BBCodeParsingException : Exception
    {
        public BBCodeParsingException()
        {
        }
        public BBCodeParsingException(string message)
            : base(message)
        {
        }
    }
}