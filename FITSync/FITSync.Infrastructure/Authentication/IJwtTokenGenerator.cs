using FITSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Infrastructure.Authentication
{
    public interface IJwtTokenGenerator
    {
        object GenerateToken(User user);
    }
}
