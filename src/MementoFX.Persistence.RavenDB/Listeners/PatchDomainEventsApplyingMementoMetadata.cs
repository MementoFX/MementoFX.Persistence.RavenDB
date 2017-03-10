using Raven.Client.Listeners;
using Raven.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.RavenDB.Listeners
{
    public class PatchDomainEventsApplyingMementoMetadata : IDocumentStoreListener
    {
        public void AfterStore(string key, object entityInstance, RavenJObject metadata)
        {

        }

        public bool BeforeStore(string key, object entityInstance, RavenJObject metadata, RavenJObject original)
        {
            if (entityInstance is DomainEvent)
            {
                metadata.Add("Memento-DomainEvent", true);
            }

            return true;
        }
    }
}
