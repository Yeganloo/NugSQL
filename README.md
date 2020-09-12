# NugSql

> **Attention**: Project is under construction.

## What is NugSql?
Nugql is descendant of [HugSql](https://www.hugsql.org/), [PugSql](https://pugsql.org/) and [PetaPoco](https://github.com/CollaboratingPlatypus/PetaPoco). The main idea is that:

`SQL is the right tool for the job when working with a relational database!`

So, learning SQL should be enough for a programmer to start working with a RDBMS.

NugSql based on some unbreakable rules:

1. No SQL generation!

> All SQL queries should execute as they are written.

2. No explicit  mapping for standard parameters.

> All DBMS basic data types (depend on DBMS) should be mapped automatically.

3. No explicit mapping for standard results!

> If result is a basic C# type or an object with properties of basic C# types, all mappings should be done automatically.

The first rule is the key, what makes NugSql fast, compatible and maintainable.

* No sql parsing is needed, so it is **Fast**.
* The same query you run inside your database tool will be used in app. So it is **Compatible** and **Maintainable**.
* The only parameters that effect your queries is your RDBMS! not the NugSql version, not anything else! so it is **Maintainable**.

The 2 last rules are the motivations to use an ORM!

## Usage:

1. ### write your queries:
create user query:
``` SQL
-- :name create_user :scalar
insert into test.user(user_name, password, salt, profile, status)
values(:user_name, :password, :salt, :profile, :status) RETURNING id
```
get user list query:
``` SQL
-- :name get_users :many
select  *
    from test.user u
    where u.user_name like :name
```
> **Attention**: The first line is a SQL-comment that specify which method should be linked to this query and what kind of result we expect from this query.

2. ### define your entities:

```c#
public class User
{
    public int id { get; set; }

    public string user_name { get; set; }

    public string profile { get; set; }

    public byte[] salt { get; set; }

    public byte[] password { get; set; }

    public short status { get; set; }
}
```

3. ### define your database interface:

``` c#
public interface IMyDB: IQueries
{
    int create_user(string user_name, byte[] password, byte[] salt, Jsonb profile, short status);
    IEnumerable<User> get_users(string name);
}
```

4. ### compile queries:
``` c#
var MyDBCompiled =  QuerBuilder.Compile<ISample>("path/to/queries", new PgDatabaseProvider());
```

5. ### connect to database and use it:
``` c#
var MyDB = QuerBuilder.New<ISample>(cnn, typ);
using(var tr = query.BeginTransaction())
{
    MyDB.create_user("u1", new byte[0], new byte[0], @"{ ""title"":""test1"" }", 1);
    MyDB.create_user("u2", new byte[0], new byte[0], @"{ ""title"":""test2"" }", 1);
    tr.Commit();
}
foreach(var u in query.get_users("u%"))
{
    // Do something!
}
```