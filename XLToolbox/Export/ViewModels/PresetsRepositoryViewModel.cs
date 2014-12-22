﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bovender.Mvvm;
using Bovender.Mvvm.ViewModels;
using Bovender.Mvvm.Messaging;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.Office.Interop.Excel;
using XLToolbox.Export.Models;
using XLToolbox.WorkbookStorage;

namespace XLToolbox.Export.ViewModels
{
    /// <summary>
    /// View model for an export settings repository.
    /// </summary>
    public class PresetsRepositoryViewModel : ViewModelBase
    {
        #region Public properties

        public PresetViewModelCollection Presets { get; private set; }

        public PresetViewModel LastSelected { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Instantiates the view model and creates a new PresetRepository
        /// instance which will load previously saved values in the background.
        /// </summary>
        public PresetsRepositoryViewModel()
            : base()
        {
            _repository = new PresetsRepository();
            Presets = new PresetViewModelCollection(_repository);
            // Since the PropertyChanged event of the ObservableCollection<T> from which
            // PresetViewModelCollection derives is protected, we need to access it
            // explicitly via the INotifyPropertyChanged interface. The reason why the event
            // is protected is because consumers are supposed to subscribe to
            // INotifyCollectionChanged.CollectionChanged; but in this case, we want to
            // know when the LastSelected property changes.
            ((INotifyPropertyChanged)Presets).PropertyChanged += PresetsRepositoryViewModel_PropertyChanged;
        }

        /// <summary>
        /// Instantiates the view model by creating a new repository instance
        /// (which loads previously saved values, if they exist) and adding
        /// the <paramref name="presetViewModel"/> to the repository.
        /// </summary>
        /// <param name="presetViewModel">Preset view model (and associated model)
        /// to add to the repository.</param>
        public PresetsRepositoryViewModel(PresetViewModel presetViewModel)
            : this()
        {
            Presets.Add(presetViewModel);
        }

        public PresetsRepositoryViewModel(PresetsRepository repository)
            : base()
        {
            _repository = repository;
            Presets = new PresetViewModelCollection(_repository);
        }

        #endregion

        #region Commands

        public DelegatingCommand AddSettingsCommand
        {
            get
            {
                if (_addSettingsCommand == null)
                {
                    _addSettingsCommand = new DelegatingCommand(
                        (param) => DoAddSettings());
                }
                return _addSettingsCommand;
            }
        }

        public DelegatingCommand RemoveSettingsCommand
        {
            get
            {
                if (_removeSettingsCommand == null)
                {
                    _removeSettingsCommand = new DelegatingCommand(
                        (param) => DoDeleteSettings(),
                        (param) => CanDeleteSettings());
                }
                return _removeSettingsCommand;
            }
        }

        public DelegatingCommand EditSettingsCommand
        {
            get
            {
                if (_editSettingsCommand == null)
                {
                    _editSettingsCommand = new DelegatingCommand(
                        (param) => DoEditSettings(),
                        (param) => CanEditSettings());
                }
                return _editSettingsCommand;
            }
        }

        #endregion

        #region Messages

        public Message<MessageContent> ConfirmRemoveMessage
        {
            get
            {
                if (_confirmRemoveMessage == null)
                {
                    _confirmRemoveMessage = new Message<MessageContent>();
                };
                return _confirmRemoveMessage;
            }
        }

        /// <summary>
        /// Sends a message indicating that a particular view model
        /// should be viewed for editing. The ExportSettingsViewModel object
        /// is conveyed in the message content.
        /// </summary>
        public Message<ViewModelMessageContent> EditSettingsMessage
        {
            get
            {
                if (_editSettingsMessage == null)
                {
                    _editSettingsMessage = new Message<ViewModelMessageContent>();
                };
                return _editSettingsMessage;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Selects the last used preset as stored in the workbook's
        /// WorkbookStorage area or in the user settings, if any.
        /// Selects the first preset in the collection if no previously
        /// stored preset is found.
        /// </summary>
        /// <param name="workbook">Workbook whose stored settings to
        /// search for a previously used preset.</param>
        /// <exception cref="InvalidOperationException">If no presets
        /// exist in the collection.</exception>
        public void SelectLastUsedOrDefault(Workbook workbook)
        {
            if (Presets.Count == 0)
            {
                throw new InvalidOperationException(
                    "Cannot select a preset because there are no presets in the collection.");
            }

            Store workbookStore = new Store(workbook);
            string presetName = workbookStore.Get(
                STORAGEKEY,
                Properties.Settings.Default.ExportPreset);
            PresetViewModel pvm = null;
            if (!String.IsNullOrEmpty(presetName))
            {
                pvm = Presets.FirstOrDefault(p => p.Name == presetName);
            }
            if (pvm != null)
            {
                pvm.IsSelected = true;
            }
            else
            {
                Presets[0].IsSelected = true;
            }
        }

        /// <summary>
        /// Stores the name of the last selected preset in the workbook's
        /// storage area and in the user settings.
        /// </summary>
        /// <param name="workbook">Workbook to store the preset in.</param>
        public void SaveLastUsed(Workbook workbook)
        {
            if (LastSelected != null)
            {
                Store store = new Store(workbook);
                store.Put(STORAGEKEY, LastSelected.Name);
                Properties.Settings.Default.ExportPreset = LastSelected.Name;
                Properties.Settings.Default.Save();
            }
        }

        #endregion

        #region Private methods

        private void DoAddSettings()
        {
            Preset s = new Preset();
            PresetViewModel svm = new PresetViewModel(s);
            Presets.Add(svm);
            svm.IsSelected = true;
            OnPropertyChanged("ExportSettings");
        }

        private void DoDeleteSettings()
        {
            ConfirmRemoveMessage.Send(
                new MessageContent(),
                content => ConfirmDeleteSettings(content)
            );
        }

        private void ConfirmDeleteSettings(MessageContent messageContent)
        {
            if (CanDeleteSettings() && messageContent.Confirmed)
            {
                this.Presets.RemoveSelected();
                OnPropertyChanged("ExportSettings");
            }
        }

        private bool CanDeleteSettings()
        {
            return (this.Presets.CountSelected > 0);
        }

        private void DoEditSettings()
        {
            EditSettingsMessage.Send(
                new ViewModelMessageContent(Presets.LastSelected),
                content => OnPropertyChanged("ExportSettings")
            );
        }

        private bool CanEditSettings()
        {
            return (this.Presets.CountSelected > 0);
        }

        private void PresetsRepositoryViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LastSelected")
            {
                LastSelected = Presets.LastSelected;
            }
        }

        #endregion

        #region Private fields

        PresetsRepository _repository;
        DelegatingCommand _addSettingsCommand;
        DelegatingCommand _removeSettingsCommand;
        DelegatingCommand _editSettingsCommand;
        Message<MessageContent> _confirmRemoveMessage;
        Message<ViewModelMessageContent> _editSettingsMessage;

        #endregion

        #region Private contants

        private const string STORAGEKEY = "ExportPreset";

        #endregion

        #region Implementation of ViewModelBase's abstract methods

        public override object RevealModelObject()
        {
            return _repository;
        }

        #endregion
    }
}
