using Akka.Actor;
using Akka.Persistence;

namespace Messenger.Akka;

public class ConversationCoordinatorActor : ReceivePersistentActor
{
    public override string PersistenceId { get; } = "conversation-coordinator";

    public ConversationCoordinatorActor()
    {
        // Context.System.ActorSelection($"/user/conversation-coordinator/himars")
        //     .Tell(new CauseErrorMessage());
        
        Context.ActorOf(
            Props.Create(() =>
                new PlayerActor(createPlayerMessage.PlayerName, DefaultStartingHealth)), createPlayerMessage.PlayerName);
        
        Command<CreateConversationCommand>(command =>
        {
            Persist(command, payload =>
            {
                Context.ActorOf(
                    Props.Create(() => new ConversationActor(payload.ConversationId, payload.Subject)), $"conversation-{command.ConversationId}");
                
                
            });
        });
    }
}

public class ConversationActor : ReceivePersistentActor
{
    public override string PersistenceId => _state?.ConversationId;
    
    private ConversationActorState _state;

    public ConversationActor(string conversationId, string subject)
    {
        _state = new ConversationActorState(conversationId, subject);
        
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
    }
}

public record ConversationActorState(string ConversationId, string Subject)
{
    public ParticipantState[] Participants { get; init; }
};

public record ParticipantState(string ParticipantId, string FullName);

public record AddParticipantCommand(string ConversationId, string ParticipantId, string FullName);
public record CreateConversationCommand(string ConversationId, string Subject);

