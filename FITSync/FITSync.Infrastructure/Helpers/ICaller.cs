using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Infrastructure.Helpers
{
    public interface ICaller
    {
        public string UserId { get; }
        public string ToString();
    }
}
