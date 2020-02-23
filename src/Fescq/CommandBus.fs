module Fescq.CommandBus

open System
open System.Reactive.Subjects
open System.Reactive.Linq
open Fescq.Command


type ICommandBus =
   abstract member Send: ICommand -> unit
   abstract member Subscribe : Action<ICommand> -> IDisposable
   abstract member Subscribe<'t when 't : not struct and 't :> ICommand> : Action<ICommand> -> IDisposable

let cons () =
   let commands = new Subject<ICommand>()

   {
      new ICommandBus with
         member x.Send cmd = commands.OnNext cmd
         member x.Subscribe (onNext:Action<ICommand>) = commands.Subscribe onNext
         member x.Subscribe<'t when 't : not struct and 't :> ICommand> (onNext:Action<ICommand>) =
            commands
               .Where(fun x -> x :? 't)
               .Subscribe(onNext)
   }
