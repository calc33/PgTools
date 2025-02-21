using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Db2Source
{
	public class ParamEditor : DependencyObject
	{
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(string), typeof(ParamEditor), new PropertyMetadata(new PropertyChangedCallback(OnValuePropertyChanged)));
		public static readonly DependencyProperty IsErrorProperty = DependencyProperty.Register("IsError", typeof(bool), typeof(ParamEditor));
		public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register("StringFormat", typeof(string), typeof(ParamEditor));
		public static readonly DependencyProperty IsNullProperty = DependencyProperty.Register("IsNull", typeof(bool), typeof(ParamEditor), new PropertyMetadata(new PropertyChangedCallback(OnIsNullPropertyChanged)));
		public static readonly DependencyProperty NewValueProperty = DependencyProperty.Register("NewValue", typeof(string), typeof(ParamEditor));
		private Parameter _parameter;
		private IDbDataParameter _dbParameter;
		public Parameter Parameter
		{
			get
			{
				return _parameter;
			}
			set
			{
				_parameter = value;
				if (_parameter == null)
				{
					return;
				}
				StringFormat = _parameter.StringFormat;
				if (_parameter.DbParameter != null)
				{
					DbParameter = _parameter.DbParameter;
				}
			}
		}
		public Type ValueType { get; set; }
		public IDbDataParameter DbParameter
		{
			get
			{
				return _dbParameter;
			}
			set
			{
				if (_dbParameter == value)
				{
					return;
				}
				_dbParameter = value;
			}
		}
		private string GetStrValue()
		{
			if (DbParameter == null)
			{
				return null;
			}
			if (DbParameter.Value == null)
			{
				return null;
			}
			if (!string.IsNullOrEmpty(StringFormat))
			{
				string fmt = "{0:" + StringFormat + "}";
				return string.Format(fmt, DbParameter.Value);
			}
			else
			{
				return DbParameter.Value.ToString();
			}
		}
		public void RevertValue()
		{
			if (DbParameter == null)
			{
				return;
			}
			IsNull = ((DbParameter.Value == null) || (DbParameter.Value is DBNull));
			Value = GetStrValue();
		}
		public void SetValue()
		{
			if (DbParameter == null)
			{
				return;
			}
			if (IsNull)
			{
				DbParameter.Value = DBNull.Value;
			}
			if (ValueType == typeof(string) || ValueType.IsSubclassOf(typeof(string)))
			{
				DbParameter.Value = string.IsNullOrEmpty(Value) ? (object)DBNull.Value : Value;
				return;
			}
			if (string.IsNullOrEmpty(Value))
			{
				DbParameter.Value = DBNull.Value;
				return;
			}
			MethodInfo mi = null;
			if (!string.IsNullOrEmpty(StringFormat))
			{
				mi = ValueType.GetMethod("ParseExact", new Type[] { typeof(string), typeof(string) });
				if (mi != null)
				{
					DbParameter.Value = mi.Invoke(null, new object[] { Value, StringFormat });
					return;
				}
			}
			mi = ValueType.GetMethod("Parse", new Type[] { typeof(string) });
			if (mi == null)
			{
				throw new NotSupportedException();
			}
			DbParameter.Value = mi.Invoke(null, new object[] { Value });
		}
		public void RevertNewValue()
		{
			NewValue = GetStrValue();
		}
		public string ParameterName
		{
			get
			{
				return DbParameter?.ParameterName;
			}
		}

		public string StringFormat
		{
			get { return (string)GetValue(StringFormatProperty); }
			private set { SetValue(StringFormatProperty, value); }
		}
		public bool IsNull
		{
			get { return (bool)GetValue(IsNullProperty); }
			set { SetValue(IsNullProperty, value); }
		}
		public string Value
		{
			get { return (string)GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}

		public string NewValue
		{
			get { return (string)GetValue(NewValueProperty); }
			set { SetValue(NewValueProperty, value); }
		}

		private void OnValuePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (!string.IsNullOrEmpty(Value) && IsNull)
			{
				IsNull = false;
			}
		}

		private static void OnValuePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
		{
			(target as ParamEditor)?.OnValuePropertyChanged(e);
		}

		private void OnIsNullPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (IsNull && !string.IsNullOrEmpty(Value))
			{
				Value = null;
			}
		}

		private static void OnIsNullPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
		{
			(target as ParamEditor)?.OnIsNullPropertyChanged(e);
		}

		//public ParamEditor() { }
		public ParamEditor(Parameter param)
		{
			Parameter = param;
			ValueType = param.ValueType;
		}
	}
}
