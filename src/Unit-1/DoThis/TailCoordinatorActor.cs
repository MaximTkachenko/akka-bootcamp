using System;
using Akka.Actor;

namespace WinTail
{
    public class TailCoordinatorActor : UntypedActor
    {
        #region msg

        public class StartTail
        {
            public StartTail(string filePath, IActorRef reporterActor)
            {
                FilePath = filePath;
                ReporterActor = reporterActor;
            }

            public string FilePath { get; }
            public IActorRef ReporterActor { get; }
        }

        public class StopTail
        {
            public StopTail(string filePath)
            {
                FilePath = filePath;
            }

            public string FilePath { get; private set; }
        }

        #endregion

        protected override void OnReceive(object message)
        {
            if (message is StartTail start)
            {
                Context.ActorOf(Props.Create(() => new TailActor(start.ReporterActor, start.FilePath)));
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                10,
                TimeSpan.FromSeconds(30),
                x =>
                {
                    //Maybe we consider ArithmeticException to not be application critical
                    //so we just ignore the error and keep going.
                    if (x is ArithmeticException) return Directive.Resume;

                    //Error that we cannot recover from, stop the failing actor
                    if (x is NotSupportedException) return Directive.Stop;

                    //In all other cases, just restart the failing actor
                    return Directive.Restart;
                });
        }
    }
}
