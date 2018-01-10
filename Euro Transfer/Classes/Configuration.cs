using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Euro_Transfer.Classes
{
    public class Configuration
    {
        static Configuration()
        {
            ExtraCust = Properties.Settings.Default.ExtraCustomer;
            ExtraReseller = Properties.Settings.Default.ExtraReseller;
            InitialTopup = Properties.Settings.Default.InitialTopup;
            InitialCost = Properties.Settings.Default.InitialCost;
            Rate = Properties.Settings.Default.Rate;
            VoucherPrice = Properties.Settings.Default.VoucherPrice;
        }

        public static DateTime lastUpdate;
        public static string DbFile = "customers";

        public static double InitialTopup;
        public static int InitialCost;
        public static int Rate;

        public static double ExtraReseller;
        public static double ExtraCust;

        public static int VoucherPrice;
    }
}
