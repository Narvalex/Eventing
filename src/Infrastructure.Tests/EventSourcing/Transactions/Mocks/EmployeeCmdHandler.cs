using Infrastructure.EventSourcing;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using System.Threading.Tasks;

namespace Infrastructure.Tests.EventSourcing
{
    public class EmployeeCmdHandler : CommandHandler<Organization>,
        ICommandHandler<CreateOrganization>,
        ICommandHandler<CreateEmployee>
    {
        public EmployeeCmdHandler(IEventSourcedRepository repo)
            : base(repo)
        {
        }

        public async Task<IHandlingResult> Handle(CreateOrganization cmd)
        {
            var org = await this.NewEventSourced(cmd.OrgId);
            org.Update(cmd, new OrganizationCreated(cmd.OrgId));

            await this.CommitAsync(org);
            return this.Ok();
        }

        public async Task<IHandlingResult> Handle(CreateEmployee cmd)
        {
            using (var tx = await this.BeginTransactionAsync(cmd))
            {
                var org = await tx.AcquireLockAsync<Organization>(cmd.OrgId);

                // Contac Bath 1
                var contact = await tx.New<Contact>(cmd.EmployeeId);
                contact.Update(cmd,
                    new ContactCreated(cmd.EmployeeId),
                    new ContactActivated(cmd.EmployeeId)
                );
                await tx.PrepareAsync(contact);

                // Person Batch 2
                var person = await tx.New<Person>(cmd.EmployeeId);
                person.Update(cmd,
                    new PersonCreated(cmd.EmployeeId)
                );
                await tx.PrepareAsync(person);

                // Employee Batch 3
                var employee = await tx.New<Employee>(cmd.EmployeeId);
                employee.Update(cmd,
                    new EmployeeCreated(cmd.EmployeeId)
                );
                await tx.PrepareAsync(employee);

                // Person again Batch 4
                person.Update(cmd,
                    new PersonNameRegistered(cmd.EmployeeId, cmd.Name));
                await tx.PrepareAsync(person);

                // Check
                if (cmd.Name.IsEmpty())
                    return new HandlingResult(false);

                await tx.CommitAsync();

                return this.Ok();
            }
        }

        public async Task<IHandlingResult> PrepareButNotCommit(CreateEmployee cmd)
        {
            var tx = await this.BeginTransactionAsync(cmd);
            var org = await tx.AcquireLockAsync<Organization>(cmd.OrgId);

            // Contac Bath 1
            var contact = await tx.New<Contact>(cmd.EmployeeId);
            contact.Update(cmd,
                new ContactCreated(cmd.EmployeeId),
                new ContactActivated(cmd.EmployeeId)
            );
            await tx.PrepareAsync(contact);

            // Person Batch 2
            var person = await tx.New<Person>(cmd.EmployeeId);
            person.Update(cmd,
                new PersonCreated(cmd.EmployeeId)
            );
            await tx.PrepareAsync(person);

            // Employee Batch 3
            var employee = await tx.New<Employee>(cmd.EmployeeId);
            employee.Update(cmd,
                new EmployeeCreated(cmd.EmployeeId)
            );
            await tx.PrepareAsync(employee);

            // Person again Batch 4
            person.Update(cmd,
                new PersonNameRegistered(cmd.EmployeeId, cmd.Name));
            await tx.PrepareAsync(person);

            return this.Ok();
        }
    }

}
