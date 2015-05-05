# MyORM

simple orm

set connection info in app.config

model:
[Init(TableName:"demo", ConnectionName: "DefaultConnString")]
public class Demo : OrmOperation<Demo>
{
    public string demo1;
    public decimal? demo2;
    public int? demo3;
    public double? demo4;
    public DateTime? demo5; 
}
