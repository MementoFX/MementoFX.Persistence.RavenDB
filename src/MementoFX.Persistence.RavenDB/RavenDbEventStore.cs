using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Raven.Json.Linq;
using MementoFX.Messaging;
using MementoFX.Persistence.RavenDB.Indexes;
using MementoFX.Persistence.RavenDB.Listeners;

namespace MementoFX.Persistence.RavenDB
{
    /// <summary>
    /// Provides an implementation of a Memento event store
    /// using RavenDB as the storage
    /// </summary>
    public class RavenDbEventStore : EventStore
    {
        /// <summary>
        /// The underlying RavenDB Document Store
        /// </summary>
        public IDocumentStore DocumentStore { get; protected set; }

        private static IDocumentStore InitialiseDocumentStore()
        {
            var documentStore = new DocumentStore()
            {
                ConnectionStringName = "EventStore"
            };
            documentStore.Initialize();
            documentStore.RegisterListener(new PatchDomainEventsApplyingMementoMetadata());
            return documentStore;
        }

        /// <summary>
        /// Creates a new instance of the event store
        /// </summary>
        /// <param name="eventDispatcher">The event dispatcher to be used by the instance</param>
        public RavenDbEventStore(IEventDispatcher eventDispatcher) : this(InitialiseDocumentStore(), eventDispatcher)
        {
            
        }

        /// <summary>
        /// Creates a new instance of the event store
        /// </summary>
        /// <param name="documentStore">The document store to be used by the instance</param>
        /// <param name="eventDispatcher">The event dispatcher to be used by the instance</param>
        public RavenDbEventStore(IDocumentStore documentStore, IEventDispatcher eventDispatcher) : base(eventDispatcher)
        {
            if (documentStore == null)
                throw new ArgumentNullException(nameof(documentStore));
            if (eventDispatcher == null)
                throw new ArgumentNullException(nameof(eventDispatcher));
            DocumentStore = documentStore;

            new DomainEvents_Stream().Execute(DocumentStore);
            new RavenDocumentsByEntityName().Execute(DocumentStore);
        }

        /// <summary>
        /// Retrieves all events of a type which satisfy a requirement
        /// </summary>
        /// <typeparam name="T">The type of the event</typeparam>
        /// <param name="filter">The requirement</param>
        /// <returns>The events which satisfy the given requirement</returns>
        public override IEnumerable<T> Find<T>(Func<T, bool> filter)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var events = session.Query<T>().Where(filter);
                return events;
            }
        }

        /// <summary>
        /// Saves an event within the store
        /// </summary>
        /// <param name="event">The event</param>
        protected override void _Save(DomainEvent @event)
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Store(@event);
                session.SaveChanges();
            }
        }

        /// <summary>
        /// Retrieves the desired events from the store
        /// </summary>
        /// <typeparam name="T">The aggregate type for which to retrieve the events</typeparam>
        /// <param name="aggregateId">The aggregate id</param>
        /// <param name="pointInTime">The point in time up to which the events have to be retrieved</param>
        /// <param name="eventDescriptors">The descriptors defining the events to be retrieved</param>
        /// <param name="timelineId">The id of the timeline from which to retrieve the events</param>
        /// <returns>The list of the retrieved events</returns>
        public override IEnumerable<DomainEvent> RetrieveEvents(Guid aggregateId, DateTime pointInTime, IEnumerable<EventMapping> eventDescriptors, Guid? timelineId)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var descriptors = eventDescriptors.ToList();
                var fullQuery = "";
                for (int i = 0; i < descriptors.Count; i++)
                {
                    var d = descriptors[i];
                    var tag = DocumentStore.Conventions.FindTypeTagName(d.EventType);

                    var descriptorQuery = $"({d.AggregateIdPropertyName}:{aggregateId} AND Tag:{tag} AND TimeStamp:[* TO {pointInTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff")}]";
                    if (!timelineId.HasValue)
                    {
                        descriptorQuery += " AND TimelineId:[[NULL_VALUE]]";
                    }
                    else
                    {
                        descriptorQuery += $" AND (TimelineId:[[NULL_VALUE]] OR TimelineId:{timelineId.Value})";
                    }

                    descriptorQuery += ")";

                    fullQuery += descriptorQuery;
                    if (i < descriptors.Count - 1)
                    {
                        fullQuery += " OR ";
                    }
                }

                QueryHeaderInformation qhi;
                var query = session.Advanced.DocumentStore
                    .DatabaseCommands
                    .StreamQuery("DomainEvents/Stream", new IndexQuery()
                    {
                        Query = fullQuery,
                        SortedFields = new SortedField[]
                        {
                            new SortedField("+TimeStamp")
                        }
                    }, out qhi);

                var serializer = DocumentStore.Conventions.CreateSerializer();
                var events = new List<DomainEvent>();
                while (query.MoveNext())
                {
                    var mtd = (RavenJObject)query.Current["@metadata"];
                    var type = Type.GetType(mtd["Raven-Clr-Type"].ToString());

                    var instance = serializer.Deserialize(
                        new RavenJTokenReader(query.Current), type);

                    events.Add((DomainEvent)instance);
                }
                return events;
            }
        }
    }
}
