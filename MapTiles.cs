using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
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
    class MapTiles
    {

        #region 经纬度与墨卡托互转
        //TODO
        #endregion


        #region 经纬度
        /// <summary>
        /// 经纬度和级别获取所在的瓦片行列号
        /// </summary>
        /// <param name="lon 经度"></param>
        /// <param name="lat 纬度"></param>
        /// <param name="zoom 缩放级别"></param>
        /// <param name="xtile "></param>
        /// <param name="ytile"></param>
        public static void LonLan2TilesColRow(double lon, double lat, int zoom,out int xtile,out int ytile)
        {
            PointF p = new Point();
            xtile = (int)((lon + 180.0) / 360.0 * (1 << zoom));
            ytile = (int)((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) +
                1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));
        }

        /// <summary>
        /// 瓦片行列号和级别获取经纬度
        /// </summary>
        /// <param name="tile_x"></param>
        /// <param name="tile_y"></param>
        /// <param name="zoom"></param>
        /// <param name="lon"></param>
        /// <param name="lat"></param>
        public static void TilesColRow2LanLon(int tile_x, int tile_y, int zoom,out double lon,out double lat)
        {           
            double n = Math.PI - ((2.0 * Math.PI * tile_y) / Math.Pow(2.0, zoom));
            lon = (float)((tile_x / Math.Pow(2.0, zoom) * 360.0) - 180.0);
            lat = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));
        }
        #endregion

        #region 网络墨卡托
        /// <summary>
        /// 获取网络墨卡托不同显示级别下分辨率
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static double webMercatorResolution(int level)
        {
            return 20037508.3427892 / Math.Pow(2,level+7);
        }

        /// <summary>
        /// 获取视口地理范围
        /// </summary>
        /// <param name="center 当前视口中心的地理坐标"></param>
        /// <param name="level 当前显示的级别"></param>
        /// <param name="viewsize 当前视口的大小 像素级别的大小"></param>
        /// <param name="leftdown 地理范围左下点"></param>
        /// <param name="rightup 地理范围右上点坐标"></param>
        public static void webMercatorBounds(Point2d center, int level, Size viewsize, ref Point2d left_down, ref Point2d right_up)
        {
            double resolution_l = webMercatorResolution(level);
            double left_down_x = center.X - resolution_l * viewsize.Width / 2.0;
            double left_down_y = center.Y - resolution_l * viewsize.Height / 2.0;

            left_down = new Point2d(left_down_x, left_down_y);

            double right_up_x = center.X + resolution_l * viewsize.Width / 2.0;
            double right_up_y = center.Y + resolution_l * viewsize.Height / 2.0;
            right_up = new Point2d(right_up_x,right_up_y);
        }

        /// <summary>
        /// 根据地理坐标 获取瓦片的起始行列号
        /// </summary>
        /// <param name="left_down 地理坐标左下点"></param>
        /// <param name="right_up 地理坐标右上点"></param>
        /// <param name="level 地理显示级别"></param>
        /// <param name="start_row 起始行号"></param>
        /// <param name="start_col 起始列号"></param>
        /// <param name="end_row 终止行号"></param>
        /// <param name="end_col 终止列号"></param>
        public static void webMercatorTilesFromBound(Point2d left_down, Point2d right_up, int level, out int start_row, out int start_col, out int end_row, out int end_col)
        {
            double origin_x = -m_earth_radius;
            double origin_y =  m_earth_radius;
            double resolution_l = webMercatorResolution(level);
            start_col = (int)Math.Floor(Math.Abs((origin_x - left_down.X)) / resolution_l / m_pipixel_size);
            start_col -= 1;
            if (0 > start_col)
            {
                start_col = 0;
            }
            end_row = (int)Math.Floor(Math.Abs(origin_y - left_down.Y) / resolution_l / m_pipixel_size);
            end_row += 1;
            if (0 > end_row)
            {
                end_row = 0;
            }
            end_col = (int)Math.Floor(Math.Abs(origin_x - right_up.X) / resolution_l / m_pipixel_size);
            if (0 > end_col)
            {
                end_col = 0;
            }
            start_row = (int)Math.Floor(Math.Abs(origin_y - right_up.Y) / resolution_l / m_pipixel_size);
            start_row -= 1;
            if (0 > start_row)
            {
                start_row = 0;
            }
        }

        public static void webMercatorTilesFromBound1(Point2d left_down, Point2d right_up, int level, out int start_row, out int start_col, out int end_row, out int end_col)
        {
            double origin_x = -m_earth_radius;
            double origin_y = m_earth_radius;
            double resolution_l = webMercatorResolution(level);
            start_row = (int)Math.Floor(Math.Abs((origin_x - left_down.X)) / resolution_l / m_pipixel_size);
            if (0 > start_row)
            {
                start_row = 0;
            }
            end_col = (int)Math.Floor(Math.Abs(origin_y - left_down.Y) / resolution_l / m_pipixel_size);
            if (0 > end_col)
            {
                end_col = 0;
            }
            end_row = (int)Math.Floor(Math.Abs(origin_x - right_up.X) / resolution_l / m_pipixel_size);
            if (0 > end_row)
            {
                end_row = 0;
            }
            start_col = (int)Math.Floor(Math.Abs(origin_y - right_up.Y) / resolution_l / m_pipixel_size);
            if (0 > start_col)
            {
                start_col = 0;
            }
        }

        /// <summary>
        /// 根据瓦片的行列号级别计算瓦片的坐标
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static Point2d webMercatorTilesLeftUpLocation(int row, int col, int level)
        {
            double resolution = webMercatorResolution(level);
            double left_up_x =  -m_earth_radius + row * m_pipixel_size * resolution;
            double left_up_y =  m_earth_radius - col * m_pipixel_size * resolution - m_pipixel_size*resolution;
            Point2d left_up = new Point2d(left_up_x, left_up_y);
            return left_up;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left_down"></param>
        /// <param name="right_up"></param>
        /// <param name="tile_leftup"></param>
        /// <param name="level"></param>
        /// <param name="distance_x"></param>
        /// <param name="distance_y"></param>
        public static void webMercatorTilesDistance(Point2d left_down, Point2d right_up, Point2d tile_leftup, int level, out double distance_x, out double distance_y)
        {
            double resolution_l = webMercatorResolution(level);
            distance_x = (left_down.X - tile_leftup.X) / resolution_l;
            distance_y = (tile_leftup.Y - right_up.Y) / resolution_l;
        }
        
        public static bool getIntersects(Point2d left_down, Point2d right_up,ref RectangleF result)
        {
            RectangleF view_display = new RectangleF((float)(left_down.X), (float)right_up.Y, (float)Math.Abs(left_down.X - right_up.X), (float)Math.Abs(left_down.Y - right_up.Y));
            RectangleF earth_range = new RectangleF(new PointF(-(float)m_earth_radius, (float)m_earth_radius), new SizeF((float)(m_earth_radius * 2), (float)(m_earth_radius * 2)));
            bool is_contain = view_display.IntersectsWith(earth_range);
            //darwBounds(left_down, right_up, Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 0), 20, 20);
            if (!is_contain)
            {
                return false;
            }
            else
            {
                result = RectangleF.Intersect(view_display, earth_range);
                return true;
            }
        }

        public static void darwBounds(Point2d left_down, Point2d right_up,Autodesk.AutoCAD.Colors.Color color,double start_width,double end_width)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            using(doc.LockDocument(DocumentLockMode.ProtectedAutoWrite,null,null,false))
            {
                Database database = doc.Database;
                using (Transaction trans = database.TransactionManager.StartTransaction())
                {
                    BlockTable black_table = trans.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord blocktable_record = trans.GetObject(black_table[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                    using (Autodesk.AutoCAD.DatabaseServices.Polyline poly = new Autodesk.AutoCAD.DatabaseServices.Polyline())
                    {
                        poly.AddVertexAt(0, new Point2d(left_down.X, left_down.Y), 0, start_width, end_width);
                        poly.AddVertexAt(1, new Point2d(right_up.X, left_down.Y), 0, start_width, end_width);
                        poly.AddVertexAt(2, new Point2d(right_up.X, right_up.Y), 0, start_width, end_width);
                        poly.AddVertexAt(3, new Point2d(left_down.X, right_up.Y), 0, start_width, end_width);
                        poly.Closed = true;
                        poly.Color = color;
                        blocktable_record.AppendEntity(poly);
                        trans.AddNewlyCreatedDBObject(poly, true);
                    }
                    trans.Commit();
                }
            }
        }

        public static void darwBounds()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database database = doc.Database;
            using (Transaction trans = database.TransactionManager.StartTransaction())
            {
                BlockTable black_table = trans.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord blocktable_record = trans.GetObject(black_table[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                using (Autodesk.AutoCAD.DatabaseServices.Polyline poly = new Autodesk.AutoCAD.DatabaseServices.Polyline())
                {
                    poly.AddVertexAt(0, new Point2d(-m_earth_radius, -m_earth_radius), 0, 10, 10);
                    poly.AddVertexAt(1, new Point2d(m_earth_radius, -m_earth_radius), 0, 10, 10);
                    poly.AddVertexAt(2, new Point2d(m_earth_radius, m_earth_radius), 0, 10, 10);
                    poly.AddVertexAt(3, new Point2d(-m_earth_radius, m_earth_radius), 0, 10, 10);
                    poly.AddVertexAt(4, new Point2d(-m_earth_radius, -m_earth_radius), 0, 10, 10);
                    poly.Closed = true;
                    poly.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 0, 0);
                    blocktable_record.AppendEntity(poly);
                    trans.AddNewlyCreatedDBObject(poly, true);
                }
                trans.Commit();
            }
        }

        /// <summary>
        /// 判断是否超限制
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private double getInsideValue(double value)
        {
            if (value > 0 && value > m_earth_radius)
            {
                value = m_earth_radius;
            }
            if (value < 0 && value < -m_earth_radius)
            {
                value = -m_earth_radius;
            }
            return value;
        }
        /// <summary>
        /// 地图范围：单位为米，20037508.3427892表示地图周长的一半，以地图中心点做为（0，0）坐标。
        /// </summary>
        private static int m_pipixel_size = 256;
        private static double m_earth_radius = 20037508.3427892;
        #endregion

    }


}
