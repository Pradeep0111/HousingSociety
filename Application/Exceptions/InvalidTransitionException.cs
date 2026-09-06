using System;

namespace Application.Exceptions
{
    public class InvalidTransitionException : Exception
    {
        public InvalidTransitionException(string message) : base(message) { }
    }
}
