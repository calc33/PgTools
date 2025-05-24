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
using System.Windows.Threading;

namespace Db2Source
{
	/// <summary>
	/// ConnectionStringWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class ConnectionStringWindow : Window
	{
		public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(ConnectionInfo), typeof(ConnectionStringWindow), new FrameworkPropertyMetadata(OnTargetPropertyChanged));
		public static readonly DependencyProperty CanShowPasswordProperty = DependencyProperty.Register("CanShowPassword", typeof(bool), typeof(ConnectionStringWindow));
		public static readonly DependencyProperty ShowPasswordProperty = DependencyProperty.Register("ShowPassword", typeof(bool), typeof(ConnectionStringWindow));
		public static readonly DependencyProperty ConnectionStringProperty = DependencyProperty.Register("ConnectionString", typeof(string), typeof(ConnectionStringWindow));

		private static void OnTargetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
		{
			(target as ConnectionStringWindow)?.OnTargetPropertyChanged(e);
		}

		public ConnectionInfo Target
		{
			get { return (ConnectionInfo)GetValue(TargetProperty); }
			set { SetValue(TargetProperty, value); }
		}

		public string ConnectionString
		{
			get { return (string)GetValue(ConnectionStringProperty); }
			set { SetValue(ConnectionStringProperty, value); }
		}

		public bool CanShowPassword
		{
			get { return (bool)GetValue(CanShowPasswordProperty); }
			set { SetValue(CanShowPasswordProperty, value); }
		}

		public bool ShowPassword
		{
			get { return (bool)GetValue(ShowPasswordProperty); }
			set { SetValue(ShowPasswordProperty, value); }
		}

		private void UpdateConnectionString()
		{
			ConnectionString = Target.GetExampleConnectionString(!(ShowPassword && CanShowPassword));
		}

		private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateConnectionString();
		}

		public ConnectionStringWindow()
		{
			InitializeComponent();
			new CloseOnDeactiveWindowHelper(this, true);
		}
	}
}
