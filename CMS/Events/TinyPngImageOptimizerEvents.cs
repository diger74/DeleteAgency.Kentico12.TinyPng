using System;

namespace Delete.Kentico12.TinyPng.Events
{
    public class TinyPngImageOptimizerEvents
    {
        public EventHandler<TinyPngImageOptimizerEventArgs> Before;

        public EventHandler<TinyPngImageOptimizerEventArgs> After;

        public EventHandler<TinyPngImageOptimizerEventArgs> Error;
    }
}