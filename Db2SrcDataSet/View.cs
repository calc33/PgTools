﻿namespace Db2Source
{
    public partial class View: Selectable
    {
        public override string GetSqlType()
        {
            return "VIEW";
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
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Definition", value, _definition);
                _definition = value;
                OnPropertyChanged(e);
            }
        }
        public override bool IsModified()
        {
            return _definition != _oldDefinition;
        }
        public string[] ExtraInfo { get; set; }
        internal View(Db2SourceContext context, string owner, string schema, string viewName, string defintion, bool isLoaded) : base(context, owner, schema, viewName)
        {
            _definition = defintion;
            if (isLoaded)
            {
                _oldDefinition = _definition;
            }
        }
    }
}
