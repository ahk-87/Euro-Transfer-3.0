using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Euro_Transfer.Classes
{
	public class VoucherRecord : INotifyPropertyChanged
	{
		string status;
		string number;
		DateTime date;

		public DateTime Date
		{
			get { return date; }
			set { date = value; OnPropertyChanged("Date"); }
		}
		public string Voucher
		{ get; set; }
		public string Status
		{
			get { return status; }
			set { status = value; OnPropertyChanged("Status"); }
		}
		public string Number
		{
			get { return number; }
			set { number = value; OnPropertyChanged("Number"); }
		}

		public VoucherRecord()
		{

		}

		public VoucherRecord(DateTime date, string voucher, string number, string status)
		{
			Date = date;
			Voucher = voucher;
			Number = number;
			Status = status;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		public static void SaveToXml(VoucherRecord record)
		{
			XElement root = XElement.Load(App.xmlPath);
			root.Element("Rynga").Element("Vouchers").AddFirst(new XElement("Voucher",
				new XAttribute("Date", record.Date),
				new XAttribute("Voucher", record.Voucher),
				new XAttribute("Number", record.Number),
				new XAttribute("Status", record.Status)));
			root.Save(App.xmlPath);
		}


		public static void UpdateXml(VoucherRecord newRecord)
		{
			XElement root = XElement.Load(App.xmlPath);
			XElement record = root.Descendants("Voucher").
				Where(new Func<XElement, bool>(elem => elem.Attribute("Voucher").Value == newRecord.Voucher)).First();
			record.SetAttributeValue("Date", newRecord.Date);
			record.SetAttributeValue("Number", newRecord.Number);
			record.SetAttributeValue("Status", newRecord.Status);
			root.Save(App.xmlPath);
		}

		public static void GetVouchersFromXml()
		{
			XElement root = XElement.Load(App.xmlPath);
			foreach (XElement elem in root.Descendants("Voucher"))
			{
				VoucherRecord vouch = new VoucherRecord();
				DateTime d;
				try
				{
					d = DateTime.Parse(elem.Attribute("Date").Value, CultureInfo.InvariantCulture);
				}
				catch (Exception)
				{
					d = DateTime.ParseExact(elem.Attribute("Date").Value, "dd-MM-yy HH:mm", CultureInfo.InvariantCulture);
				}
				vouch.Date = d;
				vouch.Voucher = elem.Attribute("Voucher").Value;
				vouch.Number = elem.Attribute("Number").Value;
				vouch.Status = elem.Attribute("Status").Value;
				if (vouch.Status == "Not Sent")
					App.VoucherRecords.Insert(0, vouch);
				else
					App.VoucherRecords.Add(vouch);
			}
		}
	}
}
