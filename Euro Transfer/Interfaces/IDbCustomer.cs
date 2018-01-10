using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO.Compression;

namespace Euro_Transfer.Classes
{
    public class CustomersDbBase
    {
        public virtual void AddReseller(Reseller res) { }
        public virtual void GetCustomers(Reseller reseller) { }
        public virtual void AddCustomer(Customer cust)
        {
            cust.Reseller.Customers.Insert(0, cust);
        }
        public virtual void UpdateCustomer(Customer cust) { }
        public virtual void AddTransfer(Customer cust, Transfer trans)
        {
            cust.Transfers.Insert(0, trans);
            if (cust.Status != AccountStatus.Active)
            {
                if (cust.Status == AccountStatus.SemiActive)
                    cust.Reseller.SemiActiveCount--;
                else if (cust.Status == AccountStatus.NotActive)
                    cust.Reseller.InactiveCount--;
                else if (cust.Status == AccountStatus.NotUsed)
                {
                    cust.CreatedOn = trans.TransferDate;
                }

                cust.Status = AccountStatus.Active;
                cust.Reseller.ActiveCount++;

                App.CustomersDB.UpdateCustomer(cust);
            }
        }
        public virtual void Save() { }
    }

    public class CustomersXmlDb : CustomersDbBase
    {
        const string cResellerElem = "Reseller";
        const string cResellerNameAttr = "Name";
        const string cResellerLastUpdateAttr = "LastUpdate";

        const string cUserElem = "User";
        const string cUserNameAttr = "Name";
        const string cUserPassAttr = "Password";
        const string cUserCreatedAttr = "CreatedOn";
        const string cUserPhoneAttr = "Phone";
        const string cUserStatusAttr = "Status";
        const string cUserIsBlockedAttr = "IsBlocked";
        const string cUserIsResellerAttr = "IsReseller";

        const string cTransferElem = "Transfer";
        const string cTransferAmountAttr = "Euro";
        const string cTransferCostAttr = "Cost";
        const string cTransferDateAttr = "Date";

        XElement root;

        bool immediateSave = true;
        public void EnableSaving()
        {
            immediateSave = true;
        }
        public void DisableSaving()
        {
            immediateSave = false;
        }

        public CustomersXmlDb()
        {
            if (File.Exists(Configuration.DbFile))
            {
                FileStream stream = File.OpenRead(Configuration.DbFile);
                GZipStream compressionStream = new GZipStream(stream, CompressionMode.Decompress);
                root = XElement.Load(compressionStream);
                compressionStream.Close();
                stream.Close();
            }
            else
                root = new XElement("Root");
        }

        public override void GetCustomers(Reseller res)
        {
            XElement resellerElem = getResellerElement(res);

            if (resellerElem == null)
            {
				AddReseller(res);
                res.Customers = new CustomerCollection();
                return;
            }

            DateTime lastUpdate = DateTime.FromBinary(long.Parse(resellerElem.Attribute(cResellerLastUpdateAttr).Value));
            res.LastUpdate = lastUpdate;

            var customers = from custElem in resellerElem.Descendants(cUserElem)
                            let custName = custElem.Attribute(cUserNameAttr).Value
                            let custPass = custElem.Attribute(cUserPassAttr).Value
                            let custCreatedDate = DateTime.FromBinary(long.Parse(custElem.Attribute(cUserCreatedAttr).Value))
                            let custPhone = custElem.Attribute(cUserPhoneAttr).Value
                            let custStatus = (AccountStatus)Enum.Parse(typeof(AccountStatus), custElem.Attribute(cUserStatusAttr).Value)
                            let custIsBlocked = custElem.Attribute(cUserIsBlockedAttr).Value == "true"
                            let custIsReseller = custElem.Attribute(cUserIsResellerAttr).Value == "true"
                            let custTransfers = from transElem in custElem.Descendants(cTransferElem)
                                                let euro = double.Parse(transElem.Attribute(cTransferAmountAttr).Value)
                                                let cost = int.Parse(transElem.Attribute(cTransferCostAttr).Value)
                                                let transferDate = DateTime.FromBinary(long.Parse(transElem.Attribute(cTransferDateAttr).Value))
                                                select new Transfer(euro, cost, transferDate)
                            select new Customer(custName, custPass, res,custPhone,custIsBlocked,custStatus)
                                                {
                                                    CreatedOn = custCreatedDate,
                                                    Transfers = new System.Collections.ObjectModel.ObservableCollection<Transfer>(custTransfers),
                                                    IsReseller = custIsReseller,
													Reference = custElem
                                                };

            res.Customers = new CustomerCollection(customers);
        }

        public override void AddReseller(Reseller res)
        {
            XElement resElem = getResellerElement(res);

            if (resElem != null)
            {
                resElem.SetAttributeValue(cResellerLastUpdateAttr, res.LastUpdate.ToBinary());
            }
            else
            {
                resElem = new XElement(cResellerElem);
                resElem.Add(new XAttribute(cResellerNameAttr, res.Name));
                resElem.Add(new XAttribute(cResellerLastUpdateAttr, res.LastUpdate.ToBinary()));

                root.Add(resElem);
            }

            if (immediateSave)
                Save();
        }

        public override void AddCustomer(Customer cust)
        {
            XElement resElem = getResellerElement(cust.Reseller);

            XElement custElem = new XElement(cUserElem);

            custElem.SetAttributeValue(cUserNameAttr, cust.Username);
            custElem.SetAttributeValue(cUserPassAttr, cust.Password);
            custElem.SetAttributeValue(cUserCreatedAttr, cust.CreatedOn.ToBinary());
            custElem.SetAttributeValue(cUserPhoneAttr, cust.PhoneNumber);
            custElem.SetAttributeValue(cUserStatusAttr, cust.Status);
            custElem.SetAttributeValue(cUserIsBlockedAttr, cust.IsBlocked);
            custElem.SetAttributeValue(cUserIsResellerAttr, cust.IsReseller);

            resElem.Add(custElem);
			cust.Reference = custElem;

            if (immediateSave)
                Save();

            base.AddCustomer(cust);
        }

        public override void UpdateCustomer(Customer cust)
        {
			XElement custElem = cust.Reference;

            custElem.SetAttributeValue(cUserNameAttr, cust.Username);
            custElem.SetAttributeValue(cUserPassAttr, cust.Password);
            custElem.SetAttributeValue(cUserCreatedAttr, cust.CreatedOn.ToBinary());
            custElem.SetAttributeValue(cUserPhoneAttr, cust.PhoneNumber);
            custElem.SetAttributeValue(cUserStatusAttr, cust.Status);
            custElem.SetAttributeValue(cUserIsBlockedAttr, cust.IsBlocked);
            custElem.SetAttributeValue(cUserIsResellerAttr, cust.IsReseller);

            if (immediateSave)
                Save();

            base.UpdateCustomer(cust);
        }

        public override void AddTransfer(Customer cust, Transfer trans)
        {
			XElement custElem = cust.Reference;

            XElement transElem = new XElement(cTransferElem);
            transElem.Add(new XAttribute(cTransferAmountAttr, trans.Amount.ToString("0.00")));
            transElem.Add(new XAttribute(cTransferCostAttr, trans.Cost));
            transElem.Add(new XAttribute(cTransferDateAttr, trans.TransferDate.ToBinary()));

            custElem.AddFirst(transElem);

            if (immediateSave)
                Save();

            base.AddTransfer(cust, trans);
        }

        private XElement getResellerElement(Reseller reseller)
        {
            var resellerElements = root.Descendants(cResellerElem);
            return resellerElements.FirstOrDefault(r => r.Attribute(cResellerNameAttr).Value == reseller.Name);
        }

        public override void Save()
        {
            FileStream stream = File.OpenWrite(Configuration.DbFile);
            GZipStream compressionStream = new GZipStream(stream, CompressionLevel.Optimal);

            root.Save(compressionStream);
            compressionStream.Close();
            stream.Close();

			base.Save();
        }

    }
}
