using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Controls;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using ProAppModule1.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ProAppModule1
{
    internal class Dockpane1ViewModel : DockPane
    {
        private const string _dockPaneID = "ProAppModule1_Dockpane1";
        private readonly PanelHistoryViewModel _panelHistoryVm;

        public List<TabControl> PrimaryMenuList { get; } = new List<TabControl>();
        private int _selectedPanelHeaderIndex = 0;
        public int SelectedPanelHeaderIndex
        {
            get => _selectedPanelHeaderIndex;
            set
            {
                SetProperty(ref _selectedPanelHeaderIndex, value, () => SelectedPanelHeaderIndex);
                if (_selectedPanelHeaderIndex == 0)
                {
                    CurrentPage = _panelHistoryVm;
                }
            }
        }

        private PanelViewModelBase _currentPage;
        public PanelViewModelBase CurrentPage
        {
            get => _currentPage;
            private set
            {
                SetProperty(ref _currentPage, value, () => CurrentPage);
            }
        }

        protected Dockpane1ViewModel()
        {
            PrimaryMenuList.Add(new TabControl() { Text = "Historiser une couche", Tooltip = "Historiser une couche" });

            _panelHistoryVm = new PanelHistoryViewModel(this);

            SelectedPanelHeaderIndex = 0;
        }

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "My DockPane";
        public string Heading
        {
            get { return _heading; }
            set
            {
                SetProperty(ref _heading, value, () => Heading);
            }
        }
    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class Dockpane1_ShowButton : Button
    {
        protected override void OnClick()
        {
            Dockpane1ViewModel.Show();
        }
    }
}
