using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTUSDKDemo
{
    public class MQTTSettings
    {
        public string URI { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SubscribeTopic { get; set; }
        public string PublishTopic { get; set; }
        public string ClientCertificateFilePath { get; set; }
        public string ClientCertificatePassword { get; set; }

        public MQTTSettings()
        {
            URI = "";
            Username = "";
            Password = "";
            SubscribeTopic = "";
            PublishTopic = "";
            ClientCertificateFilePath = "";
            ClientCertificatePassword = "";
        }
    }
}
