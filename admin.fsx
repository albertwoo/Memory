#r "src/Memory.Db/bin/Debug/net8.0/Memory.Db.dll"
#r "nuget: Microsoft.EntityframeworkCore"
#r "nuget: Microsoft.EntityframeworkCore.Sqlite"

open System
open System.Linq
open Microsoft.EntityFrameworkCore
open Memory.Db

let memoryDb =
    let dbFile = "Data Source=C:\\IIS\\Memory\\Memory.db"
    let options = DbContextOptionsBuilder<MemoryDbContext>()
    new MemoryDbContext(options.UseSqlite(dbFile).Options)

memoryDb.Memories.Count()
