using Infrastructure.Utils;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.EventStore.Messaging
{
    public static class StreamJoinProjectionScript
    {
        public static string Generate(string outuptStreamName, IEnumerable<string> streams)
        {
            Ensure.NotEmpty(outuptStreamName, nameof(outuptStreamName));
            Ensure.NotEmpty(streams, nameof(streams));

            var sb = new StringBuilder();
            streams.ForEach(s => sb.AppendLine($"            case '{Ensured.NotEmpty(s, "stream")}':"));
            var streamsToProject = sb.ToString().TrimEnd();

            var script = $@"
fromAll()
.when({{
    '$any': (s, e) => {{
        let streamId = e.streamId;
        if (streamId === undefined || streamId === null) return;
        let category = streamId.split('-')[0];
        
        switch(category) {{
{streamsToProject}
                linkTo('{outuptStreamName}', e);
                break;
            default:
                return;
        }}
    }}
}});
";
            return script.TrimStart();
        }
    }
}
