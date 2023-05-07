using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Db2Source
{
    public class ForeignKeyRuleConverter : IValueConverter
    {
        private static Dictionary<ForeignKeyRule, string> _ruleToText = new Dictionary<ForeignKeyRule, string>()
        {
            { ForeignKeyRule.NoAction, "NO ACTION" },
            { ForeignKeyRule.Restrict, "RESTICT" },
            { ForeignKeyRule.Cascade, "CASCADE" },
            { ForeignKeyRule.SetNull, "SET NULL" },
            { ForeignKeyRule.SetDefault, "SET DEFAULT" },
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ForeignKeyRule))
            {
                return null;
            }
            ForeignKeyRule rule = (ForeignKeyRule)value;
            return _ruleToText[rule];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
