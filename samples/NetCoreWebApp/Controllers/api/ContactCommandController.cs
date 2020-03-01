using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LanguageExt;
using static LanguageExt.Prelude;
using static Fescq.Core;
using CrmDomain.CSharp;
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

         return await Task.Run(() =>

            Try(() => ContactWorkflow.Create(() => TimestampNow, _eventStore, "", cmd))
            .Match(
               Succ: agg => CreatedAtAction(nameof(GetAggregate), new { aggregateId = agg.Key.Id }, null),
               Fail: ex => ErrorResultAsBadRequest(ex))
         );
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

      private async Task<ActionResult> UpdateAsync(Guid aggregateId, Fescq.Command.UpdateCommand cmd) =>
         await Task.Run(() =>

            Try(() => ContactWorkflow.Update(() => TimestampNow, _eventStore, aggregateId, "", cmd))
            .Match(
               Succ: result => Ok(),
               Fail: ex => ErrorResultAsBadRequest(ex))
         );


      [HttpGet("{aggregateId}")]
      public async Task<ActionResult> GetAggregate(Guid aggregateId) =>
         await Task.Run(() => 

            Try(() => ContactWorkflow.Load(_eventStore, aggregateId))
            .Match(
               Succ: agg => ToJsonContent(new AggregateFetched<CrmDomain.Aggregate.Contact.Contact> { Key = agg.Key, Entity = agg.Entity }),
               Fail: ex => ErrorResultAsBadRequest(ex))
         );


      private static DateTimeOffset TimestampNow { get { return DateTimeOffset.UtcNow; } }

      private static ErrorResult ErrorResult(Exception ex) =>
         new ErrorResult { Error = string.IsNullOrEmpty(ex.Message) ? "unknown error" : ex.Message };
      
      private ActionResult ErrorResultAsBadRequest(Exception ex) => BadRequest(ErrorResult(ex)) as ActionResult;


      private ContentResult ToJsonContent<T>(T value) where T : class =>
         Newtonsoft.Json.JsonConvert.SerializeObject(value, _jsonSerializerSettings)
            .Apply(json => Content(json, "application/json"));

      private readonly Newtonsoft.Json.JsonSerializerSettings _jsonSerializerSettings =
         new Newtonsoft.Json.JsonSerializerSettings
         {
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
            Converters = new List<Newtonsoft.Json.JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
         };
   }
}
