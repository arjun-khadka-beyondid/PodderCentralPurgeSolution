using System;
using System.Collections.Generic;
using System.Text;

namespace PodderCentral.PurgeData
{
    public class DBSecretCredential
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Engine { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string DbClusterIdentifier { get; set; }
        public string DbName { get; set; }
    }
}
