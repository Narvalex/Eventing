using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Processing.WriteLock;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Tests.Messaging.Handling.EmptyBus
{
    public class dyanmicBus : given_empty_bus
    {
        public dyanmicBus()
            : base(new CommandBus("Infrastructure.Tests.Messaging.Handling", new NoExclusiveWriteLock()))
        { }
    }

    //public class precompiledBus : given_empty_bus
    //{
    //    public precompiledBus()
    //        : base(new PrecompiledCommandBus())
    //    {

    //    }
    //}

    public abstract class given_empty_bus
    {
        private readonly ICommandBus sut;

        public given_empty_bus(ICommandBus sut)
        {
            this.sut = sut;
        }

        [Fact]
        public async Task when_sending_a_command_then_throws()
        {
            var command = new CommandA();
            await Assert.ThrowsAsync<KeyNotFoundException>(() => this.sut.Send(command, null));
        }

        [Fact]
        public void when_registering_handler_twice_then_throws()
        {
            var handlerMock = new Mock<ICommandHandler>().As<ICommandHandler<CommandA>>();
            ((ICommandHandlerRegistry)this.sut).Register(handlerMock.Object);

            Assert.Throws<ArgumentException>(() => ((ICommandHandlerRegistry)this.sut).Register(handlerMock.Object));
        }

        [Fact]
        public void when_registering_multiple_handlers_of_the_same_message_type_then_throws()
        {
            var handlerMock = new Mock<ICommandHandler>().As<ICommandHandler<CommandA>>();
            var secondHandlerMock = new Mock<ICommandHandler>().As<ICommandHandler<CommandA>>();

            ((ICommandHandlerRegistry)this.sut).Register(handlerMock.Object);

            Assert.Throws<ArgumentException>(() => ((ICommandHandlerRegistry)this.sut).Register(secondHandlerMock.Object));
        }

        [Fact]
        public void when_registering_single_handler_for_a_given_type_then_registration_succeeds()
        {
            var handlerMock = new Mock<ICommandHandler>().As<ICommandHandler<CommandA>>();
            var secondHandlerMock = new Mock<ICommandHandler>().As<ICommandHandler<CommandB>>();

            ((ICommandHandlerRegistry)this.sut).Register(handlerMock.Object);
            ((ICommandHandlerRegistry)this.sut).Register(secondHandlerMock.Object);
        }
    }
}

namespace Infrastructure.Tests.Messaging.Handling.BusWithHandler
{
    public class dyanmicBus : given_bus_with_handler
    {
        public dyanmicBus()
            : base(new CommandBus("Infrastructure.Tests.Messaging.Handling", new NoExclusiveWriteLock()))
        { }
    }

    // Precompiled was deprecated
    //--------------------------------------------------------
    //public class precompiledBus : given_bus_with_handler
    //{
    //    public precompiledBus()
    //        : base(new PrecompiledCommandBus())
    //    {

    //    }
    //}

    public abstract class given_bus_with_handler
    {
        private readonly ICommandBus sut;
        private readonly Mock<ICommandHandler> handlerMock;

        public given_bus_with_handler(ICommandBus sut)
        {
            this.sut = sut;

            this.handlerMock = new Mock<ICommandHandler>();
            this.handlerMock.As<ICommandHandler<CommandA>>()
                            .Setup(x => x.Handle(It.IsAny<CommandA>()))
                            .ReturnsAsync(new HandlingResult(true));

            ((ICommandHandlerRegistry)this.sut).Register(this.handlerMock.Object);
        }

        [Fact]
        public async Task when_sending_a_command_with_registered_handler_then_invokes_handler()
        {
            var command = new CommandA();

            await this.sut.Send(command, null);

            this.handlerMock.As<ICommandHandler<CommandA>>().Verify(h => h.Handle(command), Times.Once);
        }

        [Fact]
        public async Task when_sending_a_valid_command_then_handles_successfully()
        {
            var command = new CommandA();

            var result = await this.sut.Send(command, null);

            Assert.True(result.Success);
        }

        [Fact]
        public async Task when_sending_an_invalid_command_then_fails()
        {
            var command = new CommandA(validArgument: false);

            var result = await this.sut.Send(command, null);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task when_sending_a_command_with_no_registered_handler_then_throws()
        {
            var command = new CommandB();
            await Assert.ThrowsAsync<KeyNotFoundException>(() => this.sut.Send(command, null));
        }

        [Fact]
        public async Task when_sending_command_then_enriches_with_metadata_before_handling()
        {
            var command = new CommandA();
            var metadata = new MessageMetadata("authorId", "authorName", "ip", "win vista");

            await this.sut.Send(command, metadata);

            this.handlerMock.As<ICommandHandler<CommandA>>()
                .Verify(
                    h => h.Handle(It.Is<CommandA>(
                        x =>
                            x.GetMessageMetadata().AuthorId == metadata.AuthorId
                            && x.GetMessageMetadata().AuthorName == metadata.AuthorName
                            && x.GetMessageMetadata().ClientIpAddress == metadata.ClientIpAddress
                            && x.GetMessageMetadata().UserAgent == metadata.UserAgent
            )));
        }
    }
}

namespace Infrastructure.Tests.Messaging.Handling
{
    public class CommandA : Command
    {
        public CommandA(bool validArgument = true)
        {
            this.ValidArgument = validArgument;
        }

        public bool ValidArgument { get; }

        protected override ValidationResult OnExecutingBasicValidation()
            => this.Requires(this.ValidArgument, "The argument should be valid");
    }

    public class CommandB : Command
    {
        protected override ValidationResult OnExecutingBasicValidation()
            => this.IsValid();
    }
}
