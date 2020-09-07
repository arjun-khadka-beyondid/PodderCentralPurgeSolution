using System;
using System.Collections.Generic;
using System.Text;

namespace PodderCentral.PurgeData
{
    public class EnvironmentSetting
    {
        public bool IsCloud { get; set; }
        public string Environment { get; set; }        
    }

    public class ConfigSetting
    {
        public string SecretName { get; set; }
        public string Region { get; set; }
        public string DbName { get; set; }
        public int DaysToPreserveData { get; set; }
    }
}
