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

using System.IO;
using System.Xml.Linq;
using System.Security.Cryptography;
using Euro_Transfer.Classes;
using System.Globalization;
using System.Diagnostics;

namespace Euro_Transfer.Dialogs
{
    /// <summary>
    /// Interaction logic for ResellerWindow.xaml
    /// </summary>
    public partial class ResellerWindow : Window
    {
        //List<Reseller> wrongResellers;
        //List<Reseller> blockedResellers;

        public ResellerWindow()
        {
            InitializeComponent();
            ResellersList.ItemsSource = App.Resellers;
        }

        async private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddButton.IsEnabled = false;
            ErrorLabel.Text = "";
            string name = ResellerNameTextBox.Text.ToLowerInvariant();
            string pass = ResellerPassTextBox.Text;

            if (!name.Contains("ahkvoip"))
            {
                ErrorLabel.Text = "Reseller not working with this program.";
                return;
            }


            try
            {
                bool resellerCreated = await ResellerAPI.CheckReseller(name, pass);
                if (resellerCreated)
                {
                    ResellerXml.AddReseller(name, pass);
                    ResellerNameTextBox.Text = "";
                    ResellerPassTextBox.Text = "";
                    App.CustomersDB.AddReseller(new Reseller(name, pass));
                }
                else
                {
                    ErrorLabel.Text = "Wrong password,try again later";
                    AddButton.IsEnabled = true;
                }
            }
            catch (NoInternetException)
            {
                ErrorLabel.Text = "No Internet Connection!";
                AddButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = ex.Message;
            }

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

        }

        private void ResellerNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ErrorLabel.Text = "";

            if (string.IsNullOrWhiteSpace(ResellerNameTextBox.Text) || string.IsNullOrWhiteSpace(ResellerPassTextBox.Text))
            {
                AddButton.IsEnabled = false;
            }
            else
            {
                AddButton.IsEnabled = true;
            }

            if (ResellersList.HasItems == true)
            {
                Reseller matchedReseller = App.Resellers.SingleOrDefault(res => res.Name == ResellerNameTextBox.Text);

                ResellersList.SelectedItem = matchedReseller;
            }

        }

        private void ResellerPassTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ErrorLabel.Text = "";
            if (string.IsNullOrWhiteSpace(ResellerPassTextBox.Text) || string.IsNullOrWhiteSpace(ResellerNameTextBox.Text))
            {
                AddButton.IsEnabled = false;
            }
            else
            {
                AddButton.IsEnabled = true;
            }
        }
        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (Reseller res in App.Resellers)
            {
                App.CustomersDB.GetCustomers(res);
            }
            if (this.Owner == null)
                new MainWindow().Show();
            this.Close();
        }

        private void ResellersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Reseller res = ResellersList.SelectedItem as Reseller;
            if (res != null)
            {
                ResellerNameTextBox.TextChanged -= ResellerNameTextBox_TextChanged;
                ResellerNameTextBox.Text = res.Name;
                ResellerNameTextBox.TextChanged += ResellerNameTextBox_TextChanged;
                AddButton.Content = "Update";
            }
            else
            {
                AddButton.Content = "Add";
            }
        }

        private void DeleteLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            Reseller res = tb.Tag as Reseller;
            //System.Windows.Forms.MessageBox.Show("You have selected " + res.Name);
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete " + res.Name,
                "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (result == MessageBoxResult.Yes)
            {
                ResellerXml.DeleteReseller(res);
                ResellerNameTextBox.Text = "";
            }
            e.Handled = true;
        }

    }
}
