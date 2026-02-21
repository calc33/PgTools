using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Db2Source
{
	/// <summary>
	/// AdjustSequencesControl.xaml の相互作用ロジック
	/// </summary>
	public partial class AdjustSequencesControl : UserControl, ISchemaObjectWpfControl
	{
		public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(PgsqlDatabase), typeof(AdjustSequencesControl), new FrameworkPropertyMetadata(OnTargetPropertyChanged));

		private void OnTargetPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			UpdateSequences();
		}

		private static void OnTargetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((AdjustSequencesControl)d).OnTargetPropertyChanged(d, e);
		}

		public static readonly DependencyProperty SequencesProperty = DependencyProperty.Register("Sequences", typeof(List<Sequence>), typeof(AdjustSequencesControl), new FrameworkPropertyMetadata(new List<Sequence>()));

		public PgsqlDatabase Target
		{
			get
			{
				return (PgsqlDatabase)GetValue(TargetProperty);
			}
			set
			{
				SetValue(TargetProperty, value);
			}
		}

		SchemaObject ISchemaObjectControl.Target
		{
			get
			{
				return Target;
			}
			set
			{
				Target = value as PgsqlDatabase;
			}
		}

		public AdjustSequencesControl()
		{
			InitializeComponent();
		}

		private string _lastSelectedTabKey = string.Empty;
		public string SelectedTabKey
		{
			get { return _lastSelectedTabKey; }
			set { _lastSelectedTabKey = value; }
		}

		public string[] SettingCheckBoxNames => Array.Empty<string>();

		public void Dispose()
		{
			BindingOperations.ClearAllBindings(this);
		}

		public void OnTabClosed(object sender) { }

		public void OnTabClosing(object sender, ref bool cancel) { }

		private void UpdateSequences()
		{
			if (Target == null)
			{
				Sequences = new List<Sequence>();
			}
			else
			{
				List<Sequence> list = new List<Sequence>();
				foreach (Sequence sequence in Target.Context.Sequences)
				{
					if (sequence.HasOwnedColumn)
					{
						list.Add(sequence);
					}
				}
				list.Sort();
				Sequences = list;
			}
		}

		public List<Sequence> Sequences
		{
			get { return (List<Sequence>)GetValue(SequencesProperty); }
			set { SetValue(SequencesProperty, value); }
		}

	}
}
