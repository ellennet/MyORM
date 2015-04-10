using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using System.Linq;

namespace MyORM
{
    public class Run
    {
        #region 直接运行SQL语句
        /// <summary>
        /// 直接运行SQL语句
        /// </summary>
        /// <param name="ConnectionName">连接名</param>
        /// <param name="sql">sql语句</param>
        /// <param name="opType">操作类型</param>
        /// <returns></returns>
        public static object RunSQL(string connectionName,string sql, emOperationType opType)
        {
            string wrongMessage = "";
            object obj = null;

            DBLInit _DBLInit = new DBLInit(connectionName);            
            
            switch (opType)
            {
                case emOperationType.select:
                    obj = _DBLInit.GetDataTable(sql, out wrongMessage);
                    break;

                case emOperationType.delete:
                case emOperationType.insert:
                case emOperationType.update:
                    obj = _DBLInit.ProcessSql(sql, opType, out wrongMessage);
                    break;
            }

            MyORM.Log.WriteInfo(connectionName + "-" + opType.ToString() + "-" + sql);

            if (!string.IsNullOrEmpty(wrongMessage))
            {
                MyORM.Log.WriteError(wrongMessage);
            }

            return obj;
        }
        #endregion
    }

    /// <summary>
    /// 基类
    /// </summary>
    /// <typeparam name="T">T</typeparam>
    public class OrmOperation<T> where T : new()
    {
        #region 初始化
        public DBLInit _DBLInit;
        public string _TableName;        
        public int tid;//流水号 固定为tid
        public DataTable returnDt;
        private string _PrimaryKey = "tid";//主键 固定为tid

        /// <summary>
        /// 初始化表名及连接字符串
        /// </summary>
        public OrmOperation()
        {
            try
            {                
                Type myType = typeof(T);
                string className = myType.Name;
                InitAttribute iAttr = myType.GetCustomAttributes(true)[0] as InitAttribute;

                //数据表名
                _TableName = iAttr._TableName;

                //数据库连接名
                _DBLInit = new DBLInit(iAttr._ConnectionName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region linq相关
        private void DisplayTree(int indent, string message, Expression expression)
        {
            string output = String.Format("{0} {1}! NodeType: {2}; Expr: {3} ", "".PadLeft(indent, '>'), message, expression.NodeType, expression);

            indent++;
            switch (expression.NodeType)
            {
                case ExpressionType.Lambda:
                    System.Diagnostics.Trace.TraceInformation(output);
                    LambdaExpression lambdaExpr = (LambdaExpression)expression;
                    foreach (var parameter in lambdaExpr.Parameters)
                    {
                        DisplayTree(indent, "Parameter", parameter);
                    }
                    DisplayTree(indent, "Body", lambdaExpr.Body);
                    break;
                case ExpressionType.Constant:
                    ConstantExpression constExpr = (ConstantExpression)expression;
                    System.Diagnostics.Trace.TraceInformation("{0} Const Value: {1}", output, constExpr.Value);
                    break;
                case ExpressionType.Parameter:
                    ParameterExpression paramExpr = (ParameterExpression)expression;
                    System.Diagnostics.Trace.TraceInformation("{0} Param Type: {1}", output, paramExpr.Type.Name);
                    break;
                case ExpressionType.Equal:
                case ExpressionType.AndAlso:
                case ExpressionType.GreaterThan:
                    BinaryExpression binExpr = (BinaryExpression)expression;
                    if (binExpr.Method != null)
                    {
                        System.Diagnostics.Trace.TraceInformation("{0} Method: {1}", output, binExpr.Method.Name);
                    }
                    else
                    {
                        System.Diagnostics.Trace.TraceInformation(output);
                    }
                    DisplayTree(indent, "Left", binExpr.Left);
                    DisplayTree(indent, "Right", binExpr.Right);
                    break;
                case ExpressionType.MemberAccess:
                    MemberExpression memberExpr = (MemberExpression)expression;
                    System.Diagnostics.Trace.TraceInformation("{0} Member Name: {1}, Type: {2}", output, memberExpr.Member.Name, memberExpr.Type.Name);
                    DisplayTree(indent, "Member Expr", memberExpr.Expression);
                    break;
                default:
                    System.Diagnostics.Trace.TraceInformation("....{0} {1}", expression.NodeType, expression.Type.Name);
                    break;
            }

        }

        public List<T> Where(Expression<Func<T, bool>> expression)
        {
            Expression body = expression.Body;
            var para = expression.Parameters;
            string Name = para[0].Name;
            string es = body.ToString();
            //DisplayTree(0, "Lambda", expression);
            //Expression.Field(expression,)
            //Func<T, bool> myf = expression.Compile();            
            es = es.Replace(Name + ".", "this.")
                .Replace("AndAlso", " AND ")
                .Replace("OrElse", " OR ")
                .Replace("==", "=")
                .Replace("!=", "<>")
                .Replace(".Contains", " LIKE ")
                .Replace("\"", "'");

            return SelectByQual(es);
        }
        #endregion

        #region 插入记录
        /// <summary>
        /// 插入记录
        /// </summary>
        /// <returns>返回记录的流水号</returns>
        public int Insert()
        {
            var o = new OrmOperationBase(_DBLInit, _TableName, _PrimaryKey);
            return o.Insert(typeof(T), this);
        }
        #endregion

        #region 修改记录
        /// <summary>
        /// 修改记录
        /// </summary>
        /// <returns></returns>
        public int Update()
        {
            var o = new OrmOperationBase(_DBLInit, _TableName, _PrimaryKey);
            return o.Update(typeof(T), this, tid);
        }
        #endregion

        #region 移除记录
        /// <summary>
        /// 移除记录
        /// </summary>
        /// <returns></returns>
        public int Remove()
        {
            var o = new OrmOperationBase(_DBLInit, _TableName, _PrimaryKey);
            return o.Remove(tid, this.GetType());
        }
        #endregion

        #region 查询返回所有行
        /// <summary>
        /// 查询返回所有行
        /// </summary>
        /// <returns></returns>
        public List<T> SelectAll()
        {
            var o = new OrmOperationBase(_DBLInit, _TableName, _PrimaryKey);
            return o.Select<T>(this, out returnDt);
        }
        #endregion

        #region 根据条件查询
        /// <summary>
        /// 根据条件查询
        /// </summary>
        /// <param name="Qual">条件</param>
        /// <returns></returns>
        public List<T> SelectByQual(string Qual)
        {
            var o = new OrmOperationBase(_DBLInit, _TableName, _PrimaryKey);
            return o.Select<T>(this, out returnDt, Qual);
        }
        #endregion

        #region 根据主键查询
        /// <summary>
        /// 根据主键查询
        /// </summary>
        /// <param name="tid">主键值</param>
        /// <returns></returns>
        public T Get(int tid)
        {
            var o = new OrmOperationBase(_DBLInit, _TableName, _PrimaryKey);
            return o.Get<T>(this, tid);
        }
        #endregion           
    }

    /// <summary>
    /// 方法实现
    /// </summary>        
    public class OrmOperationBase
    {
        #region 初始化
        private DBLInit _DBLInit; //数据源
        private string _TableName; //表名
        private string _PrimaryKey; //主键名

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="DBLInit">数据源</param>
        /// <param name="TableName">表名</param>
        /// <param name="PrimaryKey">主键名</param>
        public OrmOperationBase(DBLInit DBLInit, string TableName, string PrimaryKey)
        {                        
            _DBLInit = DBLInit;
            _TableName = TableName;
            _PrimaryKey = PrimaryKey;
        }
        #endregion

        #region 生成查询语句
        /// <summary>
        /// 生成查询语句
        /// </summary>
        /// <param name="tp">实例的类型</param>
        /// <param name="obj">实例</param>
        /// <param name="Qual">查询条件</param>
        /// <param name="top">返回的记录数</param>
        /// <returns></returns>
        public string CreateSelectSQL(Type tp, object obj, string Qual, int top)
        {
            //缓存SQL语句
            string key = tp.ToString();
            string keyTableName = tp.ToString() + "TableName";
            MyCache.Caching cache = new MyCache.Caching();

            string sql = "select ";
            string _TableName = this._TableName;
            if (!cache.IsCache(key))
            {
                int tidflag = 0;
                string _PrimaryKey = "";
                FieldInfo[] fis = tp.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (top > 0) sql += String.Format(" top ({0}) ", top);
                for (int i = 0; i < fis.Length; i++)
                {
                    if (String.Compare(fis[i].Name, "_TableName", false) == 0)
                    {
                        //_TableName = fis[i].GetValue(obj).ToString();
                    }
                    else if (String.Compare(fis[i].Name, "_PrimaryKey", false) == 0)
                    {
                        _PrimaryKey = fis[i].GetValue(obj).ToString();
                    }
                    else if (String.Compare(fis[i].Name, "returnDt", false) == 0)
                    {
                    }
                    else if (String.Compare(fis[i].Name, "_DBLInit", false) != 0 && String.Compare(fis[i].Name, "_JoinList", false) != 0)
                    {
                        if (fis[i].Name == "tid")
                        {
                            if (tidflag == 0)
                            {
                                sql += String.Format("this.{0},", fis[i].Name);
                                tidflag = 1;
                            }
                        }
                        else
                        {
                            sql += String.Format("this.{0},", fis[i].Name);
                        }
                    }
                }
                sql = sql.Substring(0, sql.Length - 1);
                sql = sql.Replace("this", _TableName);
                sql += " FROM " + _TableName;

                cache.Add(key, sql);
                cache.Add(keyTableName, _TableName);
            }
            else
            {
                sql = cache.Get(key).ToString();
                _TableName = cache.Get(keyTableName).ToString();
            }


            if (string.IsNullOrEmpty(Qual))
                return sql;

            Qual = Qual.Replace("this", _TableName);
            sql += " WHERE " + Qual;

            return sql;
        }
        #endregion

        #region 插入记录
        /// <summary>
        /// 插入记录
        /// </summary>
        /// <param name="tp">对象Type</param>
        /// <param name="obj">对象实例</param>
        /// <param name="_DBLInit">DBL对象</param>
        /// <param name="_TableName">表名</param>
        /// <param name="_PrimaryKey">主键名</param>
        /// <returns></returns>
        public int Insert(Type tp, object obj)
        {
            string WrongMessage = null;
            string[] arrayID;
            object[] arrayValue;
            SqlDbType[] arrayType;
            int ret = 0;

            DataDevanning(tp, obj, out arrayID, out arrayValue, out arrayType);
            ret = _DBLInit.InsertData(arrayID, arrayValue, arrayType, _TableName, _PrimaryKey, out WrongMessage);

            MyORM.Log.WriteInfo("插入数据，表：" + _TableName + "，流水号：" + ret);

            if (!string.IsNullOrEmpty(WrongMessage))
            {
                MyORM.Log.WriteError(WrongMessage);
            }
            else
            {
                return ret;
            }          

            ret = -1;

            return ret;
        }
        #endregion

        #region 更新记录
        /// <summary>
        /// 更新记录
        /// </summary>
        /// <param name="tp">对象类型</param>
        /// <param name="obj">对象</param>
        /// <param name="_PrimaryKeyValue">主键值</param>
        /// <returns></returns>
        public int Update(Type tp, object obj, int _PrimaryKeyValue)
        {
            string WrongMessage = null;
            string[] arrayID;
            object[] arrayValue;
            SqlDbType[] arrayType;
            int ret = 0;

            DataDevanning(tp, obj, out arrayID, out arrayValue, out arrayType);

            WrongMessage = _DBLInit.UpdateDate(arrayID, arrayValue, arrayType, _TableName, _PrimaryKey);

            Log.WriteInfo("更新数据，表：" + _TableName + "，流水号：" + _PrimaryKeyValue);

            if (string.IsNullOrEmpty(WrongMessage))
            {
                //更新对应的缓存
                string key = tp.ToString() + "." + _PrimaryKeyValue;
                MyCache.Caching cache = new MyCache.Caching();
                cache.Add(key, obj, true); //重置缓存

                return ret;
            }

            MyORM.Log.WriteError(WrongMessage);                                        
            ret = -1;

            return ret;
        }
        #endregion

        #region 删除记录
        /// <summary>
        /// 删除记录
        /// </summary>
        /// <param name="_PrimaryKeyValue">主键值</param>
        /// <param name="tp">对象类型</param>
        /// <returns></returns>
        public int Remove(int _PrimaryKeyValue, Type tp)
        {
            string WrongMessage = null;
            string sql = String.Format("delete from {0} where {1}={2}", _TableName, _PrimaryKey, _PrimaryKeyValue);
            int ret = _DBLInit.ProcessSql(sql, emOperationType.delete, out WrongMessage);

            Log.WriteInfo("删除数据，表：" + _TableName + "，流水号：" + _PrimaryKeyValue);

            if (string.IsNullOrEmpty(WrongMessage))
            {
                //删除缓存               
                string key = tp.ToString() + "." + _PrimaryKeyValue;
                MyCache.Caching cache = new MyCache.Caching();
                cache.Remove(key);

                return ret;
            }

            ret = -1;
            MyORM.Log.WriteError(WrongMessage);

            return ret;
        }
        #endregion

        #region 查询数据
        /// <summary>
        /// 根据主键查询
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="obj">对象</param>
        /// <param name="tid">主键值</param>
        /// <returns></returns>
        public T Get<T>(object obj, int tid) where T : new()
        {
            //从缓存中读取该主键值的数据是否存在()
            var tp = obj.GetType();
            string key = tp.ToString() + "." + tid;
            MyCache.Caching cache = new MyCache.Caching();

            T v = new T();

            if (cache.IsCache(key))
            {
                v = (T)cache.Get(key);
            }
            else
            {
                string WrongMessage = null;
                string SelectSql = CreateSelectSQL(typeof(T), obj, "this.tid=" + tid, 0);
                var dt = _DBLInit.GetDataTable(SelectSql, out WrongMessage);

                if (WrongMessage == null)
                {
                    List<T> myList = new List<T>(dt.Rows.Count);
                    int i = 0;
                    foreach (DataRow dr in dt.Rows)
                    {
                        object objClass = Activator.CreateInstance(typeof(T), null);
                        myList.Add((T)DataPacking(typeof(T), objClass, dr));
                        i++;
                    }

                    if (myList.Count == 1)
                    {
                        v = myList[0];
                        cache.Add(key, v);
                    }
                }
                else
                {
                    //ClassCommon.LogError(String.Format("{0}|{1}", SelectSql, WrongMessage));
                }
            }

            return v;
        }


        /// <summary>
        /// 查询数据
        /// </summary>
        /// <typeparam name="T">对象泛型</typeparam>
        /// <param name="obj">对象</param>        
        /// <param name="returnDT">返回的数据Table</param>
        /// <param name="Qual">查询条件</param>
        /// <returns></returns>
        public List<T> Select<T>(object obj, out DataTable returnDT, string Qual = null) where T : new()
        {
            string WrongMessage = null;
            DataTable dt;
            string SelectSql = CreateSelectSQL(typeof(T), obj, Qual, 0);

            dt = _DBLInit.GetDataTable(SelectSql, out WrongMessage);
            returnDT = dt;

            List<T> myList = new List<T>(dt.Rows.Count);

            if (WrongMessage == null)
            {
                int i = 0;
                foreach (DataRow dr in dt.Rows)
                {
                    object objClass = Activator.CreateInstance(typeof(T), null);
                    myList.Add((T)DataPacking(typeof(T), objClass, dr));
                    i++;
                }
            }
            else
            {
                //ClassCommon.LogError(String.Format("{0}|{1}", SelectSql, WrongMessage));
            }

            return myList;
        }
        #endregion

        #region 根据DataRow生成对象 
        /// <summary>
        /// 根据DataRow生成对象
        /// </summary>
        /// <param name="tp">对象Type</param>
        /// <param name="obj">对象实例</param>
        /// <param name="dr">数据行</param>
        /// <returns></returns>
        public object DataPacking(Type tp, object obj, DataRow dr)
        {
            DateTime csNull = new DateTime(1, 1, 1);
            DateTime sqlNull = new DateTime(1900, 1, 1);

            FieldInfo[] fis = tp.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fis.Length; i++)
            {
                if (String.Compare(fis[i].Name, "returnDt", false) == 0)
                {
                }
                else if (String.Compare(fis[i].Name, "_TableName", false) != 0 && String.Compare(fis[i].Name, "_PrimaryKey", false) != 0 && String.Compare(fis[i].Name, "_DBLInit", false) != 0 && String.Compare(fis[i].Name, "_JoinList", false) != 0)
                {
                    string rTypeName = ""; //字段实际的类型名称
                    string v = ""; //可为空类型字段的值
                    bool isNullable = false;
                    
                    if (fis[i].FieldType.Name == "Nullable`1")
                    {
                        //可为空变量
                        Type[] t = fis[i].FieldType.GetGenericArguments();
                        rTypeName = t[0].Name;
                        isNullable = true;
                        v = dr[fis[i].Name].ToString();
                    }
                    else
                    {
                        //普通变量
                        rTypeName = fis[i].FieldType.Name;
                        isNullable = false;
                    }

                    //当字段为可控类型且值为空的时候，赋值空
                    if(isNullable && string.IsNullOrEmpty(v))
                    {
                        fis[i].SetValue(obj, null);
                    }
                    else
                    {
                        switch (rTypeName)
                        {
                            case "String":
                                fis[i].SetValue(obj, dr[fis[i].Name].ToString());
                                break;

                            case "Double":
                                double d1 = 0;
                                double.TryParse(dr[fis[i].Name].ToString(), out d1);
                                fis[i].SetValue(obj, d1);
                                break;

                            case "Single":
                            case "Float":
                                float f1 = 0;
                                float.TryParse(dr[fis[i].Name].ToString(), out f1);
                                fis[i].SetValue(obj, f1);
                                break;

                            case "Decimal":
                                decimal d2 = 0;
                                decimal.TryParse(dr[fis[i].Name].ToString(), out d2);
                                fis[i].SetValue(obj, d2);
                                break;

                            case "DateTime":
                                DateTime dt = new DateTime();
                                System.DateTime.TryParse(dr[fis[i].Name].ToString(), out dt);
                                fis[i].SetValue(obj, dt);
                                break;

                            case "Int32":
                                int i32 = 0;
                                int.TryParse(dr[fis[i].Name].ToString(), out i32);
                                fis[i].SetValue(obj, i32);
                                break;

                            case "Boolean":
                                bool b = false;
                                string a = dr[fis[i].Name].ToString();
                                bool.TryParse(dr[fis[i].Name].ToString(), out b);
                                fis[i].SetValue(obj, b);
                                break;

                            case "Byte[]":
                                if (dr[fis[i].Name].ToString() == "")
                                {
                                    fis[i].SetValue(obj, null);
                                }
                                else
                                {
                                    fis[i].SetValue(obj, dr[fis[i].Name]);
                                }

                                break;

                            default:
                                fis[i].SetValue(obj, dr[fis[i].Name].ToString());
                                break;
                        }
                    }                    
                }

            }

            return obj;
        }
        #endregion

        #region 从对象中返回字段名称、字段值、字段类型
        /// <summary>
        /// 从对象中返回字段、字段值、字段类型
        /// </summary>
        /// <param name="tp">对象类型</param>
        /// <param name="obj">对象</param>
        /// <param name="arrayID">字段名称</param>
        /// <param name="arrayValue">字段值</param>
        /// <param name="arrayType">字段类型</param>
        public void DataDevanning(Type tp, object obj, out string[] arrayID, out object[] arrayValue, out SqlDbType[] arrayType)
        {            
            int TidFlag = 0;
            List<FieldInfo> FieldInfoList = new List<FieldInfo>();
            FieldInfo[] fisa = tp.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo fi in fisa)
            {

                if (String.Compare(fi.Name, "_TableName", false) == 0 || String.Compare(fi.Name, "_PrimaryKey", false) == 0 || String.Compare(fi.Name, "_DBLInit", false) == 0 || String.Compare(fi.Name, "_JoinList", false) == 0 || String.Compare(fi.Name, "returnDt", false) == 0)
                    continue;

                if (fi.Name == "tid")
                {
                    if (TidFlag == 0)
                        TidFlag = 1;
                    else
                        continue;
                }

                FieldInfoList.Add(fi);
            }

            arrayID = new string[FieldInfoList.Count];
            arrayValue = new object[FieldInfoList.Count];
            arrayType = new SqlDbType[FieldInfoList.Count];
            int i = 0;
            foreach (FieldInfo fi in FieldInfoList)
            {
                arrayID[i] = fi.Name;
                arrayValue[i] = fi.GetValue(obj);

                switch (fi.FieldType.Name)
                {
                    case "String":
                        arrayType[i] = SqlDbType.VarChar;
                        break;

                    case "Single":
                    case "Float":
                        arrayType[i] = SqlDbType.Real;
                        break;

                    case "Double":
                        arrayType[i] = SqlDbType.Float;
                        break;

                    case "Decimal":
                        arrayType[i] = SqlDbType.Money;
                        break;

                    case "DateTime":
                        arrayType[i] = SqlDbType.DateTime;
                        break;

                    case "Int32":
                        arrayType[i] = SqlDbType.Int;
                        break;

                    case "Boolean":
                        arrayType[i] = SqlDbType.Bit;
                        break;

                    case "Byte[]":
                        arrayType[i] = SqlDbType.Image;
                        break;

                    default:
                        arrayType[i] = SqlDbType.VarChar;
                        break;
                }


                i++;
            }
           
        }
        #endregion
    }
}

