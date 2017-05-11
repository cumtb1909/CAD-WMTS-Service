using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Net;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.ComponentModel;
using Autodesk.AutoCAD.Windows.ToolPalette;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.GraphicsInterface;


namespace AutoCADLinkWTMS
{
    class DrawImage
    {
        double _viewSize = 0;
        Point3d _viewCtr = new Point3d(0, 0, 0);
        Point3d _viewDir= new Point3d(0,0,0);
        public static DBObjectCollection _transients = null;

        int _zoom_wheel = 0;//记录前滚后滚 1 前缩小，后放大，0 反之
        int _level = 0;//显示级别
        int _zoomfactor = 3;//zoom 鼠标滚轮的灵敏度设置
        int _zoom_message = 0;//记录鼠标滚动消息的次数

        Point3d _center_point = new Point3d();
        Point2d _screenSize = new Point2d();
        string _layer_name = "BaseMap";

        public  DrawImage()
        {
            _zoomfactor = int.Parse(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("ZOOMFACTOR").ToString());
            _zoom_wheel =int.Parse(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("ZOOMWHEEL").ToString());
            Autodesk.AutoCAD.ApplicationServices.Application.SystemVariableChanged += Application_SystemVariableChanged;
            Autodesk.AutoCAD.ApplicationServices.Application.PreTranslateMessage += Application_PreTranslateMessage;
            Document dwg = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.CurrentDocument;
            MapTiles.darwBounds();
            add0Level();
            dwg.ViewChanged += dwg_ViewChanged;

        }

        ~DrawImage()
        {
           
        }

        /// <summary>
        /// AutoCAD 系统变量发生变化,鼠标灵敏度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Application_SystemVariableChanged(object sender, Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs e)
        {
            if (0 == e.Name.CompareTo("ZOOMFACTOR"))
            {
                _zoomfactor = int.Parse(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("ZOOMFACTOR").ToString());
            }
        }

        /// <summary>
        /// 消息分发前事件  监听滚轮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Application_PreTranslateMessage(object sender, PreTranslateMessageEventArgs e)
        {
            if ((e.Message.message == 522 && e.Message.wParam != IntPtr.Zero))//WM_MOUSEWHEEL
            {
               _zoom_wheel = int.Parse(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("ZOOMWHEEL").ToString());
               _center_point = (Point3d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWCTR");//视口中心点
               _screenSize = (Point2d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("SCREENSIZE");//视口大小，像素级别
               if (0 == _zoom_wheel)
               {
                   if (0 == e.Message.wParam.ToString().CompareTo("4287102976"))
                   {
                       //后滚 缩小
                       zoomLevel(false);
                   }
                   if (0 == e.Message.wParam.ToString().CompareTo("7864320"))
                   {
                       //前滚 放大
                       zoomLevel(true);

                   }
               }
               else if(1 == _zoom_wheel)
               {
                   if (0 == e.Message.wParam.ToString().CompareTo("4287102976"))
                   {
                       //前滚 缩小
                       zoomLevel(false);
                   }
                   if (0 == e.Message.wParam.ToString().CompareTo("7864320"))
                   {
                       //后滚 放大
                       zoomLevel(true);
                   }
               }
            }
        }

        /// <summary>
        /// 视口变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dwg_ViewChanged(object sender, EventArgs e)
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.CurrentDocument;
            using (document.LockDocument(DocumentLockMode.ProtectedAutoWrite, null, null, false))
            {
                Editor editor = document.Editor;
                //Size win_size = document.Window.GetSize();
                //Point location = document.Window.GetLocation();
                //editor.WriteMessage("\nwin_size:" + win_size.ToString() + "location:" + location.ToString());

                if (1 == _level || 0 == _level)
                {
                    return;
                }
                ViewTableRecord currentView = editor.GetCurrentView();
                Point2d view_center_point = currentView.CenterPoint;
                Size view_size = new Size((int)currentView.Width, (int)currentView.Height);
                //editor.WriteMessage("\ncenter_point:" + view_center_point.ToString() + "view_size:" + view_size.ToString());
                Point2d left_down = new Point2d(view_center_point.X - view_size.Width/2.0,view_center_point.Y-view_size.Height/2.0);
                Point2d right_up = new Point2d(view_center_point.X + view_size.Width/2.0,view_center_point.Y+view_size.Height/2.0);

                //Point3d center_point3d = (Point3d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWCTR");
                //Point2d size_screen = (Point2d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("SCREENSIZE");
                //Size size = new Size((int)size_screen.X, (int)size_screen.Y);
                //Point2d center_point = new Point2d(center_point3d.X, center_point3d.Y);
                //MapTiles.webMercatorBounds(center_point, _level, size, ref left_down, ref right_up);

                //document.Editor.WriteMessage("\n CenterPoint:" + center_point.ToString() + "left_down:" + left_down.ToString() + "right_up:" + right_up.ToString() + "level:" + _level.ToString());
                //RectangleF view_image = new RectangleF();
                //bool is_contain = MapTiles.getIntersects(left_down, right_up, ref view_image);
                //if (!is_contain)
                //{
                //    return;
                //}
                //else
                //{
                //    left_down = new Point2d(view_image.X, view_image.Y-view_image.Height);
                //    right_up = new Point2d(view_image.X+view_image.Width, view_image.Y);
                //    MapTiles.darwBounds(left_down,right_up,Autodesk.AutoCAD.Colors.Color.FromRgb(0,255,0),20,20);

                //}
                int start_row = 0, end_row = 0, start_col = 0, end_col = 0;
                MapTiles.webMercatorTilesFromBound(left_down, right_up, _level, out start_row, out start_col, out end_row, out end_col);
                
                addImage(document, MapTiles.webMercatorResolution(_level), start_row, end_row, start_col, end_col);
            }
        }

        void zoomLevel(bool is_zoom_in)
        {

            if (is_zoom_in) // Scoll up or down?
            {
                if (_level >= 0 && _level < 20)
                {//放大
                    ++_level;
                }
            }
            else
            {
                if (_level <= 20 && _level >= 1)
                {//缩小
                    --_level;
                }
            }
        }


        /// <summary>
        /// 加载 首级别的影像
        /// </summary>
        public void add0Level()
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.CurrentDocument;
            using (document.LockDocument(DocumentLockMode.ProtectedAutoWrite, null, null, false))
            {
                _level = 1;
                addImage(document, MapTiles.webMercatorResolution(1), 0, 1, 0, 1);
            }
            //Point2d point_left_up = MapTiles.webMercatorTilesLeftUpLocation(0,0,1);
            //string url = String.Format("http://t3.tianditu.cn/img_c/wmts?service=wmts&request=GetTile&version=1.0.0&LAYER=img&tileMatrixSet=c&TileMatrix={0}&TileRow={1}&TileCol={2}&style=default&format=tiles", 1, 0, 0);
            addImage(document,MapTiles.webMercatorResolution(1),0,1,0,1);
            //point_left_up = MapTiles.webMercatorTilesLeftUpLocation(1, 0, 1);
            //string url_01 = String.Format("http://t3.tianditu.cn/img_c/wmts?service=wmts&request=GetTile&version=1.0.0&LAYER=img&tileMatrixSet=c&TileMatrix={0}&TileRow={1}&TileCol={2}&style=default&format=tiles", 1, 0, 1);
            //addImage(document, url_01, point_left_up,MapTiles.webMercatorResolution(1));            
            ZoomExtents();
        }


        void editor_PointMonitor(object sender, PointMonitorEventArgs e)
        {

            Document activeDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            double viewSize
                = (double)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWSIZE");

            Point3d viewCtr
                = (Point3d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWCTR");

            Point3d viewDir
                = (Point3d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWDIR");

            // Simple check to verify if the view parameters changed
            // since we last drew the transient graphics
            if (viewSize != _viewSize ||
                    viewCtr.Equals(_viewCtr) == false ||
                    viewDir.Equals(_viewDir) == false
                )
            {
                _viewSize = viewSize;

                _viewCtr  = (Point3d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWCTR");

                _viewDir = viewDir;
                Editor edit = sender as Editor;
                edit.WriteMessage(_viewCtr.ToString());
                // Draw the transient graphics again since
                // the view parameters seem to have changed
                // DrawTransientRectMethod();
            }
            //throw new NotImplementedException();
        }

        //private void DrawTransientRectMethod()
        //{
        //    Document activeDoc
        //    = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        //    Database db = activeDoc.Database;
        //    Editor ed = activeDoc.Editor;

        //    // Clear the previous transient graphics
        //    ClearTransientGraphics();

        //    Point2d screenSize
        //        = (Point2d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("SCREENSIZE");

        //    // Width and height of the rectangle
        //    int width = 20;
        //    int height = 60;

        //    // Four corner points in screen coordinates
        //    System.Drawing.Point upperLeft
        //                        = new System.Drawing.Point(20, 20);

        //    System.Drawing.Point upperRight
        //        = new System.Drawing.Point(upperLeft.X + width, upperLeft.Y);

        //    System.Drawing.Point lowerLeft
        //        = new System.Drawing.Point(upperLeft.X, upperLeft.Y + height);

        //    System.Drawing.Point lowerRight
        //        = new System.Drawing.Point(upperLeft.X + width, upperLeft.Y + height);

        //    // Four corner points in WCS
        //    Point3d upperLeftWorld = ed.PointToWorld(upperLeft, 0);
        //    Point3d upperRightWorld = ed.PointToWorld(upperRight, 0);
        //    Point3d lowerLeftWorld = ed.PointToWorld(lowerLeft, 0);
        //    Point3d lowerRightWorld = ed.PointToWorld(lowerRight, 0);

        //    // Create the transient entities
        //    Line l1 = new Line(upperLeftWorld, upperRightWorld);
        //    l1.ColorIndex = 2;
        //    Line l2 = new Line(upperRightWorld, lowerRightWorld);
        //    l2.ColorIndex = 2;
        //    Line l3 = new Line(lowerRightWorld, lowerLeftWorld);
        //    l3.ColorIndex = 2;
        //    Line l4 = new Line(lowerLeftWorld, upperLeftWorld);
        //    l4.ColorIndex = 2;

        //    _transients.Add(l1);
        //    _transients.Add(l2);
        //    _transients.Add(l3);
        //    _transients.Add(l4);

        //    IntegerCollection intCol = new IntegerCollection();
        //    TransientManager tm = TransientManager.CurrentTransientManager;
        //    tm.AddTransient
        //        (
        //            l1,
        //            TransientDrawingMode.Main,
        //            128,
        //            intCol
        //        );

        //    tm.AddTransient
        //        (
        //            l2,
        //            TransientDrawingMode.Main,
        //            128,
        //            intCol
        //        );

        //    tm.AddTransient
        //        (
        //            l3,
        //            TransientDrawingMode.Main,
        //            128,
        //            intCol
        //        );

        //    tm.AddTransient
        //        (
        //            l4,
        //            TransientDrawingMode.Main,
        //            128,
        //            intCol
        //        );
        //}

        // Erases any transient graphics
        //void ClearTransientGraphics()
        //{
        //    TransientManager tm
        //            = TransientManager.CurrentTransientManager;
        //    IntegerCollection intCol = new IntegerCollection();
        //    if (_transients != null)
        //    {
        //        foreach (DBObject transient in _transients)
        //        {
        //            tm.EraseTransient(
        //                                transient,
        //                                intCol
        //                                );
        //            transient.Dispose();
        //        }
        //        _transients.Clear();
        //    }
        //    else
        //        _transients = new DBObjectCollection();
        //}

        void editor_Dragging(object sender, DraggingEventArgs e)
        {
            int i = 0;
            Document document = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.CurrentDocument;
            Editor editor = document.Editor;
            editor.WriteMessage(e.ToString()+"\n"+sender.ToString());
            //throw new NotImplementedException();
        }

        public void BindChange()
        {
            Document dwg = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.CurrentDocument;
            dwg.ViewChanged += dwg_ViewChanged;
        }
        public void UnBindChange()
        {
            Document dwg = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.CurrentDocument;
            dwg.ViewChanged -= dwg_ViewChanged;
        }


        /// <summary>
        /// 获取当前视图范围
        /// </summary>
        public void drawRect()
        {


            //Document document = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.CurrentDocument;
            
            //ViewportTableRecord vptr = getCurrentViewPort(document);
            //Extents2d rect = new Extents2d(vptr.LowerLeftCorner,vptr.UpperRightCorner);
            //Extents3d rect3d;
            //GetActiveViewportExtent(document, out rect3d);
            //double width = vptr.Width;
            //double length = vptr.Height;
            //Point3d point3d = (Point3d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWCTR");//视口中心点
            //double num = (double)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWSIZE");//视口大小
            //Point3d point_vsmax = (Point3d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VSMAX");//存储当前视口虚屏的右上角
            //Point3d point_vsmin = (Point3d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VSMIN");//存储当前视口虚屏的左下角

            //Database db = HostApplicationServices.WorkingDatabase;
            //using (Transaction tran = db.TransactionManager.StartTransaction())
            //{
            //    BlockTableRecord mspace = tran.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
            //    Point3dCollection point_collection = new Point3dCollection();
            //    point_collection.Add(new Point3d(rect.MinPoint.X,rect.MinPoint.Y,0));
            //    point_collection.Add(new Point3d(rect.MaxPoint.X,rect.MinPoint.Y,0));
            //    point_collection.Add(new Point3d(rect.MaxPoint.X, rect.MaxPoint.Y, 0));
            //    point_collection.Add(new Point3d(rect.MinPoint.X, rect.MaxPoint.Y, 0));
            //    point_collection.Add(new Point3d(rect.MinPoint.X, rect.MinPoint.Y, 0));
            //    Polyline3d polyline3d = new Polyline3d(Poly3dType.SimplePoly, point_collection, true);
            //    mspace.AppendEntity(polyline3d);
            //    tran.AddNewlyCreatedDBObject(polyline3d, true);
            //    tran.Commit();
            //}
        }

        

        public void transactionsTest()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using(Transaction tran = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord mspace = tran.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                Line line = new Line();
                line.StartPoint = new Point3d(0, 0, 0);
                line.EndPoint = new Point3d(3, 3, 3);
                mspace.AppendEntity(line);
                tran.AddNewlyCreatedDBObject(line, true);
                tran.Commit();
            }
            
        }

        public ObjectId GetLayer(Database db, Transaction t, ref string layerName)
        {
            ObjectId result = db.Clayer;
            if (!string.IsNullOrEmpty(layerName))
            {
                //layerName = DocUtil.FixLayerName(layerName);
                LayerTable layerTable = (LayerTable)t.TransactionManager.GetObject(db.LayerTableId, OpenMode.ForWrite, false);
                if (layerTable.Has(layerName))
                {
                    LayerTableRecord acLyrTblRec = t.GetObject(layerTable[layerName], OpenMode.ForWrite) as LayerTableRecord;
                    result = layerTable[layerName];
                }
                else
                {
                    LayerTableRecord layerTableRecord = new LayerTableRecord();
                    layerTableRecord.Name = layerName;
                    result = layerTable.Add(layerTableRecord);
                    t.TransactionManager.AddNewlyCreatedDBObject(layerTableRecord, true);
                }
            }
            return result;
        }

        void lockLayerOrNot(string layer_name,bool is_lock)
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            using (document.LockDocument(DocumentLockMode.ProtectedAutoWrite, null, null, false))
            {
                using (Transaction tran = document.TransactionManager.StartTransaction())
                {
                    LayerTable layer_table = tran.GetObject(document.Database.LayerTableId, OpenMode.ForWrite) as LayerTable;
                    if (layer_table.Has(layer_name))
                    {
                        LayerTableRecord ltr = tran.GetObject(layer_table[layer_name], OpenMode.ForWrite) as LayerTableRecord;
                        ltr.IsLocked = is_lock;
                    }
                    tran.Commit();
                }
            }
        }

        public void ZoomExtents(Point3d minPoint, Point3d maxPoint)
        {
            try
            {
                Document document = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.CurrentDocument;
                Editor editor = document.Editor;
                ViewTableRecord currentView = editor.GetCurrentView();
                double num = currentView.Width / currentView.Height;
                double num2 = maxPoint.X - minPoint.X;
                double num3 = maxPoint.Y - minPoint.Y;
                if (num2 > num3 * num)
                {
                    num3 = num2 / num;
                }
                Point2d centerPoint = new Point2d((maxPoint.X + minPoint.X) / 2.0, (maxPoint.Y + minPoint.Y) / 2.0);
                currentView.Height = (num3);
                currentView.Width = (num2);
                currentView.CenterPoint = (centerPoint);
                
                editor.SetCurrentView(currentView);
            }
            catch
            {
            }
        }

        public ObjectId DefineRasterImage(Document doc, string url, Point3d basePoint, Vector3d v1, Vector3d v2, string suggestedName, byte transparency)
        {
            ObjectId result;
            try
            {
                System.Drawing.Image.FromStream(new WebClient().OpenRead(url));
            }
            catch
            {
                result = ObjectId.Null;
                return result;
            }
            Database database = doc.Database;
            Editor editor = doc.Editor;
            ObjectId objectId = ObjectId.Null;
            ObjectId arg_39_0 = ObjectId.Null;
            try
            {
                using (doc.LockDocument(DocumentLockMode.ProtectedAutoWrite, null, null, false))
                {
                    Autodesk.AutoCAD.ApplicationServices.TransactionManager transactionManager = doc.TransactionManager;
                    doc.TransactionManager.EnableGraphicsFlush(true);
                    using (Transaction transaction = transactionManager.StartTransaction())
                    {
                        string.IsNullOrEmpty(suggestedName);
                        ObjectId objectId2 = RasterImageDef.GetImageDictionary(database);
                        if (objectId2.IsNull)
                        {
                            objectId2 = RasterImageDef.CreateImageDictionary(database);
                        }
                        RasterImageDef rasterImageDef = new RasterImageDef();
                        rasterImageDef.SourceFileName = (url);
                        rasterImageDef.Load();
                        bool arg_A4_0 = rasterImageDef.IsLoaded;
                        DBDictionary dBDictionary = (DBDictionary)transaction.GetObject(objectId2, OpenMode.ForWrite);
                        string text = RasterImageDef.SuggestName(dBDictionary, url);
                        if (!string.IsNullOrEmpty(suggestedName))
                        {
                            text = suggestedName;
                            int num = 0;
                            while (dBDictionary.Contains(text))
                            {
                                num++;
                                text = suggestedName + num;
                            }
                        }
                        ObjectId arg_F8_0 = ObjectId.Null;
                        if (dBDictionary.Contains(text))
                        {
                            //editor.WriteMessage(AfaStrings.ImageAlreadyExits);
                            result = ObjectId.Null;
                            return result;
                        }
                        dBDictionary.SetAt(text, rasterImageDef);
                        transaction.AddNewlyCreatedDBObject(rasterImageDef, true);
                        dBDictionary.Contains(text);
                        ObjectId layer = GetLayer(database, transaction, ref text);
                        RasterImage rasterImage = new RasterImage();
                        rasterImage.ImageDefId = (rasterImageDef.ObjectId);
                        rasterImage.SetLayerId(layer,false);
                        byte b = Convert.ToByte(Math.Floor((100.0 - (double)transparency) / 100.0 * 254.0));
                        Transparency transparency2 = new Transparency(b);
                        rasterImage.Transparency = transparency2;
;                       rasterImage.Orientation = new CoordinateSystem3d(basePoint, v1, v2);
                        BlockTable blockTable = (BlockTable)transactionManager.GetObject(database.BlockTableId, 0, false);
                        BlockTableRecord blockTableRecord = (BlockTableRecord)transactionManager.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false);
                        int num2 = 0;
                        try
                        {
                            num2 = blockTableRecord.Cast<object>().Count<object>();
                        }
                        catch
                        {
                        }
                        rasterImage.ColorIndex = (256);
                        objectId = blockTableRecord.AppendEntity(rasterImage);
                        transactionManager.AddNewlyCreatedDBObject(rasterImage, true);
                        rasterImage.AssociateRasterDef(rasterImageDef);
                        RasterImage.EnableReactors(true);
                        rasterImageDef.UpdateEntities();
                        DrawOrderTable drawOrderTable = (DrawOrderTable)transaction.GetObject(blockTableRecord.DrawOrderTableId, OpenMode.ForWrite);
                        ObjectIdCollection objectIdCollection = new ObjectIdCollection();
                        objectIdCollection.Add(objectId);
                        drawOrderTable.MoveToBottom(objectIdCollection);
                        try
                        {
                            rasterImageDef.UpdateEntities();
                            if (num2 == 0)
                            {
                                ZoomExtents(rasterImage.GeometricExtents.MinPoint, rasterImage.GeometricExtents.MaxPoint);
                                editor.WriteMessage(rasterImage.Orientation.Origin.ToString());
                            }
                        }
                        catch
                        {
                        }
                        transaction.Commit();
                    }
                }
                result = objectId;
            }
            catch (System.Exception ex)
            {
                result = ObjectId.Null;
            }
            catch
            {
                //rrorReport.ShowErrorMessage(AfaStrings.UnexpectedErrorInAddingRasterImage);
                result = ObjectId.Null;
            }
            return result;
        }

        public ObjectId addImage(Document doc,double resulotion,int start_row,int end_row,int start_col,int end_col)
        {
            lockLayerOrNot(_layer_name, false);
            Database database = doc.Database;
            Editor editor = doc.Editor;
            ObjectId result;
            //try
            //{
            //    System.Drawing.Image.FromStream(new WebClient().OpenRead(url));
            //}
            //catch
            //{
            //    result = ObjectId.Null;
            //    editor.WriteMessage("Url is invalid!");
            //    return result;
            //}
           
            ObjectId objectId = ObjectId.Null;
            try
            {
                using (doc.LockDocument(DocumentLockMode.ProtectedAutoWrite, null, null, false))
                {
                    Autodesk.AutoCAD.ApplicationServices.TransactionManager transactionManager = doc.TransactionManager;
                    doc.TransactionManager.EnableGraphicsFlush(true);
                    using (Transaction transaction = transactionManager.StartTransaction())
                    {
                        ObjectId objectId2 = RasterImageDef.GetImageDictionary(database);
                        if (objectId2.IsNull)
                        {
                            objectId2 = RasterImageDef.CreateImageDictionary(database);
                        }                       
                        //rasterImageDef.ResolutionMMPerPixel = new Vector2d(78217.51696, 78217.51696);
                        DBDictionary dBDictionary = (DBDictionary)transaction.GetObject(objectId2, OpenMode.ForWrite);
                        dBDictionary.Erase();
                        for (int i = start_row; i <= end_row; ++i)
                        {
                            for (int j = start_col; j <= end_col; ++j)
                            {
                                Point2d left_up = MapTiles.webMercatorTilesLeftUpLocation(j, i, _level);
                                string url = string.Format("http://t3.tianditu.cn/img_c/wmts?service=wmts&request=GetTile&version=1.0.0&LAYER=img&tileMatrixSet=c&TileMatrix={0}&TileRow={1}&TileCol={2}&style=default&format=tiles", _level, i, j);
                                RasterImageDef rasterImageDef = new RasterImageDef();
                                rasterImageDef.SourceFileName = (url);
                                rasterImageDef.Load();
                                bool arg_A4_0 = rasterImageDef.IsLoaded;

                                string text = RasterImageDef.SuggestName(dBDictionary, url);
                                if (dBDictionary.Contains(text))
                                {
                                    result = ObjectId.Null;
                                    return result;
                                }
                                dBDictionary.SetAt(text, rasterImageDef);
                                transaction.AddNewlyCreatedDBObject(rasterImageDef, true);
                                string layer_name = _layer_name;
                                ObjectId layer = GetLayer(database, transaction, ref layer_name);
                                
                                RasterImage rasterImage = new RasterImage();
                                rasterImage.ImageDefId = (rasterImageDef.ObjectId);
                                rasterImage.SetLayerId(layer, false);
                                //byte b = Convert.ToByte(Math.Floor((100.0 - (double)transparency) / 100.0 * 254.0));
                                Transparency transparency2 = new Autodesk.AutoCAD.Colors.Transparency(100);
                                rasterImage.Transparency = transparency2;
                                rasterImage.Orientation = new CoordinateSystem3d(new Point3d(left_up.X, left_up.Y, 0), new Vector3d(resulotion * 256, 0, 0), new Vector3d(0, resulotion * 256, 0));
                                BlockTable blockTable = (BlockTable)transactionManager.GetObject(database.BlockTableId, 0, false);
                                BlockTableRecord blockTableRecord = (BlockTableRecord)transactionManager.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false);

                                rasterImage.ColorIndex = (256);
                                objectId = blockTableRecord.AppendEntity(rasterImage);
                                transactionManager.AddNewlyCreatedDBObject(rasterImage, true);
                                rasterImage.AssociateRasterDef(rasterImageDef);
                                RasterImage.EnableReactors(true);
                                rasterImageDef.UpdateEntities();
                                try
                                {
                                    rasterImageDef.UpdateEntities();
                                }
                                catch (System.Exception ex)
                                {

                                }
                            
                            }
                        }
                       
                        //DrawOrderTable drawOrderTable = (DrawOrderTable)transaction.GetObject(blockTableRecord.DrawOrderTableId, OpenMode.ForWrite);
                        //ObjectIdCollection objectIdCollection = new ObjectIdCollection();
                        //objectIdCollection.Add(objectId);
                        //drawOrderTable.MoveToBottom(objectIdCollection);
                       
                        transaction.Commit();
                    }
                }
                result = objectId;
            }
            catch (System.Exception ex)
            {
                string message = ex.Message;
                ObjectId arg_2FE_0 = ObjectId.Null;
                result = ObjectId.Null;
            }
            catch
            {
                //rrorReport.ShowErrorMessage(AfaStrings.UnexpectedErrorInAddingRasterImage);
                result = ObjectId.Null;
            }
            lockLayerOrNot(_layer_name,true);
            return result;
        }

        private bool UpdateRasterImage(Document doc, ObjectId rasterId, string url, Point3d basePoint, Vector3d v1, Vector3d v2)
        {
            bool result;
            try
            {
                if (doc == null)
                {
                    result = false;
                }
                else
                {
                    if (doc.IsDisposed)
                    {
                        result = false;
                    }
                    else
                    {
                        if (rasterId.IsEffectivelyErased)
                        {
                            result = false;
                        }
                        else
                        {
                            if (rasterId == ObjectId.Null)
                            {
                                result = false;
                            }
                            else
                            {
                                Database arg_4D_0 = doc.Database;
                                try
                                {
                                    Editor arg_54_0 = doc.Editor;
;
                                    using (doc.LockDocument(DocumentLockMode.Write, null, null, false))
                                    {
                                        doc.TransactionManager.EnableGraphicsFlush(true);
                                        Autodesk.AutoCAD.ApplicationServices.TransactionManager transactionManager = doc.TransactionManager;
                                        using (Transaction transaction = transactionManager.StartTransaction())
                                        {
                                            RasterImage rasterImage = (RasterImage)transaction.GetObject(rasterId, OpenMode.ForWrite);
                                            rasterImage.DisableUndoRecording(true);
                                            ObjectId imageDefId = rasterImage.ImageDefId;
                                            RasterImageDef rasterImageDef = (RasterImageDef)transaction.GetObject(imageDefId, OpenMode.ForWrite);
                                            rasterImageDef.DisableUndoRecording(true);
                                            string sourceFileName = rasterImageDef.SourceFileName;
                                            try
                                            {
                                                rasterImageDef.Unload(true);
                                                rasterImageDef.SourceFileName = url;
                                                rasterImageDef.Load();
                                                if (rasterImageDef.IsLoaded)
                                                {
                                                    try
                                                    {
                                                        if (!string.IsNullOrEmpty(sourceFileName))
                                                        {
                                                            System.IO.File.Delete(sourceFileName);
                                                        }
                                                    }
                                                    catch
                                                    {
                                                    }
                                                }
                                                //rasterImage.Orientation = new CoordinateSystem3d(basePoint, v1, v2);
                                                rasterImage.Draw();
                                            }
                                            catch (System.Exception ex)
                                            {
                                                System.Windows.Forms.MessageBox.Show(ex.ToString());
                                            }
                                            try
                                            {
                                                rasterImageDef.UpdateEntities();
                                                doc.TransactionManager.QueueForGraphicsFlush();
                                                doc.TransactionManager.FlushGraphics();
                                                doc.Editor.UpdateScreen();
                                            }
                                            catch
                                            {
                                            }
                                            transaction.Commit();
                                        }
                                    }
                                    result = true;
                                }
                                catch (System.Exception)
                                {
                                    result = false;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                result = false;
            }
            return result;
        }

        public  bool GetActiveExtents(Document doc, out Point3d minPoint, out Point3d maxPoint)
        {
            minPoint = Point3d.Origin;
            maxPoint = Point3d.Origin;
            bool result;
            try
            {
                Point3d point3d = (Point3d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWCTR");
                double num = (double)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWSIZE");
                Point2d point2d = (Point2d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("SCREENSIZE");
                Point2d point2d2 = new Point2d(point2d.X, point2d.Y);
                double num2 = 0.5 * num * (point2d2.X / point2d2.Y);
                double num3 = 0.5 * num;
                Extents2d extents2d = new Extents2d(point3d.X - num2, point3d.Y- num3, point3d.X + num2, point3d.Y + num3);
                minPoint = new Point3d(extents2d.MinPoint.X, extents2d.MinPoint.Y, 0.0);
                maxPoint = new Point3d(extents2d.MaxPoint.X, extents2d.MaxPoint.Y, 0.0);
                result = true;
            }
            catch
            {
                result = false;
            }
            return result;
        }

        public void ZoomExtents()
        {
            try
            {
                Document document = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                using(document.LockDocument())
                {
                    Editor edit = document.Editor;
                    Database database = document.Database;
                    document.Database.UpdateExt(true);
                    Point3d extmax = database.Extmax;
                    Point3d extmin = database.Extmin;
                    new Extents3d(extmin, extmax);
                    ZoomExtents(extmin, extmax);
                }
                
            }
            catch (System.Exception ex)
            { 
            }
        }

        public bool  GetActiveViewportExtent(Document document, out Extents3d ext)
        {
            Point3d point3d;
			Point3d point3d2;
			if (GetActiveExtents(out point3d, out point3d2))
			{
				Point3d p = new Point3d(point3d.X, point3d.Y, 0.0);
				Point3d p2 = new Point3d(point3d2.X, point3d2.Y, 0.0);
				ext = new Extents3d(p, p2);
				//ext.SpatialReference = AfaDocData.ActiveDocData.DocPRJ.WKT;
				return true;
			}
            ext = new Extents3d();
			return false;
        }

        public bool GetActiveExtents(out Point3d minPoint, out Point3d maxPoint)
		{
			minPoint = Point3d.Origin;
			maxPoint = Point3d.Origin;
			Document mdiActiveDocument = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
			return !(mdiActiveDocument == null) && GetActiveExtents(mdiActiveDocument, out minPoint, out maxPoint);
		}

        public ViewportTableRecord getCurrentViewPort(Document document)
        {
            ViewportTableRecord viewport_tablerecord = new ViewportTableRecord();
            using (Transaction tr = document.Database.TransactionManager.StartTransaction())
            {
                ViewportTable viewport_table = tr.GetObject(document.Database.ViewportTableId, OpenMode.ForRead) as ViewportTable;
                viewport_tablerecord = tr.GetObject(viewport_table["*Active"], OpenMode.ForRead) as ViewportTableRecord;
                tr.Commit();
                return viewport_tablerecord;
            }
        }

    }

//    class MouseWheelMsgFilter:IMessageFilter
//    {
//        private int _level = 0;
//        public int Level
//        {
//            get { return _level; }
//            set { _level = value; }
//        }
//        public bool PreFilterMessage(ref Message m)
//        {
//            if (m.Msg == 0x20a && m.WParam != IntPtr.Zero )//WM_MOUSEWHEEL
//            {
//                Document document = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.CurrentDocument;
//                Editor editor = document.Editor;
//                editor.WriteMessage(m.ToString() + "\n" + m.WParam.ToString());
//                //int highword = HighWord((int)(m.WParam));

//                //if (highword > 0) // Scoll up or down?
//                //{
//                //    if (_level >=  0 && _level < 20)
//                //    {
//                //        ++_level;
//                //    }
//                //}
//                //else
//                //{
//                //    if (_level <= 20 && _level >= 1)
//                //    {
//                //        --_level;
//                //    }
//                //}
//                return true;
//            }

//            return false;
//        }

//        private Int32 HighWord(Int32 word)
//        {
//            return word >> 16; 
//        }
//    }
}
