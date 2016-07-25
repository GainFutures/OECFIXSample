using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using OpenFAST;
using OpenFAST.Sessions;

namespace OEC.FIX.Sample.FAST
{
    public class ClientMessageHandler : IMessageListener
    {
        private readonly ManualResetEvent _messageEvent = new ManualResetEvent(false);
        private readonly List<Message> _messages = new List<Message>();
        private readonly Dictionary<string, string> _outputFiles = new Dictionary<string, string>();

        #region MessageListener Members

        public void OnMessage(Session session, Message message)
        {
            bool writeConsole = true;
            if (message.IsDefined(FastFieldsNames.MDREQID))
            {
                lock (_outputFiles)
                {
                    string id = message.GetString(FastFieldsNames.MDREQID);
                    if (_outputFiles.ContainsKey(id))
                    {
                        using (var sw = new StreamWriter(_outputFiles[id], true))
                        {
                            writeConsole = false;
                            sw.WriteLine(message.ToString());
                        }
                    }
                }
            }

            if (writeConsole)
                Console.WriteLine("<fast inc>: {0}", message);

            lock (_messages)
            {
                _messages.Add(message);
                _messageEvent.Set();
            }
        }

        #endregion

        public void AddOutputFiles(string id, string fileName)
        {
            lock (_outputFiles)
            {
                _outputFiles[id] = fileName;
            }
        }

        public void RemoveOutputFiles(string id)
        {
            lock (_outputFiles)
            {
                _outputFiles.Remove(id);
            }
        }

        public void Clear()
        {
            lock (_outputFiles)
            {
                _outputFiles.Clear();
            }
            lock (_messages)
            {
                _messages.Clear();
            }
        }

        public Message WaitMessage(string msgType, TimeSpan timeout, Predicate<Message> predicate)
        {
            // EnsureConnected();

            if (timeout < TimeSpan.Zero)
            {
                throw new ExecutionException("Invalid Timeout.");
            }

            lock (_messages)
            {
                Message msg = RetrieveMessage(msgType, predicate);
                if (msg != null)
                {
                    _messageEvent.Reset();
                    return msg;
                }
            }

            if (timeout == TimeSpan.Zero)
            {
                return null;
            }

            DateTime start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start) < timeout)
            {
                if (_messageEvent.WaitOne(timeout))
                {
                    lock (_messages)
                    {
                        Message msg = RetrieveMessage(msgType, predicate);
                        if (msg != null)
                        {
                            _messageEvent.Reset();
                            return msg;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        private Message RetrieveMessage(string msgType, Predicate<Message> predicate)
        {
            if (string.IsNullOrEmpty(msgType))
            {
                throw new ExecutionException("Invalid MsgType.");
            }

            for (int i = 0; i < _messages.Count; ++i)
            {
                Message msg = _messages[i];

                if (msg.IsDefined(FastFieldsNames.MSGTYPE) && msgType == msg.GetString(FastFieldsNames.MSGTYPE))
                {
                    if (predicate != null)
                    {
                        try
                        {
                            if (predicate(msg))
                            {
                                _messages.RemoveAt(i);
                                return msg;
                            }
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        _messages.RemoveAt(i);
                        return msg;
                    }
                }
            }
            return null;
        }
    }
}