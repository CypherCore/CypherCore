using System;

namespace Framework.Web.API
{
    public class ApiRequest<T>
    {
        public uint? SearchId { get; set; }
        public Func<T, bool> SearchFunc { get; set; }
    }
}
