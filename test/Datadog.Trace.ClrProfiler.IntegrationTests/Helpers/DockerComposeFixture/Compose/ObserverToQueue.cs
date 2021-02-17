using System;
using System.Collections.Generic;

namespace DockerComposeFixture.Compose
{
    public class ObserverToQueue<T>:IObserver<T>
    {
        public ObserverToQueue()
        {
            this.Queue = new Queue<T>();
        }
        public Queue<T> Queue { get; set; }
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            throw error;
        }

        public void OnNext(T value)
        {
            this.Queue.Enqueue(value);
        }
    }
}