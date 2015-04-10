using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyORM
{
    /// <summary>
    /// init
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class InitAttribute : Attribute
    {
        public string _TableName { get; set; }
        public string _ConnectionName { get; set; }

        public InitAttribute(string TableName, string ConnectionName)
        {
            this._TableName = TableName;
            this._ConnectionName = ConnectionName;
        }
    }

}
