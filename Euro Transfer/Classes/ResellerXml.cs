using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Euro_Transfer.Classes
{
    class ResellerXml
    {
        static readonly string ResellerFile;
        static byte[] key;
        static byte[] iv;

		static ResellerXml()
		{
			ResellerFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\win";
		}
        public static void InitializeFile()
        {
            if (!File.Exists(ResellerFile))
            {
                using (FileStream fs = File.Create(ResellerFile))
                {
                    XElement root = new XElement("Root");
                    root.Add(new XElement("Resellers"));
                    XElement securityElem = new XElement("Security");
                    root.Add(securityElem);
                    using (AesManaged aes = new AesManaged())
                    {
                        iv = aes.IV;
                        key = aes.Key;

                        securityElem.Add(new XElement("K", key.Select(b => b.ToString("000"))),
                                         new XElement("IV", iv.Select(b => b.ToString("000"))));
                        root.Save(fs);
                    }
                }
            }
            else
            {
                XElement root = XElement.Load(ResellerFile);
                string keyString = root.Descendants("K").First().Value;
                string ivString = root.Descendants("IV").First().Value;

                key = new byte[(keyString.Length) / 3];
                for (int i = 0; i < keyString.Length; i += 3)
                {
                    key[i / 3] = byte.Parse(keyString.Substring(i, 3));
                }

                iv = new byte[(ivString.Length) / 3];
                for (int i = 0; i < ivString.Length; i += 3)
                {
                    iv[i / 3] = byte.Parse(ivString.Substring(i, 3));
                }

                GetResellers();
            }
        }

        public static void GetResellers()
        {
            XElement root = XElement.Load(ResellerFile);
            foreach (XElement elem in root.Descendants("Reseller"))
            {
                string name = elem.Attribute("Name").Value;
                string password = decrypt(elem.Element("Password").Value);
                Reseller r = new Reseller(name, password);
                App.Resellers.Add(r);
            }
        }

        public static void AddReseller(string name, string pass)
        {
            XElement root = XElement.Load(ResellerFile);
            XElement newResellerElem = new XElement("Reseller");
            root.Element("Resellers").Add(newResellerElem);

            newResellerElem.Add(new XAttribute("Name", name),
                new XElement("Password", encrypt(pass)));
            root.Save(ResellerFile);
            App.Resellers.Add(new Reseller(name, pass));
        }

        public static void DeleteReseller(Reseller res)
        {
            App.Resellers.Remove(res);
            XElement root = XElement.Load(ResellerFile);
            XElement resellerElem = root.Descendants("Reseller").FirstOrDefault(r => r.Attribute("Name").Value == res.Name);

            resellerElem.Remove();
            root.Save(ResellerFile);
        }

        private static string encrypt(string pass)
        {
            using (AesManaged aes = new AesManaged())
            {
                aes.Key = key;
                aes.IV = iv;

                byte[] data = Encoding.ASCII.GetBytes(pass);
                byte[] encData = aes.CreateEncryptor().TransformFinalBlock(data, 0, data.Count());
                return encData.Select(b => b.ToString("000")).Aggregate((s, next) => s + next);

            }
        }

        private static string decrypt(string pass)
        {
            using (AesManaged aes = new AesManaged())
            {
                aes.Key = key;
                aes.IV = iv;

                byte[] data;
                data = new byte[(pass.Length) / 3];
                for (int i = 0; i < pass.Length; i += 3)
                {
                    data[i / 3] = byte.Parse(pass.Substring(i, 3));
                }

                byte[] decData = aes.CreateDecryptor().TransformFinalBlock(data, 0, data.Count());
                return Encoding.ASCII.GetString(decData);

            }
        }
    }
}
