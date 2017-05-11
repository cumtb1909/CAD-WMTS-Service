using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using System.Reflection;

[assembly: CommandClass(typeof(AutoCADLinkWTMS.CommandClass))]

namespace AutoCADLinkWTMS
{
    public class CommandClass:IExtensionApplication
    {
        Editor _edit;
        public void Initialize()
        {
            _edit = Application.DocumentManager.MdiActiveDocument.Editor;
            _edit.WriteMessage("Initialize");
            new DrawImage();
         }
        public void Terminate()
        {
            _edit.WriteMessage("Terminate");
        }
        [CommandMethod("cc")]
        public void cc()
        {
            string s1 = Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWCTR").ToString();//视口中心点
            string s2 = Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("SCREENSIZE").ToString();//视口大小，像素级别
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.CurrentDocument.Editor.WriteMessage("\ncenter_point:" + s1 + "screen_size:" + s2);
            Document document = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            document.GraphicsManager.ViewToBeUpdated += GraphicsManager_ViewToBeUpdated;
            

            //using (Autodesk.AutoCAD.DatabaseServices.Database dwg = Application.DocumentManager.CurrentDocument.Database)
            //{
            //    using (Autodesk.AutoCAD.DatabaseServices.Transaction tr = dwg.TransactionManager.StartTransaction())
            //    {
            //        Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.CurrentDocument.Editor;
            //        Autodesk.AutoCAD.DatabaseServices.Viewport view_port = (Autodesk.AutoCAD.DatabaseServices.Viewport)tr.GetObject(ed.CurrentViewportObjectId, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
            //        Point2d p1 = view_port.ViewCenter;
            //        Point3d p2 = ed.GetCurrentView().Target;
            //        ed.WriteMessage("p1:" + p1.ToString() + "p2:" + p2.ToString());
            //    }
            //}
        }

        void GraphicsManager_ViewToBeUpdated(object sender, Autodesk.AutoCAD.GraphicsSystem.ViewUpdateEventArgs e)
        {
            Point3d position = e.View.Position;
            Matrix3d scren_matrix = e.View.ScreenMatrix;
            Point3d target = e.View.Target;
            Autodesk.AutoCAD.DatabaseServices.Extents2d viewport_extent =   e.View.ViewportExtents;
            int i = 0;
        }

    }
}

