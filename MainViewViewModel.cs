using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Prism.Commands;
using RevitAPIUILibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Mechanical;

namespace RevitAPIUI3
{
    public class MainViewViewModel
    {
        private ExternalCommandData _commandData;

        public DelegateCommand SaveCommand { get; }
        public List<DuctType> DuctTypes { get; } = new List<DuctType>();
        public List<Level> DuctLevels { get; } = new List<Level>();
        public DuctType SelectedDuctType { get; set; }
        public Level SelectedLevel { get; set; }
        public double DuctValue { get; set; }
        public List<XYZ> Points { get; } = new List<XYZ>();
        
        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            SaveCommand = new DelegateCommand(OnSaveCommand);
            DuctTypes = DuctUtils.GetDuctTypes(commandData);
            DuctLevels = LevelsUtils.GetLevels(commandData);
            DuctValue = 100;
            Points = SelectionUtils.GetPoints(_commandData, "Выберите точки", ObjectSnapTypes.Endpoints);
            
        }

        private void OnSaveCommand()
        {
            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            
            if (Points.Count < 2 || SelectedDuctType == null || SelectedLevel == null)
                return;

            MEPSystemType mepSystemType = new FilteredElementCollector(doc)
                                                    .OfClass(typeof(MEPSystemType))
                                                    .Cast<MEPSystemType>()
                                                    .FirstOrDefault(sysType => sysType.SystemClassification == MEPSystemClassification.SupplyAir);

            var curves = new List<Curve>();
            for (int i = 0; i < Points.Count; i++)
            {
                if (i == 0)
                    continue;

                var prevPoint = Points[i - 1];
                var currentPoint = Points[i];

                Curve curve = Line.CreateBound(prevPoint, currentPoint);
                XYZ curve1 = prevPoint;
                XYZ curve2 = currentPoint;
                curves.Add(curve);
                
                using (var ts = new Transaction(doc, "Create Duct"))
                {
                    ts.Start();
                    {
                        Duct duct = Duct.Create(doc, mepSystemType.Id, SelectedDuctType.Id, SelectedLevel.Id, curve1, curve2);
                        Parameter value = duct.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                        value.Set(UnitUtils.ConvertToInternalUnits(DuctValue, UnitTypeId.Millimeters));
                    }
                    ts.Commit();
                }
            }
            RaiseCloseRequest();
        }

        public event EventHandler CloseRequest;
        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }
    }
}
