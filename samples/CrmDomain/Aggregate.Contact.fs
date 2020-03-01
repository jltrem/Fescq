module CrmDomain.Aggregate.Contact

open System
open Fescq.Core
open Fescq.Repository
open CrmDomain

type Contact = {
   Name: PersonalName
   Phones: Map<Guid, PhoneNumber> 
}

(*
   COMMANDS
*)

type CreateContact (name:PersonalName) =
   inherit Fescq.Command.CreateAggregateCommand()
   member val Name = name

type RenameContact (aggregateId:Guid, originalVersion:int, name:PersonalName) =
   inherit Fescq.Command.UpdateCommand(aggregateId, originalVersion)
   member val Name = name

type AddContactPhone (aggregateId:Guid, originalVersion:int, phone: PhoneNumber) =
   inherit Fescq.Command.DetailCommand(aggregateId, originalVersion)
   member val Phone = phone

type UpdateContactPhone (aggregateId:Guid, originalVersion:int, phoneId:Guid, phone: PhoneNumber) =
   inherit Fescq.Command.DetailCommand(aggregateId, phoneId, originalVersion)
   member val Phone = phone

type ContactCommand =
   private
   | Create of CreateContact
   | Rename of RenameContact
   | AddPhone of AddContactPhone
   | UpdatePhone of UpdateContactPhone

// only public constructor for ContactCommand
let validateCommand (command:Fescq.Command.ICommand) =
   match command with 
   
   | :? CreateContact -> 
      let cmd = command :?> CreateContact
      cmd.Name 
      |> Validate.personalName
      |> Option.map (fun _ -> cmd |> ContactCommand.Create)
   
   | :? RenameContact -> 
      let cmd = command :?> RenameContact
      cmd.Name 
      |> Validate.personalName
      |> Option.map (fun _ -> cmd |> ContactCommand.Rename)
   
   | :? AddContactPhone -> 
      let cmd = command :?> AddContactPhone
      cmd.Phone
      |> Validate.phoneNumber
      |> Option.map (fun _ -> cmd |> ContactCommand.AddPhone)
   
   | :? UpdateContactPhone -> 
      let cmd = command :?> UpdateContactPhone
      cmd.Phone
      |> Validate.phoneNumber
      |> Option.map (fun _ -> cmd |> ContactCommand.UpdatePhone)
   
   | _ -> failwith "unexpected command"


(*
   EVENTS
*)

[<EventData("contact-created", 1)>]
type ContactCreated (name:PersonalName) = 
   interface IEventData
   member val Name = name

[<EventData("contact-renamed", 1)>]
type ContactRenamed (name:PersonalName) = 
   interface IEventData
   member val Name = name

[<EventData("contact-phone-added", 1)>]
type ContactPhoneAdded (phoneId:Guid, phone:PhoneNumber) = 
   interface IEventData
   member val PhoneId = phoneId
   member val Phone = phone

[<EventData("contact-phone-updated", 1)>]
type ContactPhoneUpdated (phoneId:Guid, phone:PhoneNumber) = 
   interface IEventData
   member val PhoneId = phoneId
   member val Phone = phone

type ContactEvent = 
   | Created of ContactCreated
   | Renamed of ContactRenamed
   | PhoneAdded of ContactPhoneAdded
   | PhoneUpdated of ContactPhoneUpdated


let validateEventData (eventData:IEventData) =
   match eventData with 
   | :? ContactCreated -> eventData :?> ContactCreated |> ContactEvent.Created
   | :? ContactRenamed -> eventData :?> ContactRenamed |> ContactEvent.Renamed
   | :? ContactPhoneAdded -> eventData :?> ContactPhoneAdded |> ContactEvent.PhoneAdded
   | :? ContactPhoneUpdated -> eventData :?> ContactPhoneUpdated |> ContactEvent.PhoneUpdated
   | _ -> failwith "unsupported event type"



let private applyContactEvent =

   let create (e:Event) =
      match validateEventData e.EventData with
      | ContactEvent.Created data ->
         { Name = data.Name
           Phones = []|> Map }
         |> fun contact -> Ok (EntityState.create contact)
      | _ -> (sprintf "unexpected event for create: %A" e.EventData) |> Error


   let update (state:EntityState<Contact>) (e:Event) =

      let prev = state.Entity

      let update = 
         match validateEventData e.EventData with    

         | ContactEvent.Created _ ->
            Error "update called with created event"

         | ContactEvent.Renamed data -> 
            Ok { prev with Name = data.Name }

         | ContactEvent.PhoneAdded data -> 
            if prev.Phones.ContainsKey(data.PhoneId) then
               Error "ContactPhoneAdded: id already exists"
            else
               Ok { prev with Phones = prev.Phones.Add(data.PhoneId, data.Phone) }

         | ContactEvent.PhoneUpdated data -> 
            if not(prev.Phones.ContainsKey(data.PhoneId)) then
               Error "ContactPhoneUpdated: id does not exist"
            else 
               Ok { prev with Phones = prev.Phones.Add(data.PhoneId, data.Phone) }

      match update with
      | Ok contact -> Ok (EntityState.update contact e)
      | Error err -> Error err


   Fescq.Aggregate.makeApplyFunc create update


module private Handle =

   let aggId (command:Fescq.Command.ICommand) =
      command.AggregateId

   let create utcNow metaData (command:CreateContact) =

      let key = 
         { Name = "contact"
           Id = aggId command
           Version = 1 }

      { 
         AggregateKey = key
         Timestamp = utcNow
         MetaData = metaData
         EventData = ContactCreated(command.Name) 
      }
      |> Fescq.Aggregate.createWithFirstEvent applyContactEvent

   
   let update utcNow metaData (command:Fescq.Command.ICommand) (aggregate:Agg<Contact>) =

      try
         command
         |> validateCommand 
         |> function 
            | Some cmd ->
               match cmd with 
               | Create _ -> failwith "'create' command provided for an update"
               | Rename cmd -> ContactRenamed(cmd.Name) :> IEventData, cmd |> aggId
               | AddPhone cmd -> ContactPhoneAdded(cmd.DetailId, cmd.Phone) :> IEventData, cmd |> aggId
               | UpdatePhone cmd -> ContactPhoneUpdated(cmd.DetailId, cmd.Phone) :> IEventData, cmd |> aggId
            | None -> failwith "data validation failed"

         |> fun (eventData, aggId) ->
            if aggId = aggregate.Key.Id then

               { AggregateKey = { aggregate.Key with Version = aggregate.Key.Version + 1 }
                 Timestamp = utcNow
                 MetaData = metaData
                 EventData = eventData }
               |> Fescq.Aggregate.createWithNextEvent applyContactEvent aggregate.History 
               |> Ok
            else 
               Error "aggregate and command refer to different ids"
      with
         ex -> Error ex.Message


module private Storage =

   let private factory history = 
      try
         let (agg, _) = Fescq.Aggregate.createFromHistory<Contact> applyContactEvent history
         Ok agg
      with 
         ex -> Error ex.Message
   
   let load (store:IEventStore) (aggId:Guid) = 
      Repository<Contact> store
      :> IRepository<Contact>
      |> fun x -> x.Load(aggId, factory)

   let loadExpectedVersion (store:IEventStore) (aggId:Guid) (expectedVersion:int) = 
      Repository<Contact> store
      :> IRepository<Contact>
      |> fun x -> x.LoadExpectedVersion(aggId, factory, expectedVersion)

   let save (store:IEventStore) (update:Agg<Contact> * Event list) =

      Repository<Contact> store
      :> IRepository<Contact>
      |> fun x -> x.Save(fst update, snd update)


module Workflow =

   let create (getUtcNow:unit->DateTimeOffset) (store:IEventStore) (metaData:string) (cmd:CreateContact) =
      Handle.create (getUtcNow()) metaData cmd
      |> fun (contact, events) ->
            store.AddEvent events.[0]
            |> Result.bind (fun _ -> store.Save())
            |> Result.bind (fun _ -> Ok contact)

   // TODO: make this accept an UpdateContact DU for the cmd
   let update (getUtcNow:unit->DateTimeOffset) (store:IEventStore) (aggId:Guid) (metaData:string) (cmd:Fescq.Command.UpdateCommand) =
      Storage.loadExpectedVersion store aggId cmd.OriginalVersion
      |> Result.bind (fun loaded -> Handle.update (getUtcNow()) metaData cmd loaded)
      |> Result.bind (fun updated -> Storage.save store updated)

   let load (store:IEventStore) (aggId:Guid) =
      Storage.load store aggId

   // TODO: load aggregate at particular version
