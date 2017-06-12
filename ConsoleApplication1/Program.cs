using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CMSTIP;

namespace ConsoleApplication1
{
    class Program
    {
        static string ConfigFile = "c:\\Logs\\config.xml";
        private static void updateMonerisConfig(String key, String value)
        {
            XmlDocument xmlDoc = new XmlDocument();
            //String configName = @"c:\cms slyce1\chipdna.config.xml";
            Boolean result = false;

            try
            {
                xmlDoc.Load(ConfigFile);//loading config xml file
            }
            catch (Exception e)
            {
                Console.WriteLine("error" + e.Message);
            }

            XmlNode xns = xmlDoc.SelectSingleNode("CTIS");//get root node
            XmlNodeList xnl = xns.ChildNodes;//get all child nodes
            foreach (XmlNode xn in xnl)
            {
                if (xn.Name == "Moneris")
                {
                    XmlNodeList xnl2 = xn.ChildNodes;//get all child nodes
                    foreach (XmlNode xn2 in xnl2)
                    {
                        if (xn2.Name.Equals(key))
                        {
                            xn2.InnerText = value;
                            xmlDoc.Save(ConfigFile);
                            result = true;
                        }
                    }
                }

            }
            if (!result)
            {
                XmlElement xe1 = xmlDoc.CreateElement("Moneris");//创建一个<book>节点 
                XmlElement xesub1 = xmlDoc.CreateElement("TerminalIP");
                if (key == "TerminalIP")
                {
                    xesub1.InnerText = value;
                }
              
                XmlElement xesub2 = xmlDoc.CreateElement("TerminalPort");
                if (key=="TerminalPort")
                {
                    xesub1.InnerText = value;
                }
                xe1.AppendChild(xesub1);
                xe1.AppendChild(xesub2);


                xns.AppendChild(xe1);
                xmlDoc.Save(ConfigFile);     

            }

        }
        public static void SetServiceConfig(JObject inPar)
        {
            Dictionary<string, string> htmlAttributes = JsonConvert.DeserializeObject<Dictionary<string, string>>(inPar.ToString());
            foreach (var vv in htmlAttributes)
            {
                string key = vv.Key;
                string s = vv.Value;
                Console.WriteLine(" " + vv.Key + " " + vv.Value);
                UpdateServiceConfigXml(key, s);
            }
        }
        private static void UpdateServiceConfigXml(String key, String value)
        {
            XmlDocument xmlDoc = new XmlDocument();
            //String configName = @"c:\cms slyce1\chipdna.config.xml";
            if (key == "TerminalIP" || key == "TerminalPort")
            {
                updateMonerisConfig(key, value);
                return;
            }
            try
            {
                xmlDoc.Load(ConfigFile);//loading config xml file
            }
            catch (Exception e)
            {
                // Console.WriteLine("Open config.xml get exception: " + e.Message);
            }

            XmlNode xns = xmlDoc.SelectSingleNode("CTIS");//get root node
            XmlNodeList xnl = xns.ChildNodes;//get all child nodes
            foreach (XmlNode xn in xnl)
            {
                switch (xn.Name)
                {
                    case "SocketIP":
                    case "SocketPort":
                    case "Secure":
                    case "Cert":
                    case "PWD":
                    case "RootCert":
                    case "Delete":
                    case "Store":
                    case "FirefoxCert":
                    case "FirefoxPWD":
                    case "FirefoxRootCert":
                    case "UsingFirefox":
                    case "AutoDisconnect":
                    case "AutoDisconnectTimeout":
                    case "COMWriteTimeout":
                    case "COMReadTimeout":
                    case "EnableCheckServerLoop":
                    case "CheckServerInterval":
                        if (xn.Name.Equals(key))
                        {
                            xn.InnerText = value;
                            xmlDoc.Save(ConfigFile);
                        }

                        break;
                    default:
                       //  Console.WriteLine("Key " + xn.Name + " is not supported in config.xml");
                        break;
                }
            }

        }
        static void Main(string[] args)
        {
            /*Program.updateMonerisConfig("TerminalIP", "192.168.0.3");
            Program.updateMonerisConfig("TerminalPort", "1855");*/
            string v = "{\"TerminalIP\":\"10.0.0.200\",\"TerminalPort\":\"8180\",\"UsingFirefox\":\"false\"}";
            JObject x = JObject.Parse(v);
            //Program.SetServiceConfig(x);

            Common_SocketServer cs = new Common_SocketServer("127.0.0.1", 33333);
            cs.setConnection();

            Console.ReadLine();
            Common_SocketServer.ReleaseServer();

        }
    }
}
