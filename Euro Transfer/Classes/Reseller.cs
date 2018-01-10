using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Euro_Transfer.Classes
{
	public class Reseller : INotifyPropertyChanged
	{
		string name;
		string superReseller;
		string password;

		public string Name
		{
            get
            {
                return name;
            }
            set
            {
                name = value;
                if (value.Contains('*'))
                {
                    string[] names = value.Split(new char[] { '*' });
                    superReseller = names[1];
                }
                else
                {
                    superReseller = "";
                }
            }
		}

		public string SuperReseller
		{
			get { return superReseller; }
		}

		public string Password
		{
			get { return password; }
		}

		public DateTime LastUpdate
		{ get; set; }

		public Reseller(string name, string pass)
		{
			Name = name;
			password = pass;
			LastUpdate = DateTime.MinValue;
		}

		public CustomerCollection Customers
		{
			get;
			set;
		}

		int active = 0, semi = 0, inactive = 0;
		public int ActiveCount
		{
			get { return active; }
			set
			{
				active = value;
				onPropertyChanged("ActiveCount");
			}
		}
		public int SemiActiveCount
		{
			get { return semi; }
			set
			{
				semi = value;
				onPropertyChanged("SemiActiveCount");
			}
		}
		public int InactiveCount
		{
			get { return inactive; }
			set
			{
				inactive = value;
				onPropertyChanged("InactiveCount");
			}
		}

		public void UpdateCustomers()
		{

			try
			{
				int a = 0, s = 0, i = 0;
				((CustomersXmlDb)App.CustomersDB).DisableSaving();
				foreach (Customer c in Customers)
				{
					Transfer lastTransfer = (from t in c.Transfers
											 orderby t.TransferDate descending
											 select t).FirstOrDefault();

					if (lastTransfer == null)
					{
						c.Status = AccountStatus.NotUsed;
						c.CreatedOn = new DateTime(2013, 6, 1);
						goto Finish;
					}

					double ts = (from t in c.Transfers
								 select t.Amount).Sum();

					if (ts == 0)
					{
						c.Status = AccountStatus.NotUsed;
						c.CreatedOn = new DateTime(2013, 6, 1);
						goto Finish;
					}

					Transfer firstTransfer = (from t in c.Transfers
											  orderby t.TransferDate ascending
											  select t).FirstOrDefault();
					c.CreatedOn = firstTransfer.TransferDate;

					TimeSpan difference = DateTime.Now.Subtract(lastTransfer.TransferDate);

					if (difference.Days < 28)
					{
						a++;
						c.Status = AccountStatus.Active;
					}
					else if (difference.Days >= 28 && difference.Days < 56)
					{
						s++;
						c.Status = AccountStatus.SemiActive;
					}
					else
					{
						i++;
						c.Status = AccountStatus.NotActive;
					}

				Finish:
					App.CustomersDB.UpdateCustomer(c);
				}

				ActiveCount = a;
				SemiActiveCount = s;
				InactiveCount = i;

				((CustomersXmlDb)App.CustomersDB).EnableSaving();
			}
			catch (Exception)
			{

			}

		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void onPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
