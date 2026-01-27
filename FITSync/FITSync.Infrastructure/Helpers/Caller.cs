using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Infrastructure.Helpers
{
    public class Caller : ICaller
    {
        private int userID;
        public Caller(HttpContext httpContext)
        {
            var userid = httpContext.GetUserID();
            if (userid == null)
                throw new Exception("User id not found");
            this.userID = int.Parse(userid);
        }
        public string UserId => userID.ToString();
    }
}
