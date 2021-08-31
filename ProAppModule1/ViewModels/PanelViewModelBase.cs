using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProAppModule1.ViewModels
{
    internal abstract class PanelViewModelBase : PropertyChangedBase
    {
        public abstract string DisplayName { get; }
    }
}
