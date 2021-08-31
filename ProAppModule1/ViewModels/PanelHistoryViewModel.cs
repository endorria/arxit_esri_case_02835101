using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework;
using ProAppModule1.Models;
using ProAppModule1.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProAppModule1.ViewModels
{
    internal class PanelHistoryViewModel : PanelViewModelBase
    {
        #region private properties
        private readonly Dockpane1ViewModel _parent;
        #endregion

        #region public properties
        public override string DisplayName => "Historisation";

        public RelayCommand LoadShortDescriptionCommand => new RelayCommand(async () =>
        {
            if (string.IsNullOrEmpty(TableName))
            {
                return;
            }
            SelectedShortDefinition = await HistorizationService.GetShortDefinition(TableName);
        });

        public RelayCommand CancelFormCommand => new RelayCommand(() =>
        {
            SelectedShortDefinition = null;
            IdField = null;
            LabelField = null;
            LabelField2 = null;
            StartDate = DateTime.Now;
            Description = string.Empty;
        });

        public RelayCommand LaunchHistorizationCommand => new RelayCommand(LaunchHistorization);

        private bool _isNotLoading = true;

        public bool IsNotLoading
        {
            get => _isNotLoading;
            set
            {
                SetProperty(ref _isNotLoading, value, () => IsNotLoading);
            }
        }

        private string _tableName;
        public string TableName
        {
            get => _tableName;
            set
            {
                SetProperty(ref _tableName, value, () => TableName);
            }
        }

        private TableShortDefinition _selectedShortDefinition;
        public TableShortDefinition SelectedShortDefinition
        {
            get => _selectedShortDefinition;
            set
            {
                SetProperty(ref _selectedShortDefinition, value, () => SelectedShortDefinition);
                CheckIfFormIsValid();
            }
        }

        private Field _idField;
        public Field IdField
        {
            get => _idField;
            set
            {
                SetProperty(ref _idField, value, () => IdField);
                CheckIfFormIsValid();
            }
        }

        private Field _labelField;
        public Field LabelField
        {
            get => _labelField;
            set
            {
                SetProperty(ref _labelField, value, () => LabelField);
                CheckIfFormIsValid();
            }
        }

        private Field _labelField2;
        public Field LabelField2
        {
            get => _labelField2;
            set
            {
                SetProperty(ref _labelField2, value, () => LabelField2);
            }
        }

        private DateTime _startDate = DateTime.Now;
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                SetProperty(ref _startDate, value, () => StartDate);
                CheckIfFormIsValid();
            }
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set
            {
                SetProperty(ref _description, value, () => Description);
            }
        }

        private bool _isFormValid = true;
        public bool IsFormValid
        {
            get => _isFormValid;
            set
            {
                SetProperty(ref _isFormValid, value, () => IsFormValid);
            }
        }
        #endregion

        public PanelHistoryViewModel(Dockpane1ViewModel parent)
        {
            _parent = parent;
        }

        #region public methods
        #endregion

        #region private methods

        private async void LaunchHistorization()
        {
            IsNotLoading = false;

            try
            {
                await HistorizationService.HistorizeTable(
                    SelectedShortDefinition.TableName,
                    Description,
                    StartDate,
                    Utils.GetLogin(),
                    IdField.Name,
                    LabelField.Name,
                    LabelField2 == null ? string.Empty : LabelField2.Name);


            }
            catch (Exception ex)
            {
            }
            finally
            {
                IsNotLoading = true;
                ResetForm();
            }
        }

        private void ResetForm()
        {
            SelectedShortDefinition = null;
            IdField = null;
            LabelField = null;
            LabelField2 = null;
            StartDate = DateTime.Now;
            Description = string.Empty;
        }

        private void CheckIfFormIsValid()
        {
            IsFormValid = SelectedShortDefinition != null &&
                          IdField != null &&
                          LabelField != null &&
                          StartDate != default;
        }
        #endregion
    }
}
