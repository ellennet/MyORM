using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyORM;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            //MyORM.Run.RunSQL("DefaultConnString", "select * from abc", emOperationType.select);
            var user = new UsersInfo();
            user = user.Get(2);
            //var list1 = user.SelectByQual("this.Name like '%a%'");
            //user.Name = "efg";
            //user.Update();

            var demo = new Demo();            

            var list = demo.SelectAll();

            Console.Read();
        }
    }

    [Init(TableName:"demo", ConnectionName: "DefaultConnString")]
    public class Demo : OrmOperation<Demo>
    {
        public string demo1;
        public decimal? demo2;
        public int? demo3;
        public double? demo4;
        public DateTime? demo5; 
    }

    /// <summary>
    /// 用户
    /// </summary>
    [Init(TableName: "Users", ConnectionName: "DefaultConnString")]
    public class UsersInfo : OrmOperation<UsersInfo>
    {
        /// <summary>
        /// 用户ID  
        /// </summary>
        public int UserID;

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name;

        /// <summary>
        /// 出生日期
        /// </summary>
        public DateTime? BirthDate;

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime RegDate;
    }
}
