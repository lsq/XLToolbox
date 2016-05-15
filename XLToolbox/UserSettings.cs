﻿/* UserSettings.cs
 * part of Daniel's XL Toolbox NG
 * 
 * Copyright 2014-2016 Daniel Kraus
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.IO;
using Bovender.UserSettings;
using XLToolbox.Export.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace XLToolbox
{
    /// <summary>
    /// XL Toolbox user settings. Should be used as singleton.
    /// </summary>
    /// <remarks>
    /// Settings will *not* automatically be saved to file during disposal;
    /// explicitly use the Save() method inherited from Bovender.UserSettings.UserSettingsBase.
    /// </remarks>
    public class UserSettings : UserSettingsBase
    {
        #region Static property

        public static string UserSettingsFile
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Properties.Settings.Default.AppDataFolder,
                    Properties.Settings.Default.UserSettingsFile);
            }
        }

        #endregion

        #region Singleton factory

        new public static UserSettings Default
        {
            get
            {
                return _lazy.Value;
            }
        }

        private static Lazy<UserSettings> _lazy = new Lazy<UserSettings>(() =>
        {
            Logger.Info("Initializing singleton instance");
            UserSettings s = FromFileOrDefault<UserSettings>(UserSettingsFile);
            Bovender.UserSettings.UserSettingsBase.Default = s;
            return s;
        });

        #endregion

        #region Public methods

        /// <summary>
        /// Creates a new settings object without loading the saved settings
        /// from file and without saving the current settings from file.
        /// </summary>
        new public static void LoadDefaults()
        {
            Logger.Info("LoadDefaults");
            _lazy = new Lazy<UserSettings>(() => new UserSettings());
        }

        #endregion

        #region User settings

        public DateTime LastUpdateCheck
        {
            get
            {
                if (_lastUpdateCheck == null)
                {
                    _lastUpdateCheck = new DateTime(2016, 1, 1);
                }
                return _lastUpdateCheck;
            }
            set {
                _lastUpdateCheck = value;
            }
        }

        public string LastVersionSeen
        {
            get
            {
                if (_lastVersionSeen == null)
                {
                    _lastVersionSeen = "0.0.0";
                }
                return _lastVersionSeen;
            }
            set
            {
                _lastVersionSeen = value;
            }
        }

        public int UpdateCheckInterval
        {
            get
            {
                if (_updateCheckInterval <= 0)
                {
                    _updateCheckInterval = 7;
                }
                return _updateCheckInterval;
            }
            set
            {
                _updateCheckInterval = value;
            }
        }

        public bool EnableLogging
        {
            get
            {
                // Query the LogFile without triggering initialization
                if (LogFile.IsInitializedAndEnabled)
                {
                    return LogFile.Default.IsFileLoggingEnabled;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                // Only access the singleton instance if the value if true,
                // in order to avoid superfluous initialization of the instance.
                if (value)
                {
                    LogFile.Default.IsFileLoggingEnabled = true;
                }
            }
        }

        public int LastAnova { get; set; }

        public int LastErrorBars { get; set; }

        public Csv.CsvFile CsvImport
        {
            get
            {
                if (_csvImport == null)
                {
                    _csvImport = _csvExport;
                }
                return _csvImport;
            }
            set
            {
                _csvImport = value;
            }
        }
        
        public Csv.CsvFile CsvExport
        {
            get
            {
                if (_csvExport == null)
                {
                    _csvExport = _csvImport;
                }
                return _csvExport;
            }
            set
            {
                _csvExport = value;
            }
        }

        /// <summary>
        /// Wraps the singleton PresetsRepository's Presets property.
        /// </summary>
        public ObservableCollection<Preset> ExportPresets
        {
            get
            {
                return PresetsRepository.Default.Presets;
            }
            set
            {
                PresetsRepository.Default.Presets = value;
            }
        }

        public Preset ExportPreset { get; set; }

        public BatchExportSettings BatchExportSettings
        {
            get
            {
                if (_batchExportSettings != null && _batchExportSettings.Preset != null)
                {
                    _batchExportSettings.Preset = PresetsRepository.Default.FindOrAdd(_batchExportSettings.Preset);
                }
                else
                {
                    // Invalidate the batch export settings if the Preset is null.
                    _batchExportSettings = null;
                }
                return _batchExportSettings;
            }
            set
            {
                _batchExportSettings = value;
            }
        }

        public Unit ExportUnit
        {
            get
            {
                return _lastExportUnit;
            }
            set
            {
                _lastExportUnit = value;
            }
        }

        public string ExportPath
        {
            get
            {
                return _exportPath;
            }
            set
            {
                _exportPath = value;
            }
        }

        public bool WorksheetManagerAlwaysOnTop { get; set; }

        /// <summary>
        /// Gets or sets whether the worksheet manager task pane is visible.
        /// </summary>
        /// <remarks>
        /// While it would be nice to have these accessors directly relay the
        /// value to the singleton Instance of the XLToolbox.SheetManager.SheetManagerPane,
        /// it did not work out because the task panes (being COM objects?) are
        /// not available at various stages during startup and shutdown. Therefore,
        /// the visibility is restored in ThisAddin_Startup, and the SheetManagerPane
        /// itself signals its visibility state to the UserSettings object.
        /// </remarks>
        public bool SheetManagerVisible { get; set; }

        public int TaskPaneWidth
        {
            get
            {
                if (_taskPaneWidth == 0)
                {
                    _taskPaneWidth = 320;
                }
                return _taskPaneWidth;
            }
            set
            {
                _taskPaneWidth = value;
            }
        }

        public ObservableCollection<Keyboard.Shortcut> KeyboardShortcuts
        {
            get
            {
                return Keyboard.Manager.Default.Shortcuts;
            }
            set
            {
                Keyboard.Manager.Default.Shortcuts = value;
            }
        }

        public bool Running { get; set; }

        #endregion

        #region Overrides

        public override string GetSettingsFilePath()
        {
            return UserSettingsFile;
        }

        protected override void WriteYamlHeader(StreamWriter streamWriter)
        {
            streamWriter.WriteLine(
                String.Format("# {0} <{1}>",
                    Properties.Settings.Default.AddinName,
                    Properties.Settings.Default.WebsiteUrl));
            streamWriter.WriteLine("# User settings file generated by version "
                + XLToolbox.Versioning.SemanticVersion.CurrentVersion().ToString());
            streamWriter.WriteLine("# " + System.DateTime.Now.ToString());
            base.WriteYamlHeader(streamWriter);
        }

        #endregion

        #region Private fields

        private string _exportPath;
        private BatchExportSettings _batchExportSettings;
        private Unit _lastExportUnit;
        private Csv.CsvFile _csvImport;
        private Csv.CsvFile _csvExport;
        private int _taskPaneWidth;
        private DateTime _lastUpdateCheck;
        private int _updateCheckInterval;
        private string _lastVersionSeen;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance. This should never be called directly, use
        /// the singleton factory instead. The constructor must be public to
        /// enable deserialization.
        /// </summary>
        public UserSettings()
        { }

        #endregion

        #region Class logger

        private static NLog.Logger Logger { get { return _logger.Value; } }

        private static readonly Lazy<NLog.Logger> _logger = new Lazy<NLog.Logger>(() => NLog.LogManager.GetCurrentClassLogger());

        #endregion
    }
}
