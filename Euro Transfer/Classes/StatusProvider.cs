using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Euro_Transfer.Classes
{
	public class StatusProvider : INotifyPropertyChanged
	{
		string messages;
		string status;
		bool isError = false;
		SolidColorBrush brush;

		public string Messages
		{
			get { return messages; }
			set { messages = value; OnPropertyChanged("Messages"); }
		}

		public string Status
		{
			get { return status; }
			set { status = value; OnPropertyChanged("Status"); }
		}

		public bool IsError
		{
			get { return isError; }
			set
			{
				isError = value;
				Brush = isError ? Brushes.Red : Brushes.Chartreuse;
			}
		}

		public SolidColorBrush Brush
		{
			get { return brush; }
			set { brush = value; OnPropertyChanged("Brush"); }
		}

		public StatusProvider(string initialStatus)
		{
			Status = initialStatus;
			IsError = false;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		public void RiseError(string errorMessage)
		{
			IsError = true;
			Status = errorMessage;
		}
	}
}
