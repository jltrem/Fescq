using System;
using LanguageExt;

namespace NetCoreWebApp.Models.ContactCommandModels
{
   public class ErrorResult
   {
      public string Error { get; set; }
   }

   public class AggregateFetched<Tentity>
   {
      public Fescq.Core.AggregateKey Key { get; set; }
      public Tentity Entity { get; set; }
   }

   public class CreateContact
   {
      public string Given { get; set; }
      public string Middle { get; set; }
      public string Family { get; set; }
   }

   public class RenameContact : CreateContact
   {
      public int OriginalVersion { get; set; }
   }

   public class AddOrUpdatePhone
   {
      public int OriginalVersion { get; set; }
      public string PhoneType { get; set; }
      public string Number { get; set; }
      public string Ext { get; set; }
   }

   public static class ContactCommandExt
   {
      public static CrmDomain.PhoneType PhoneTypeAsEnum(this AddOrUpdatePhone model) =>
         (model != null ? model.PhoneType : "")
            .ToLower()
            .Apply(x => x switch
            {
               "mobile" => CrmDomain.PhoneType.Mobile,
               "work" => CrmDomain.PhoneType.Work,
               "home" => CrmDomain.PhoneType.Home,
               _ => CrmDomain.PhoneType.Unknown
            });
   }
}
