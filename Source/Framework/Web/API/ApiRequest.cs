using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.Web.API
{
    public class ApiRequest<T>
    {
        public uint? SearchId { get; set; }
        public Func<T, bool> SearchFunc { get; set; }
    }
}
