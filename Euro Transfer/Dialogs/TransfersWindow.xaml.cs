using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Euro_Transfer.Classes;

namespace Euro_Transfer.Dialogs
{
    /// <summary>
    /// Interaction logic for TransfersWindow.xaml
    /// </summary>
    public partial class TransfersWindow : Window
    {
        CustomerCollection col;

        public TransfersWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Reseller r = this.Tag as Reseller;
            this.Title = r.Name;
            col = r.Customers;

            var orderedTransfers = from c in col
                                   from t in c.Transfers
                                   orderby t.TransferDate
                                   select t.TransferDate;

            TransferDatePicker.DisplayDateEnd = DateTime.Now;
            TransferDatePicker.DisplayDateStart = orderedTransfers.First();
            TransferDatePicker.SelectedDate = orderedTransfers.Last();
            TransferDatePicker.SelectedDateChanged += TransferDatePicker_SelectedDateChanged;
            filterTransfers(orderedTransfers.Last());
            getTotals(orderedTransfers.Last());
        }


        void getTotals(DateTime date)
        {
            int month = date.Month;
            int year = date.Year;

            var ts = from c in col
                     from t in c.Transfers
                     where t.TransferDate.Month == month && t.TransferDate.Year == year
                     select t;

            totalEuroText.Text = ts.Sum(t => t.Amount).ToString();
            totalMoneyText.Text = ts.Sum(t => t.Cost).ToString();

        }
        void filterTransfers(DateTime date)
        {
            double totalAmount = 0;
            int totalCost = 0;
            List<CustomerTransfer> transferList = new List<CustomerTransfer>();

            var ts = from c in col
                     from t in c.Transfers
                     where t.TransferDate.ToShortDateString().Equals(date.ToShortDateString())
                     orderby t.TransferDate descending
                     select new CustomerTransfer { Username = c.Username, TransferDate = t.TransferDate, Amount = t.Amount, Cost = t.Cost };


            totalAmount = ts.Sum(t => t.Amount ?? 0);
            totalAmountText.Content = totalAmount.ToString();

            var vs = from v in App.VoucherRecords
                     where v.Date.ToShortDateString().Equals(date.ToShortDateString())
                     select new CustomerTransfer() { Username = "[Voucher]", TransferDate = v.Date, Amount = 10, Cost = Configuration.VoucherPrice };

            if (vs.Count() > 0)
            {
                transferList.AddRange(vs);
                transferList.Add(new CustomerTransfer());
            }
            //var listTransfer = ts.ToList();

            transferList.AddRange(ts);
            totalCost = transferList.Sum(t => t.Cost ?? 0);
            totalCostText.Content = totalCost.ToString();

            TransfersList.ItemsSource = transferList;
        }

        private void TransferDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            filterTransfers((DateTime)TransferDatePicker.SelectedDate);
            getTotals((DateTime)TransferDatePicker.SelectedDate);
        }

        class CustomerTransfer
        {
            public string Username { get; set; }
            public DateTime? TransferDate { get; set; }
            public double? Amount { get; set; }
            public int? Cost { get; set; }

        }
    }
}
