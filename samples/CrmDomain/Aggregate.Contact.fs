module CrmDomain.Aggregate.Contact

open System
open Fescq
open CrmDomain

type Contact = {
   Name: PersonalName
   Phones: Map<Guid, PhoneNumber> 
}


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


let apply (state:(int*Contact) option) (e:Event) =

   let entity () =
      match state with 
      | Some (_, value) -> value
      | None -> failwith "state was None for update"

   let version = 
      match state with 
      | Some (value, _) -> value
      | None -> 1

   let update = 
      match validateEventData e.EventData with    
   
      | ContactEvent.Created data -> 
         match state with 
         | None -> { Name = data.Name; Phones = []|> Map }
         | Some _ -> failwith "state was Some for create"

      | ContactEvent.Renamed data -> 
         { entity() with Name = data.Name }

      | ContactEvent.PhoneAdded data -> 
         let prev = entity()
         if prev.Phones.ContainsKey(data.PhoneId) then failwith "ContactPhoneAdded: id already exists"
         { prev with Phones = prev.Phones.Add(data.PhoneId, data.Phone) }

      | ContactEvent.PhoneUpdated data -> 
         let prev = entity()
         if not(prev.Phones.ContainsKey(data.PhoneId)) then failwith "ContactPhoneUpdated: id does not exist"
         { prev with Phones = prev.Phones.Add(data.PhoneId, data.Phone) }

   Some (version, update)

module Handle =

   let aggId (command:Command.ICommand) =
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
      |> Aggregate.createWithFirstEvent apply

   
   let update utcNow metaData (command:Fescq.Command.ICommand) (aggregate:Agg<Contact>) =

      try
         command
         |> validateCommand 
         |> function 
            | Some cmd ->
               match cmd with 
               | Create cmd -> ContactCreated(cmd.Name) :> IEventData, cmd |> aggId
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
               |> Aggregate.createWithNextEvent apply aggregate.History 
               |> Ok
            else 
               Error "aggregate and command refer to different ids"
      with
         ex -> Error ex.Message

   module CSharp = 
   
      let Update utcNow metaData command aggregate =
         update utcNow metaData command aggregate
         |> function
            | Ok agg -> struct (Some struct (fst agg, snd agg), None)
            | Error msg -> struct (None, Some msg)

module Storage =

   let private factory history = 
      try
         let (agg, _) = Aggregate.createFromHistory<Contact> apply history
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

   module CSharp = 
      
      let Load (store:IEventStore, aggId:Guid) =
         load store aggId
         |> function
            | Ok agg -> struct (Some agg, None)
            | Error msg -> struct (None, Some msg)


      let LoadExpectedVersion (store:IEventStore, aggId:Guid, expectedVersion:int) =
         loadExpectedVersion store aggId expectedVersion
         |> function
            | Ok agg -> struct (Some agg, None)
            | Error msg -> struct (None, Some msg)

