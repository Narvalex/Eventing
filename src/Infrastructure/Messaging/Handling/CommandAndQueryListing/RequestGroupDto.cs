using System.Collections.Generic;

namespace Infrastructure.Messaging.Handling
{
    public class RequestGroupDto
    {
        public RequestGroupDto()
        {
            this.Requests = new List<RequestDto>();
        }

        public string Name { get; set; }
        public bool IsQuery { get; set; }
        public List<RequestDto> Requests { get; set; }
    }
}
