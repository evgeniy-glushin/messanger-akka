using Akka.Actor;
using Akka.Persistence;

namespace Messenger.Akka;

public class ConversationCoordinatorActor : ReceiveActor
{
    private Dictionary<string, IActorRef?> _activeConversations = new();
    //public override string PersistenceId { get; } = "conversation-coordinator";

    public ConversationCoordinatorActor()
    {
        // Context.System.ActorSelection($"/user/conversation-coordinator/himars")
        //     .Tell(new CauseErrorMessage());
        
        Receive<CreateConversationCommand>(cmd =>
        {
            var conversationActor = Context.ActorOf(Props.Create(() => new ConversationActor(cmd.ConversationId)), $"conversation-{cmd.ConversationId}");
            conversationActor.Tell(new InitConversation(cmd.ConversationId, cmd.Subject));
            _activeConversations.Add(cmd.ConversationId, conversationActor);
        });

        Receive<GetConversationActor>(cmd =>
        {
            if (_activeConversations.ContainsKey(cmd.ConversationId))
            {
                Sender.Tell(_activeConversations[cmd.ConversationId]);
            }
            else
            {
                var actor = Context.ActorOf(Props.Create(() => new ConversationActor("himars")), $"conversation-{cmd.ConversationId}");
                _activeConversations.Add(cmd.ConversationId, actor);
                Sender.Tell(actor);
            }
        });
    }
}

public class ConversationActor : ReceivePersistentActor
{
    public override string PersistenceId => _id;
    
    private ConversationActorState _state;
    private string _id;

    private int _index = 0;

    public ConversationActor(string id)
    {
        _id = id;
        Command<InitConversation>(command =>
        {
            Persist(command, persisted =>
            {
                _state = new ConversationActorState(persisted.ConversationId, persisted.Subject);
            });
        });
        
        Recover<InitConversation>(msg =>
        {
            _state = new ConversationActorState(msg.ConversationId, msg.Subject);
        });
        
        Command<AddParticipantCommand>(command =>
        {
            Persist(command, payload =>
            {
                _state = _state with
                {
                    Participants = _state.Participants.Concat(new[]
                        { new ParticipantState(payload.ParticipantId, payload.FullName) }).ToArray()
                };
            });   
        });
        
        Command<GetIndex>(_ => Sender.Tell(_index));
        
        Command<BumpIndex>(_ => ++_index);
    }
}

public record GetIndex();

public record BumpIndex();

public record GetConversationActor(string ConversationId);

public record InitConversation(string ConversationId, string Subject);

public record ConversationActorState(string ConversationId, string Subject)
{
    public ParticipantState[] Participants { get; init; }
};

public record ParticipantState(string ParticipantId, string FullName);

public record AddParticipantCommand(string ConversationId, string ParticipantId, string FullName);
public record CreateConversationCommand(string ConversationId, string Subject);

