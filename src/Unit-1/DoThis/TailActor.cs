using System;
using System.IO;
using System.Text;
using Akka.Actor;

namespace WinTail
{
    public class TailActor : UntypedActor
    {
        #region msg

        /// <summary>
        /// Signal that the file has changed, and we need to 
        /// read the next line of the file.
        /// </summary>
        public class FileWrite
        {
            public FileWrite(string fileName)
            {
                FileName = fileName;
            }

            public string FileName { get; }
        }

        /// <summary>
        /// Signal that the OS had an error accessing the file.
        /// </summary>
        public class FileError
        {
            public FileError(string fileName, string reason)
            {
                FileName = fileName;
                Reason = reason;
            }

            public string FileName { get; }

            public string Reason { get; }
        }

        /// <summary>
        /// Signal to read the initial contents of the file at actor startup.
        /// </summary>
        public class InitialRead
        {
            public InitialRead(string fileName, string text)
            {
                FileName = fileName;
                Text = text;
            }

            public string FileName { get; }
            public string Text { get; }
        }

        #endregion

        private readonly string _filePath;
        private readonly IActorRef _reporterActor;
        private StreamReader _fileStreamReader;
        private FileObserver _observer;

        public TailActor(IActorRef reportedActor, string filePath)
        {
            _reporterActor = reportedActor;
            _filePath = filePath;
        }

        protected override void PreStart()
        {
            _observer = new FileObserver(Self, Path.GetFullPath(_filePath));
            _observer.Start();

            Stream fileStream = new FileStream(Path.GetFullPath(_filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _fileStreamReader = new StreamReader(fileStream, Encoding.UTF8);

            Self.Tell(new InitialRead(_filePath, _fileStreamReader.ReadToEnd()));
        }

        protected override void PostStop()
        {
            _observer.Dispose();
            _observer = null;
            _fileStreamReader.Close();
            _fileStreamReader.Dispose();
            base.PostStop();
        }

        protected override void OnReceive(object message)
        {
            if (message is FileWrite)
            {
                var text = _fileStreamReader.ReadToEnd();
                if (!string.IsNullOrEmpty(text))
                {
                    _reporterActor.Tell(text);
                }
            }
            else if (message is FileError error)
            {
                _reporterActor.Tell($"tail error: {error.Reason}");
            }

            else if(message is InitialRead init)
            {
                _reporterActor.Tell(init.Text);
            }
        }
    }
}
