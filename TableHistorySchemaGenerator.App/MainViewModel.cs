using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TableHistorySchemaGenerator.Core;
using TableHistorySchemaGenerator.DacPack;

namespace TableHistorySchemaGenerator.App
{

    public enum EndPointType
    {
        FilePath,
        FolderPath,
        ConnectionString
    }
    public class DelegateCommand<T> : ICommand
    {
        private Action<T> ExecuteAction { get; set; }

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action<T> executeAction)
        {
            ExecuteAction = executeAction;
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => ExecuteAction((T)Convert.ChangeType(parameter, typeof(T)));
    }


    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel(ISystemSourceRequestor systemRequestor)
        {
            SourceType = EndPointType.FilePath;
            DestinationType = EndPointType.FolderPath;
            BrowseSource = new DelegateCommand<string>((c) =>
            {
                var filePath = _sourceType == EndPointType.FilePath ? systemRequestor.GetFile() : systemRequestor.GetFolder();
                this.Source = filePath;
            });
            BrowseDestination = new DelegateCommand<string>((c) =>
            {
                var filePath = _destinationType == EndPointType.FilePath ? systemRequestor.GetFile() : systemRequestor.GetFolder();
                this.Destination = filePath;
            });


            this.Configuration = new CommonConfigurationViewModel((p) => RaisePropertyChangedEvent(p))
            {
                ExpectedCreatedByColumnName = "CreatedBy",
                ExpectedCreatedTimestampColumnName = "CreatedTimestamp",
                Prefix = "History_",
                Schema = "hst"
            };
            LogLines = new ListViewModelLogger();

            GenerateScripts = new DelegateCommand<string>(async (c) =>  GenerateScriptsInternalAsync(c));
        }

        public void RaisePropertyChangedEvent(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private async Task GenerateScriptsInternalAsync(string commandValue)
        {
            LogLines.Clear();
            DacDbHistorySchemaControllerBuilder builder = new DacDbHistorySchemaControllerBuilder(this.Configuration, this.LogLines);
            DbHistorySchemaController controller = null;

            DacSchemaReader reader = null;
            FileStream sourceStream = null;
            switch (_sourceType)
            {
                case EndPointType.FilePath:
                    sourceStream = new FileStream(_source, FileMode.Open, FileAccess.Read);
                    reader = new DacSchemaReader(sourceStream, true, this.LogLines);
                    break;
                case EndPointType.ConnectionString:
                    reader = new DacSchemaReader(_source, this.LogLines);
                    break;
                case EndPointType.FolderPath:
                    throw new InvalidOperationException("Source cannot be of type folder");
            }
            try
            {
                switch (_destinationType)
                {
                    case EndPointType.FilePath:
                        controller = builder.Build(reader, new FileScriptDestinationWriter(Destination, this.Configuration, this.LogLines));
                        break;
                    case EndPointType.ConnectionString:
                        controller = builder.Build(reader, new DbScriptDestinationWriter(Destination, this.LogLines));
                        break;
                    case EndPointType.FolderPath:
                        controller = builder.Build(reader, new FolderScriptDestinationWriter(new DestinationConfiguration()
                        {
                            Common = this.Configuration,
                            Destination = Destination
                        }, this.LogLines));
                        break;
                }
                if (controller != null)
                {
                    controller.GenerateHistorySchemaObjects();
                }
            }
            finally
            {
                if (sourceStream != null) sourceStream.Dispose();
            }
        }

        public ICommand GenerateScripts { get; private set; }
        public ICommand BrowseSource { get; private set; }
        public ICommand BrowseDestination { get; private set; }

        private EndPointType _destinationType;
        public EndPointType DestinationType
        {
            get
            {
                return this._destinationType;
            }
            set
            {
                Destination = string.Empty;
                _destinationType = value;
                this.RaisePropertyChangedEvent("DestinationType");
            }
        }
        private EndPointType _sourceType;
        public EndPointType SourceType
        {
            get
            {
                return this._sourceType;
            }
            set
            {
                Source = string.Empty;
                _sourceType = value;
                this.RaisePropertyChangedEvent("SourceType");
            }
        }
        private string _source;
        public string Source
        {
            get
            {
                return this._source;
            }
            set
            {
                _source = value;
                this.RaisePropertyChangedEvent("Source");
            }
        }
        private string _destination;
        public string Destination
        {
            get
            {
                return this._destination;
            }
            set
            {
                _destination = value;
                this.RaisePropertyChangedEvent("Destination");
            }
        }

        public CommonConfigurationViewModel Configuration { get; private set; }

        public ListViewModelLogger LogLines { get;  private set; }

    }
    public class CommonConfigurationViewModel : IHistoryCommonConfiguration
    {
        private Action<string> _propertyChanged;
        public CommonConfigurationViewModel(Action<string> propertyChanged)
        {
            _propertyChanged = propertyChanged;
        }

        private string _expectedCreatedByColumnName;
        public string ExpectedCreatedByColumnName
        {
            get
            {
                return _expectedCreatedByColumnName;
            }
            set
            {
                _expectedCreatedByColumnName = value;
                this._propertyChanged("ExpectedCreatedByColumnName");
            }
        }

        private string _expectedCreatedTimestampColumnName;
        public string ExpectedCreatedTimestampColumnName
        {
            get
            {
                return _expectedCreatedTimestampColumnName;
            }
            set
            {
                _expectedCreatedTimestampColumnName = value;
                this._propertyChanged("ExpectedCreatedTimestampColumnName");
            }
        }

        private bool _includeDropStatements = false;
        public bool IncludeDropOrAlterStatements
        {
            get
            {
                return _includeDropStatements;
            }
            set
            {
                _includeDropStatements = value;
                this._propertyChanged("IncludeDropOrAlterStatements");
            }
        }

        private string _prefix;
        public string Prefix
        {
            get
            {
                return _prefix;
            }
            set
            {
                _prefix = value;
                this._propertyChanged("Prefix");
            }
        }

        private string _schema;
        public string Schema
        {
            get
            {
                return _schema;
            }
            set
            {
                _schema = value;
                this._propertyChanged("Schema");
            }
        }
    }
}
