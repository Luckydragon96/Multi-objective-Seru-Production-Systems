using NPOI.HSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOCCSNMS_NSGA2
{
    public class OuT
    {
        public class User
        {
            public double TTPT { get; set; }
            public double TLH { get; set; }
            public int numofSolution { get; set; }
            public double time { get; set; }
        }
        public void OutPut(List<SeruFormation> paretoOfFormation, List<SeruScheduling> paretoOfSeruSchedule, int sum, string name, double t)
        {
            //1、查询数据库数据  
            List<User> list = new List<User>();
            for (int i = 0; i < paretoOfFormation.Count; i++)
            {
                User user = new User() { TTPT = (paretoOfFormation[i]).throughPutTime, TLH = (paretoOfFormation[i]).labourTime, numofSolution = sum, time = t };
                list.Add(user);
            }
            for (int i = 0; i < paretoOfSeruSchedule.Count; i++)
            {
                User user = new User() { TTPT = (paretoOfSeruSchedule[i]).throughPutTime, TLH = (paretoOfSeruSchedule[i]).labourTime, numofSolution = sum, time = t};
                list.Add(user);
            }
            //2、  生成excel
            //2_1、生成workbook
            //2_2、生成sheet
            //2_3、遍历集合，生成行
            //2_4、根据对象生成单元格
            HSSFWorkbook workbook = new HSSFWorkbook();
            //创建工作表
            //var sheet = workbook.CreateSheet("界的信息");
            var sheet = workbook.CreateSheet("Test");
            //创建标题行（重点） 从0行开始写入
            var row = sheet.CreateRow(0);
            //创建单元格
            var cellTTPT = row.CreateCell(0);
            cellTTPT.SetCellValue("TTPT");
            var cellTLH = row.CreateCell(1);
            cellTLH.SetCellValue("TLH");
            var cellNumofSolution = row.CreateCell(2);
            cellNumofSolution.SetCellValue(" num");
            var cellTime = row.CreateCell(3);
            cellTime.SetCellValue(" time");
            //遍历集合，生成行
            int index = 1; //从1行开始写入
            for (int i = 0; i < list.Count; i++)
            {
                int x = index + i;
                var rowi = sheet.CreateRow(x);
                var ttpt = rowi.CreateCell(0);
                ttpt.SetCellValue(list[i].TTPT);
                var tlh = rowi.CreateCell(1);
                tlh.SetCellValue(list[i].TLH);
                var num = rowi.CreateCell(2);
                num.SetCellValue(list[i].numofSolution);
                var ctime = rowi.CreateCell(3);
                ctime.SetCellValue(list[i].time);
            }
            FileStream file = new FileStream(name, FileMode.OpenOrCreate, FileAccess.Write);
            workbook.Write(file);
            file.Dispose();
        }


    }

}
