using Infrastructure.EventSourcing;
using Infrastructure.Messaging;

namespace Infrastructure.Tests.EventSourcing
{
    public class CreateEmployee : Command
    {
        public CreateEmployee(string orgId, string employeeId, string name)
        {
            this.OrgId = orgId;
            this.EmployeeId = employeeId;
            this.Name = name;
        }

        public string OrgId { get; }
        public string EmployeeId { get; }
        public string Name { get; }
    }

    public class EmployeeCreated : Event
    {
        public EmployeeCreated(string employeeId)
        {
            this.EmployeeId = employeeId;
        }

        public override string StreamId => this.EmployeeId;

        public string EmployeeId { get; }
    }

    public class Employee : EventSourced
    {
        public Employee(EventSourcedMetadata metadata) : base(metadata)
        {
        }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry) =>
            registry.On<EmployeeCreated>();

    }

    public class Person : EventSourced
    {
        public Person(EventSourcedMetadata metadata, string? name) : base(metadata)
        {
            this.Name = name ?? "";
        }

        public string Name { get; private set; }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry) =>
            registry
                .On<PersonCreated>()
                .On<PersonNameRegistered>(x => this.Name = x.Name)
            ;
    }

    public class PersonCreated : Event
    {
        public PersonCreated(string personId)
        {
            this.PersonId = personId;
        }

        public override string StreamId => this.PersonId;

        public string PersonId { get; }
    }

    public class PersonNameRegistered : Event
    {
        public PersonNameRegistered(string personId, string name)
        {
            this.PersonId = personId;
            this.Name = name;
        }

        public override string StreamId => this.PersonId;

        public string PersonId { get; }
        public string Name { get; }
    }

    public class Contact : EventSourced
    {
        public Contact(EventSourcedMetadata metadata, bool isActivated)
            : base(metadata)
        {
            this.IsActivated = isActivated;
        }

        public bool IsActivated { get; private set; }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry) =>
            registry
                .On<ContactCreated>()
                .On<ContactActivated>(_ => this.IsActivated = true)
            ;
    }

    public class ContactCreated : Event
    {
        public ContactCreated(string contactId)
        {
            this.ContactId = contactId;
        }

        public override string StreamId => this.ContactId;

        public string ContactId { get; }
    }

    public class ContactActivated : Event
    {
        public ContactActivated(string streamId)
        {
            this.StreamId = streamId;
        }

        public override string StreamId { get; }
    }

    public class CreateOrganization : Command
    {
        public CreateOrganization(string orgId)
        {
            this.OrgId = orgId;
        }

        public string OrgId { get; }
    }

    public class OrganizationCreated : Event
    {
        public OrganizationCreated(string orgId)
        {
            this.OrgId = orgId;
        }

        public override string StreamId => this.OrgId;
        public string OrgId { get; }
    }

    public class Organization : EventSourced
    {
        public Organization(EventSourcedMetadata metadata)
            : base(metadata)
        {
        }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry) =>
            registry
                .On<OrganizationCreated>()
            ;
    }
}
