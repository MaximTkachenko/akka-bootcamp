using Akka.Actor;

namespace WinTail
{
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main()
        {
            MyActorSystem = ActorSystem.Create(nameof(MyActorSystem));

            Props tailCoordinatorProps = Props.Create(() => new TailCoordinatorActor());
            MyActorSystem.ActorOf(tailCoordinatorProps, nameof(TailCoordinatorActor));

            Props consoleWriterProps = Props.Create<ConsoleWriterActor>();
            IActorRef consoleWriterActor = MyActorSystem.ActorOf(consoleWriterProps, nameof(ConsoleWriterActor));

            Props fileValidatorActorProps = Props.Create(() => new FileValidatorActor(consoleWriterActor));
            MyActorSystem.ActorOf(fileValidatorActorProps, nameof(FileValidatorActor));

            Props consoleReaderProps = Props.Create<ConsoleReaderActor>();
            IActorRef consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProps, nameof(ConsoleReaderActor));

            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }
    }
}
