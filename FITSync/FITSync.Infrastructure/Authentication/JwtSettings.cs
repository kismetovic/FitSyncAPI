using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Infrastructure.Authentication
{
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";
        public string? SecretKey { get; init; }
        public string? ValidIssuer { get; init; }
        public string? ValidAudience { get; init; }
        public TimeSpan Expires { get; init; }
    }
}
