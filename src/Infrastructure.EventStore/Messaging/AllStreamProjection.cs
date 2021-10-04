namespace Infrastructure.EventStore.Messaging
{
    public static class AllStreamProjection
    {
        public const string EmittedStreamName = "all";

        public const string Script =
@"fromAll()
    .when({ 
        $any: (s, e) => {
            if (e.streamId.substring(0, 1) === '$')
                return;

            linkTo('" + EmittedStreamName + @"', e);
       }
})";
    }
}
