using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace MyORM
{
    #region define enum
    public enum emDBType
    {
        SqlServer,
        Access,
        MySql,
        SQLite
    };
    public enum emOperationType
    {
        select,
        update,
        insert,
        delete
    };
    #endregion

    #region DBLInit
    /// <summary>
    /// 数据源
    /// </summary>
    public class DBLInit : DBProcess
    {
        public DBLInit(string connectionName)
        {
            _ConnectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
        }
    }
    #endregion

    #region DBProcess
    /// <summary>
    /// DBProcess
    /// </summary>
    public abstract class DBProcess
    {
        public string _ConnectionString;
        private emDBType _dbType;        

        public static SqlConnection _ssqlConn;

        public string _SystemOperationId;


        public DBProcess()
        {
            
        }

        #region 运行SQL语句
        /// <summary>
        /// 运行SQL语句
        /// </summary>
        /// <param name="cmdText">SQL语句</param>
        /// <param name="eOType">操作方法</param>
        /// <param name="WrongMessage">返回的错误信息</param>
        /// <returns>当方法为insert的时候 返回新建索引</returns>
        public int ProcessSql(string cmdText, emOperationType eOType, out string WrongMessage)
        {
            WrongMessage = null;
            int tid = 0;
            switch (_dbType)
            {
                case emDBType.SqlServer:
                    if (eOType == emOperationType.insert) cmdText += ";select SCOPE_IDENTITY()";
                    using (SqlConnection conn = new SqlConnection(_ConnectionString))
                    {
                        SqlTransaction tx = null;
                        try
                        {
                            conn.Open();
                            tx = conn.BeginTransaction();
                            SqlCommand cmd = new SqlCommand(cmdText, conn) { Transaction = tx };
                            object objValue = cmd.ExecuteScalar();
                            if (objValue != null) tid = Convert.ToInt32(objValue);
                            tx.Commit();
                            cmd.Connection.Close();
                        }
                        catch (Exception ex)
                        {
                            WrongMessage = ex.Message;
                            tx.Rollback();
                        }
                    }
                    break;
            }
            return tid;
        }
        #endregion

        #region 调用存储过程
        /// <summary>
        /// 调用存储过程
        /// </summary>
        /// <param name="ProcedureName">存储过程名称</param>
        /// <param name="Names">输入参数名称</param>
        /// <param name="Values">输入参数值</param>
        /// <param name="Types">输入参数SQL类型</param>
        /// <param name="OutNames">输出参数名称</param>
        /// <param name="OutTypes">输出参数SQL类型</param>
        /// <param name="OutValues">输出参数值</param>
        /// <param name="WrongMessage">错误信息</param>
        /// <returns></returns>
        public DataTable GetProcedure(string ProcedureName, string[] Names, object[] Values, SqlDbType[] Types, string[] OutNames, SqlDbType[] OutTypes, out object[] OutValues, out string WrongMessage)
        {
            OutValues = null;
            WrongMessage = null;
            DataSet MyDataSet = new DataSet();
            DataTable dt = new DataTable();
            SqlDataAdapter DataAdapter = new SqlDataAdapter();
            try
            {
                using (SqlConnection conn = new SqlConnection(_ConnectionString))
                {
                    conn.Open();

                    SqlCommand myCommand = new SqlCommand(ProcedureName, conn);
                    myCommand.CommandType = CommandType.StoredProcedure;

                    //循环添加输入查询参数，赋值
                    for (int x = 0; x < Names.Length; x++)
                    {
                        myCommand.Parameters.Add("@" + Names[x], Types[x]);
                        myCommand.Parameters["@" + Names[x]].Value = Values[x];
                    }

                    //循环添加输出查询参数，赋值
                    for (int y = 0; y < OutNames.Length; y++)
                    {
                        myCommand.Parameters.Add("@" + OutNames[y], OutTypes[y], 500);
                        myCommand.Parameters["@" + OutNames[y]].Direction = ParameterDirection.Output;
                    }

                    myCommand.ExecuteNonQuery();
                    DataAdapter.SelectCommand = myCommand;
                    if (MyDataSet != null)
                    {
                        DataAdapter.Fill(dt);
                    }

                    for (int z = 0; z < OutNames.Length; z++)
                    {
                        OutValues[z] = myCommand.Parameters["@" + OutNames[z]].Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                WrongMessage = ex.Message;
            }

            return dt;
        }
        #endregion

        #region 返回DataTable
        /// <summary>
        /// 返回DataTable
        /// </summary>
        /// <param name="cmdText">SQL语句</param>
        /// <param name="WrongMessage">返回的错误信息</param>
        /// <returns>返回DataTable</returns>
        public DataTable GetDataTable(string cmdText, out string WrongMessage)
        {
            WrongMessage = null;
            DataTable dt = new DataTable();

            switch (_dbType)
            {
                case emDBType.SqlServer:
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(_ConnectionString))
                        {
                            using (SqlDataAdapter da = new SqlDataAdapter(cmdText, _ConnectionString))
                            {
                                conn.Open();
                                da.Fill(dt);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        WrongMessage = e.Message;
                    }
                    break;
            }
            return dt;
        }
        #endregion

        #region 更新数据
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="arrayID">字段名</param>
        /// <param name="arrayValue">字段值</param>
        /// <param name="arrayDataType">字段数据类型</param>
        /// <param name="table">数据表名</param>
        /// <param name="KeyID">主键字段</param>
        /// <returns></returns>
        public string UpdateDate(string[] arrayID, object[] arrayValue, SqlDbType[] arrayDataType, string table, string KeyID)
        {
            string WrongMessage = null;
            int fildcount = arrayID.Length;
            using (SqlConnection conn = new SqlConnection(_ConnectionString))
            {
                string KeyValue = "";
                SqlTransaction tx = null;
                try
                {
                    conn.Open();
                    tx = conn.BeginTransaction();
                    string sql = String.Format("update {0} set ", table);
                    string p = "";
                    SqlParameter[] paras = new SqlParameter[fildcount - 1];
                    int q = 0;
                    for (int i = 0; i < fildcount; i++)
                    {
                        if (arrayID[i] != KeyID)
                        {
                            p += String.Format("{0}=@{0},", arrayID[i]);
                            paras[q] = new SqlParameter();
                            paras[q].ParameterName = "@" + arrayID[i];
                            paras[q].SqlDbType = arrayDataType[i];

                            if (arrayDataType[i] == SqlDbType.Bit)
                            {
                                paras[q].Value = bool.Parse(arrayValue[i].ToString());
                            }
                            else
                            {
                                paras[q].Value = arrayValue[i];
                            }

                            q++;
                        }
                        else
                        {
                            KeyValue = arrayValue[i].ToString();
                        }

                    }
                    p = p.Remove(p.Length - 1);
                    sql += p;

                    sql += String.Format(" where {0}={1}", KeyID, KeyValue);

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    foreach (SqlParameter sp in paras)
                    {
                        if (sp.SqlDbType == SqlDbType.DateTime || sp.SqlDbType == SqlDbType.SmallDateTime)
                        {
                            DateTime dt;
                            if (DateTime.TryParse(sp.Value.ToString(), out dt))
                            {
                                if (dt.Year <= 1800)
                                {
                                    sp.Value = DBNull.Value;
                                }
                            }
                            else
                            {
                                if (sp.Value.ToString() == "0001-1-1 0:00:00" || sp.Value.ToString() == "0001/1/1 0:00:00" || string.IsNullOrEmpty(sp.Value.ToString()))
                                {
                                    sp.Value = DBNull.Value;
                                }
                            }
                        }
                        else
                        {
                            if (sp.Value != null)
                            {
                                if (string.IsNullOrEmpty(sp.Value.ToString()))
                                {
                                    sp.Value = DBNull.Value;
                                }
                            }
                            else
                            {
                                sp.Value = DBNull.Value;
                            }
                        }

                        cmd.Parameters.Add(sp);
                    }
                    cmd.Transaction = tx;
                    cmd.ExecuteNonQuery();
                    tx.Commit();
                    cmd.Connection.Close();
                }
                catch (Exception ex)
                {
                    WrongMessage = ex.Message;
                }
            }

            return WrongMessage;
        }
        #endregion

        #region 插入数据
        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="arrayID">字段名</param>
        /// <param name="arrayValue">字段值</param>
        /// <param name="arrayDataType">字段数据类型</param>
        /// <param name="table">数据表名</param>
        /// <param name="KeyId">主键字段</param>
        /// <param name="WrongMessage">错误信息</param>
        /// <param name="IsLog">是否记录</param>
        /// <returns></returns>
        public int InsertData(string[] arrayID, object[] arrayValue, SqlDbType[] arrayDataType, string table, string KeyId, out string WrongMessage)
        {
            int tid = -1;
            WrongMessage = null;
            int fieldcount = arrayID.Length;
            using (SqlConnection conn = new SqlConnection(_ConnectionString))
            {
                SqlTransaction tx = null;
                try
                {
                    conn.Open();
                    tx = conn.BeginTransaction();
                    string strID = "";
                    string strValue = "";
                    //int i = 0;
                    int q = 0;
                    SqlParameter[] paras = new SqlParameter[arrayID.Length - 1];
                    for (int i = 0; i < fieldcount; i++)
                    {
                        if (arrayID[i] != KeyId)
                        {
                            if (arrayDataType[i] == SqlDbType.DateTime || arrayDataType[i] == SqlDbType.SmallDateTime)
                            {
                                DateTime dt;
                                if (DateTime.TryParse(arrayValue[i].ToString(), out dt))
                                {
                                    if (dt.Year >= 1800)
                                    {
                                        strID += arrayID[i] + ",";
                                        strValue += String.Format("@{0},", arrayID[i]);
                                        paras[q] = new SqlParameter();
                                        paras[q].SqlDbType = arrayDataType[i];
                                        paras[q].ParameterName = "@" + arrayID[i];
                                        paras[q].Value = arrayValue[i];
                                        q++;
                                    }
                                }
                                else
                                {
                                    if (arrayValue[i].ToString() != "0001-1-1 0:00:00" && arrayValue[i].ToString() != "0001/1/1 0:00:00")
                                    {
                                        strID += arrayID[i] + ",";
                                        strValue += String.Format("@{0},", arrayID[i]);
                                        paras[q] = new SqlParameter();
                                        paras[q].SqlDbType = arrayDataType[i];
                                        paras[q].ParameterName = "@" + arrayID[i];
                                        paras[q].Value = arrayValue[i];
                                        q++;
                                    }
                                }
                            }
                            else
                            {
                                strID += arrayID[i] + ",";
                                strValue += String.Format("@{0},", arrayID[i]);
                                paras[q] = new SqlParameter();
                                paras[q].SqlDbType = arrayDataType[i];
                                paras[q].ParameterName = "@" + arrayID[i];
                                paras[q].Value = arrayValue[i];
                                q++;
                            }

                        }
                    }
                    strID = strID.Remove(strID.Length - 1);
                    strValue = strValue.Remove(strValue.Length - 1);

                    string strSql = String.Format("insert into {0}({1}) values({2});select @@identity", table, strID, strValue);
                    SqlCommand cmd = new SqlCommand(strSql, conn);
                    foreach (SqlParameter sp in paras)
                    {
                        if (sp != null)
                        {

                            if (sp.Value != null)
                            {
                                if (string.IsNullOrEmpty(sp.Value.ToString()))
                                {
                                    sp.Value = DBNull.Value;
                                }
                            }
                            else
                            {
                                sp.Value = DBNull.Value;
                            }

                            cmd.Parameters.Add(sp);
                        }
                    }
                    cmd.Transaction = tx;
                    object c = cmd.ExecuteScalar();
                    if (c != null)
                    {
                        int.TryParse(c.ToString(), out tid);
                    }
                    tx.Commit();
                    cmd.Connection.Close();
                }
                catch (Exception ex)
                {
                    WrongMessage = ex.Message;

                    tid = -1;
                }
            }

            return tid;
        }
        #endregion

    }
    #endregion
}
