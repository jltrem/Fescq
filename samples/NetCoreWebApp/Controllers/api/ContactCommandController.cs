using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LanguageExt;
using static LanguageExt.Prelude;
using static LanguageExt.FSharp;
using Fescq;
using ES = Fescq.EventStoreCSharp;
using NetCoreWebApp.Models.ContactCommandModels;

namespace NetCoreWebApp.Controllers.api
{
   [Route("api/command/contact")]
   [ApiController]
   public class ContactCommandController : ControllerBase
   {
      private readonly IEventStore _eventStore;

      public ContactCommandController(Storage.CrmEventStoreProvider provider)
      {
         _eventStore = provider.EventStore;
      }

      [HttpPost("create")]
      [Consumes(MediaTypeNames.Application.Json)]
      [ProducesResponseType(StatusCodes.Status201Created)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public async Task<ActionResult> Create(CreateContact model)
      {
         var name = new CrmDomain.PersonalName(model.Given, model.Middle, model.Family);
         var cmd = new CrmDomain.Aggregate.Contact.CreateContact(name);

         var (aggregate, events) = await Task.Run(() => CrmDomain.Aggregate.Contact.Handle.create(TimestampNow, "", cmd));

         if (events.Length == 1)
         {
            var (ok, error) = ES.AddEvent(_eventStore, events[0]);
            return fs(ok).Match(
               Some: _ =>
               {
                  ES.Save(_eventStore);
                  return CreatedAtAction(nameof(GetAggregate), new { aggregateId = aggregate.Key.Id }, null);
               },
               None: () => BadRequest(fs(error).IfNone("unknown error")) as ActionResult);
         }
         else
         {
            return BadRequest("could not create contact");
         }
      }

      [HttpPut("{aggregateId}/rename")]
      [Consumes(MediaTypeNames.Application.Json)]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public async Task<ActionResult> Rename(Guid aggregateId, [FromBody] RenameContact model)
      {
         var name = new CrmDomain.PersonalName(model.Given, model.Middle, model.Family);
         var cmd = new CrmDomain.Aggregate.Contact.RenameContact(aggregateId, model.OriginalVersion, name);
         return await UpdateAsync(aggregateId, cmd);
      }

      [HttpPut("{aggregateId}/add-phone")]
      [Consumes(MediaTypeNames.Application.Json)]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public async Task<ActionResult> AddPhone(Guid aggregateId, [FromBody] AddOrUpdatePhone model)
      {
         var phone = new CrmDomain.PhoneNumber(model.PhoneTypeAsEnum(), model.Number, model.Ext);
         var cmd = new CrmDomain.Aggregate.Contact.AddContactPhone(aggregateId, model.OriginalVersion, phone);
         return await UpdateAsync(aggregateId, cmd);
      }

      [HttpPut("{aggregateId}/update-phone/{phoneId}")]
      [Consumes(MediaTypeNames.Application.Json)]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public async Task<ActionResult> AddPhone(Guid aggregateId, Guid phoneId, [FromBody] AddOrUpdatePhone model)
      {
         var phone = new CrmDomain.PhoneNumber(model.PhoneTypeAsEnum(), model.Number, model.Ext);
         var cmd = new CrmDomain.Aggregate.Contact.UpdateContactPhone(aggregateId, model.OriginalVersion, phoneId, phone);
         return await UpdateAsync(aggregateId, cmd);
      }

      [HttpGet("{aggregateId}")]
      public async Task<ActionResult> GetAggregate(Guid aggregateId)
      {
         var (aggregate, loadError) = await Task.Run(() => CrmDomain.Aggregate.Contact.Storage.CSharp.Load(_eventStore, aggregateId));
         return fs(aggregate).Match(
            Some: agg => ToJsonContent(agg.Entity),
            None: () => BadRequest(fs(loadError).IfNone("unknown error")) as ActionResult);
      }


      private DateTimeOffset TimestampNow { get { return DateTimeOffset.UtcNow; } }

      private ContentResult ToJsonContent<T>(T value) where T : class =>
         Newtonsoft.Json.JsonConvert.SerializeObject(value, _jsonSerializerSettings)
            .Apply(json => Content(json, "application/json"));

      private readonly Newtonsoft.Json.JsonSerializerSettings _jsonSerializerSettings =
         new Newtonsoft.Json.JsonSerializerSettings
         {
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
            Converters = new List<Newtonsoft.Json.JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
         };

      private async Task<ActionResult> UpdateAsync(Guid aggregateId, Command.UpdateCommand cmd) =>
         await Task.Run(() =>
         {
            // TODO: move this ugliness to CrmDomain & only deal with the HTTP response here
            var (aggregate, loadError) = CrmDomain.Aggregate.Contact.Storage.CSharp.LoadExpectedVersion(_eventStore, aggregateId, cmd.OriginalVersion);
            return fs(aggregate).Match(
               Some: agg =>
               {
                  var (update, updateError) = CrmDomain.Aggregate.Contact.Handle.CSharp.Update(TimestampNow, "", cmd, agg);
                  return fs(update).Match(
                     Some: update =>
                     {
                        CrmDomain.Aggregate.Contact.Storage.save(_eventStore, update.Item1, update.Item2);
                        return Ok();
                     },
                     None: () => BadRequest(fs(updateError).IfNone("unknown error")) as ActionResult);
               },
               None: () => BadRequest(fs(loadError).IfNone("unknown error")) as ActionResult);
         });
   }
}
