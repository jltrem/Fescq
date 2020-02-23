namespace Fescq

open System
open Fescq.Command


[<AbstractClass>]
type CommandHandler<'t> (useRepo:Action<Action<IRepository<'t>>>, utcNow:Func<DateTimeOffset>, activeUserName:Func<string>) =

   member val UseRepo = useRepo
   member val UtcNow = utcNow
   member val Owner = activeUserName

   abstract Name: string with get

   member x.Key (cmd:ICommand, version) =
      { Name = x.Name
        Id = cmd.AggregateId
        Version = version }
