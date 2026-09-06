using System;
using System.Collections.Generic;

namespace Application.Common
{
    public class IdentityOperationResult
    {
        public bool Succeeded { get; set; }
        public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
    }
}
