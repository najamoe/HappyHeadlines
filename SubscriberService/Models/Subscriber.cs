namespace SubscriberService.Models
{
    public class Subscriber
    {

        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public DateTime SubscribedOn { get; set; }
    }
}
