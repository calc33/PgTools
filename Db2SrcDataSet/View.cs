using System.ComponentModel;

namespace Db2Source
{
    public partial class View: Selectable
    {
        public enum Kind
        {
            View,
            MarerializedView
        }
        private static readonly string[] KindToSqlType = { "VIEW", "MATERIALIZED VIEW" };
        private Kind _viewKind;
        public override string GetSqlType()
        {
            int k = (int)_viewKind;
            return KindToSqlType[(0 <= k && k < KindToSqlType.Length) ? k : 0];
        }

        public override string GetExportFolderName()
        {
            return "View";
        }

        private string _definition;
        private string _oldDefinition;
        public string Definition
        {
            get
            {
                return _definition;
            }
            set
            {
                if (_definition == value)
                {
                    return;
                }
                _definition = value;
                OnPropertyChanged("Definition");
            }
        }

        public override bool HasBackup()
        {
            return true;
        }

        public override void Backup(bool force)
        {
            _oldDefinition = _definition;
        }
        public override void Restore()
        {
            _definition = _oldDefinition;
        }
        public override bool IsModified
        {
            get
            {
                return _definition != _oldDefinition;
            }
        }
        public string[] ExtraInfo { get; set; }
        internal View(Db2SourceContext context, Kind kind, string owner, string schema, string viewName, string defintion, bool isLoaded) : base(context, owner, schema, viewName)
        {
            _viewKind = kind;
            _definition = defintion;
            if (isLoaded)
            {
                _oldDefinition = _definition;
            }
        }
    }
}
