using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Euro_Transfer.Classes;

namespace Euro_Transfer.Classes
{
    public enum AccountStatus
    {
        // Customer made a trnx in the last 4 weeks
        Active,

        // Customer made a trnx in the last 5-10 weeks
        SemiActive,

        // Customer didn't make a trnx in the last 10 weeks
        NotActive,

        // Customer is created but not given to a person
        NotUsed,


        Invalid
    }

    public class Customer : INotifyPropertyChanged, IElementRef
    {
        Reseller reseller;
        public Reseller Reseller
        {
            get { return reseller; }
            set
            {
                reseller = value;
                OnPropertyChanged("Reseller");
            }
        }

        string username;
        public string Username
        {
            get
            {
                System.Diagnostics.Debug.WriteLine(username);
                return username;
            }
        }

        string password;
        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        DateTime createdOn;
        public DateTime CreatedOn
        {
            get { return createdOn; }
            set { createdOn = value; }
        }

        AccountStatus status;
        public AccountStatus Status
        {
            get
            {
                return status; }
            set
            {
                if (status == value)
                    return;
                status = value;
                OnPropertyChanged("Status");
            }
        }

        bool isBlocked;
        public bool IsBlocked
        {
            get { return isBlocked; }
            set
            {
                if (isBlocked == value)
                    return;
                isBlocked = value;
                OnPropertyChanged("IsBlocked");
                try
                {
                    App.CustomersDB.UpdateCustomer(this);
                    App.CustomersDB.Save();
                }
                catch
                {

                }
            }
        }
        public bool IsReseller { get; set; }

        string phoneNumber;
        public string PhoneNumber
        {
            get { return phoneNumber; }
            set
            {
                if (phoneNumber == value)
                    return;
                phoneNumber = value;
                OnPropertyChanged("PhoneNumber");
            }
        }

        ObservableCollection<Transfer> transfers = new ObservableCollection<Transfer>();
        public ObservableCollection<Transfer> Transfers
        {
            get { return transfers; }
            set { transfers = value; }
        }

        public Customer(string username, string password, Reseller reseller)
        {
            this.username = username;
            this.password = password;
            this.reseller = reseller;
            this.status = AccountStatus.NotUsed;
            this.createdOn = DateTime.Now;
            this.phoneNumber = "";
            isBlocked = false;
            IsReseller = false;

            transfers = new ObservableCollection<Transfer>();
        }

		public Customer(string username, string password, Reseller reseller,
			string phone, bool isBlock, AccountStatus status)
		{
			this.username = username;
			this.password = password;
			this.reseller = reseller;
			this.status = status;
			this.createdOn = DateTime.Now;
			this.phoneNumber = phone;
			isBlocked = isBlock;
			IsReseller = false;

			transfers = new ObservableCollection<Transfer>();
		}


		public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        System.Xml.Linq.XElement reference;
        public System.Xml.Linq.XElement Reference
        {
            get
            {
                return reference;
            }
            set
            {
                reference = value;
            }
        }
    }

    public class CustomerCollection : ObservableCollection<Customer>
    {

        public CustomerCollection()
            : base()
        { }
        public CustomerCollection(IEnumerable<Customer> customers)
            : base(customers)
        { }



    }
}
