﻿/* Instance.cs
 * part of Daniel's XL Toolbox NG
 * 
 * Copyright 2014-2015 Daniel Kraus
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.Office.Interop.Excel;
using Bovender.Mvvm.ViewModels;
using System.Collections;
using Bovender.Mvvm;
using Bovender.Mvvm.Messaging;
using System.Diagnostics;

namespace XLToolbox.Excel.ViewModels
{
    /// <summary>
    /// Provide access to an instance of Excel that the
    /// components are to work with.
    /// </summary>
    /// <remarks>
    /// <para>This class uses static fields to make sure only one
    /// instance of Excel is invoked. An internal counter records
    /// the number of class instances that are currently in use;
    /// when the last instance of this class is disposed of, the
    /// Excel instance will be closed.</para>
    /// <para>Note that this class will only reference one single
    /// Excel instance, regardless whether this was started using
    /// a static method or by instantiating the class. Thus, there
    /// is no instance property to access the Exce instance, just
    /// the static property. Instantiating this class mainly serves
    /// the purpose of being able to automatically close Excel when
    /// the work is done by using Using() structures.</para>
    /// </remarks>
    public class Instance : ViewModelBase, IDisposable
    {
        #region Singleton factory

        public static Instance Default
        {
            get
            {
                if (_singletonInstance == null)
                {
                    _singletonInstance = new Instance(new Application());
                    _singletonInstance.Application.Workbooks.Add();
                }
                return _singletonInstance;
            }
            set
            {
                _singletonInstance = value;
            }
        }

        #endregion

        #region Commands

        public DelegatingCommand QuitInteractivelyCommand
        {
            get
            {
                if (_quitInteractivelyCommand == null)
                {
                    _quitInteractivelyCommand = new DelegatingCommand(
                        (param) => DoQuitInteractively());
                }
                return _quitInteractivelyCommand;
            }
        }

        public DelegatingCommand QuitSavingChangesCommand
        {
            get
            {
                if (_quitSavingChangesCommand == null)
                {
                    _quitSavingChangesCommand = new DelegatingCommand(
                        (param) => DoQuitSavingChanges(),
                        (param) => CanQuitSavingChanges());
                }
                return _quitSavingChangesCommand;
            }
        }

        public DelegatingCommand QuitDiscardingChangesCommand
        {
            get
            {
                if (_quitDiscardingChangesCommand == null)
                {
                    _quitDiscardingChangesCommand = new DelegatingCommand(
                        (param) => DoQuitDiscardingChanges(),
                        (parma) => CanQuitDiscardingChanges());
                }
                return _quitDiscardingChangesCommand;
            }
        }

        #endregion

        #region MVVM messages

        public Message<MessageContent> ConfirmQuitSavingChangesMessage
        {
            get
            {
                if (_confirmQuitSavingChangesMessage == null)
                {
                    _confirmQuitSavingChangesMessage = new Message<MessageContent>();
                }
                return _confirmQuitSavingChangesMessage;
            }
        }

        public Message<MessageContent> ConfirmQuitDiscardingChangesMessage
        {
            get
            {
                if (_confirmQuitDiscardingChangesMessage == null)
                {
                    _confirmQuitDiscardingChangesMessage = new Message<MessageContent>();
                }
                return _confirmQuitDiscardingChangesMessage;
            }
        }

        #endregion

        #region Public properties

        public Application Application
        {
            [DebuggerStepThrough]
            get
            {
                return _application;
            }
        }

        public Workbook ActiveWorkbook
        {
            get
            {
                return Application.ActiveWorkbook;
            }
        }

        /// <summary>
        /// Gets the Excel version and build number in a human-friendly form.
        /// </summary>
        /// <remarks>
        /// See http://spreadsheetpage.com/index.php/resource/excel_version_history
        /// and http://blog.pathtosharepoint.com/2014/05/06/how-to-get-your-office-365-version-number/
        /// </remarks>
        /// <param name="excel">Excel application whose version information to
        /// to retrieve.</param>
        /// <returns>String in the form of "2003", "2010 SP1" and so on.</returns>
        public string HumanFriendlyVersion
        {
            get
            {
                Application app = Application;
                string name = String.Empty;
                string sp = String.Empty;
                switch (Convert.ToInt32(app.Version.Split('.')[0]))
                {
                    // Very old versions are ignored (won't work with VSTO anyway)
                    case 11: name = "2003"; break;
                    case 12: name = "2007"; break;
                    case 14: name = "2010"; break;
                    case 15: name = "2013"; break;
                    case 16: name = "365"; break; // I believe (sparse information on the web)
                }
                int build = app.Build;
                switch (app.Version)
                {
                    case "15.0":
                        // 2013 SP information: http://support.microsoft.com/kb/2817430/en-us
                        if (build >= 4569) { sp = " SP1"; }
                        break;
                    case "14.0":
                        // 2010 SP information: http://support.microsoft.com/kb/2121559/en-us
                        if (build >= 7015) { sp = " SP2"; }
                        else if (build >= 6029) { sp = " SP1"; }
                        break;
                    case "12.0":
                        // 2007 SP information: http://support.microsoft.com/kb/928116/en-us
                        if (build >= 6611) { sp = " SP3"; }
                        else if (build >= 6425) { sp = " SP2"; }
                        else if (build >= 6241) { sp = " SP1"; }
                        break;
                }
                return String.Format("{0}{1} ({2}.{3})",
                    name, sp, app.Version, app.Build);
            }
        }

        public IEnumerable<Workbook> Workbooks
        {
            get
            {
                if (Application != null)
                {
                    return Application.Workbooks.Cast<Workbook>();
                }
                else
                {
                    return null;
                }
            }
        }

        public IEnumerable<Workbook> UnsavedWorkbooks
        {
            get
            {
                if (Application != null)
                {
                    return Workbooks.Where(w => w.Saved == false);
                }
                else
                {
                    return null;
                }
            }
        }

        public int CountOpenWorkbooks
        {
            get
            {
                if (Application != null)
                {
                    return Application.Workbooks.Count;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int CountUnsavedWorkbooks
        {
            get
            {
                if (Application != null)
                {
                    return Workbooks.Count<Workbook>(w => w.Saved == false);
                }
                else
                {
                    return 0;
                }
            }
        }

        public int CountSavedWorkbooks
        {
            get
            {
                if (Application != null)
                {
                    return Workbooks.Count<Workbook>(w => w.Saved == true);
                }
                else
                {
                    return 0;
                }
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Creates and returns a new workbook containing exactly one worksheet.
        /// </summary>
        /// <returns>Workbook with only one worksheet.</returns>
        public Workbook CreateWorkbook()
        {
            // Calling the Workbooks.Add method with a XlWBATemplate constand
            // creates a workbook that contains only one sheet.
            return Application.Workbooks.Add(XlWBATemplate.xlWBATWorksheet);
        }

        /// <summary>
        /// Creates a workbook containing the specified number of sheets (not less than 1).
        /// </summary>
        /// <remarks>If <paramref name="numberOfSheets"/> is less than 1, the workbook will still
        /// contain one worksheet.</remarks>
        /// <param name="numberOfSheets">Number of sheets in the new workbook.</param>
        /// <returns>Workbook containing the specified number of sheets (not less than 1).</returns>
        public Workbook CreateWorkbook(int numberOfSheets)
        {
            Workbook wb = CreateWorkbook();
            for (int i = 2; i <= numberOfSheets; i++)
            {
                wb.Sheets.Add(After: wb.Sheets[wb.Sheets.Count]);
            };
            return wb;
        }

        /// <summary>
        /// Disables screen updating. Increases an internal counter
        /// to be able to handle cascading calls to this method.
        /// </summary>
        public void DisableScreenUpdating()
        {
            if (_preventScreenUpdating == 0)
            {
                _wasScreenUpdating = Application.ScreenUpdating;
            }
            Application.ScreenUpdating = false;
            _preventScreenUpdating++;
        }

        /// <summary>
        /// Decreases the internal screen updating counter by one;
        /// if the counter reaches 0, the application's screen updating
        /// will resume.
        /// </summary>
        public void EnableScreenUpdating()
        {
            _preventScreenUpdating--;
            if (_preventScreenUpdating <= 0)
            {
                _preventScreenUpdating = 0;
                Application.ScreenUpdating = _wasScreenUpdating;
            }
        }

        /// <summary>
        /// Disables displaying of user alerts. Increases an internal counter
        /// to be able to handle cascading calls to this method.
        /// </summary>
        public void DisableDisplayAlerts()
        {
            if (_disableDisplayAlerts == 0)
            {
                _wasDisplayingAlerts = Application.DisplayAlerts;
            }
            Application.DisplayAlerts = false;
            _disableDisplayAlerts++;
        }

        /// <summary>
        /// Decreases the internal screen updating counter by one;
        /// if the counter reaches 0, the application's display of
        /// user alerts will be turned on again (in fact, it will
        /// be reset to its original state).
        /// </summary>
        public void EnableDisplayAlerts()
        {
            _disableDisplayAlerts--;
            if (_disableDisplayAlerts <= 0)
            {
                _disableDisplayAlerts = 0;
                Application.DisplayAlerts = _wasDisplayingAlerts;
            }
        }

        /// <summary>
        /// Debug method to reset the Excel application. The result
        /// is an application without open workbooks.
        /// </summary>
        [Conditional("DEBUG")]
        public void Reset()
        {
            DisableDisplayAlerts();
            foreach (Workbook wb in Application.Workbooks)
            {
                wb.Close();
            }
            Application.DisplayAlerts = true;
            _disableDisplayAlerts = 0;
            Application.ScreenUpdating = true;
            _preventScreenUpdating = 0;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Instantiates this class without an Excel instance
        /// </summary>
        public Instance() : base() { }

        /// <summary>
        /// Creates a new instance using <paramref name="application"/> as Excel
        /// instance.
        /// </summary>
        /// <param name="application">Excel instance.</param>
        public Instance(Application application)
            : this()
        {
            _application = application;
        }

        public Instance(bool createNewExcelInstance)
            : this()
        {
            if (createNewExcelInstance)
            {
                _application = new Application();
            }
        }

        #endregion

        #region Disposing

        ~Instance()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                // prevent executing this code again
                _disposed = true;
            }
        }

        protected virtual void Dispose(bool mayFreeManagedObjects)
        {
           Shutdown();
        }

        #endregion

        #region ViewModelBase implementation

        public override object RevealModelObject()
        {
            return _application;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Shuts down the current instance of Excel; no warning message will be shown.
        /// If an instance of this class exists, an error will be thrown.
        /// </summary>
        void Shutdown()
        {
            if (_application != null)
            {
                _application.DisplayAlerts = false;
                _application.Quit();
                _application = null;
            }
        }

        private void DoQuitInteractively()
        {
            CloseAllWorkbooksThenShutdown();
        }

        private void DoQuitSavingChanges()
        {
            ConfirmQuitSavingChangesMessage.Send(
                new MessageContent(),
                (MessageContent response) =>
                {
                    if (response.Confirmed) ConfirmQuitSavingChanges();
                });
        }

        /// <summary>
        /// Called by <see cref="DoQuitSavingChanges"/> if the view responded
        /// affirmatory to the <see cref="ConfirmQuiSavingChangesMessage"/>.
        /// </summary>
        private void ConfirmQuitSavingChanges()
        {
            foreach (Workbook w in UnsavedWorkbooks)
            {
                if (w.Path == String.Empty)
                {
                    // Cast to prevent ambiguity
                    ((_Workbook)w).Activate();
                    Application.Dialogs[XlBuiltInDialog.xlDialogSaveAs].Show();
                }
                else
                {
                    w.Save();
                }
                if (!w.Saved) return;
            }
            CloseAllWorkbooksThenShutdown();
        }

        private bool CanQuitSavingChanges()
        {
            return CountUnsavedWorkbooks > 0;
        }

        private void DoQuitDiscardingChanges()
        {
            ConfirmQuitDiscardingChangesMessage.Send(
                new MessageContent(),
                (MessageContent response) =>
                {
                    if (response.Confirmed) ConfirmQuitDiscardingChanges();
                });
        }

        /// <summary>
        /// Called by <see cref="DoQuitDiscardingChanges"/> if the view responded
        /// affirmatory to the <see cref="ConfirmQuitDiscardingChangesMessage"/>.
        /// </summary>
        private void ConfirmQuitDiscardingChanges()
        {
            foreach (Workbook w in UnsavedWorkbooks)
            {
                w.Saved = true;
            }
            CloseAllWorkbooksThenShutdown();
        }

        private bool CanQuitDiscardingChanges()
        {
            return CountUnsavedWorkbooks > 0;
        }

        /// <summary>
        /// Closes all workbooks.
        /// </summary>
        /// <returns>True if all workbooks were closed, false if not.</returns>
        private bool CloseAllWorkbooksThenShutdown()
        {
            while (Application.Workbooks.Count > 0)
            {
                // Excel collections are 1-based!
                Workbook w = Application.Workbooks[1];
                int n = CountOpenWorkbooks;
                w.Close();
                // Try if the workbook has been closed
                if (n == CountOpenWorkbooks) return false;
            }
            if (Application.Workbooks.Count == 0)
            {
                CloseViewCommand.Execute(null);
                Shutdown();
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion
       
        #region Private instance fields

        private bool _disposed;
        private Application _application;
        private DelegatingCommand _quitInteractivelyCommand;
        private DelegatingCommand _quitSavingChangesCommand;
        private DelegatingCommand _quitDiscardingChangesCommand;
        private Message<MessageContent> _confirmQuitSavingChangesMessage;
        private Message<MessageContent> _confirmQuitDiscardingChangesMessage;
        private bool _wasScreenUpdating;
        private bool _wasDisplayingAlerts;
        private int _preventScreenUpdating;
        private int _disableDisplayAlerts;

        #endregion

        #region Private static fields

        private static Instance _singletonInstance;

        #endregion
    }
}